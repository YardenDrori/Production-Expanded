# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is a RimWorld mod called "Production Expanded" that overhauls production chains by introducing multi-stage processing for metals, wood, and textiles. The mod uses both XML definitions/patches and C# code to implement custom automated processor buildings.

## Mod Structure

- `About/` - Mod metadata (About.xml, preview images, PublishedFileId.txt for Steam Workshop)
- `1.6/` - Version-specific content for RimWorld 1.6
  - `Assemblies/` - Compiled C# DLL (build output from Source/)
  - `Defs/` - New game definitions:
    - `ThingDefs_Items/` - Resources (iron.xml, pig_iron.xml, raw_wood.xml, raw_cotton.xml, etc.)
    - `ThingDefs_Buildings/` - Production buildings (furnaces.xml, smelters.xml, saw_tables.xml, cloth_spinner.xml, tanning_drum.xml)
    - `ThingDefs_Ores/` - Mineable ore deposits
    - `Recipes/` - Crafting recipes (pig_iron_recipe.xml, wood_processing.xml, cloth_recipe.xml, etc.)
    - `JobDefs/` - Custom job definitions for processor buildings
    - `WorkGiverDefs/` - Work giver definitions that control pawn AI for processor buildings
  - `Patches/` - XML patches that modify vanilla definitions
- `Textures/` - Graphics organized by category
- `Source/ProductionExpanded/` - C# source code
  - `ProductionExpanded.csproj` - .NET 4.8 project file with RimWorld/Unity DLL references
  - `CompResourceProcessor.cs` - Core automated processor component
  - `Building_Processor.cs` - Building class for automated processors
  - `ProcessorRecipeDef.cs` - Custom recipe definition for processors
  - `ProcessorsCache.cs` - Map-level tracker for processor buildings
  - `JobDrivers/` - Custom job drivers for processor interaction
  - `WorkGivers/` - Work giver implementations

## Production Chain Architecture

The mod implements multi-stage production systems across three domains:

**Metal Processing (Iron → Steel):**
1. **Iron Ore (PE_Iron)** - Mined from iron ore veins (replaces vanilla steel veins via MineableSteel)
2. **Pig Iron (PE_PigIron)** - Intermediate product from smelting iron with wood/logs
3. **Steel** - Final product (vanilla item, production method changed)

**Wood Processing (Raw Wood → Lumber):**
1. **Raw Wood (WoodRaw)** - Harvested from trees (replaces vanilla WoodLog harvest)
2. **Lumber (WoodLog)** - Processed planks from raw wood (vanilla item repurposed, texture changed via patches)
3. Processing methods: Neolithic (CraftingSpot), Industrial (PE_SawTable), Automated (PE_AutoSawTable)

**Textile Processing (Raw Cotton/Devilstrand → Cloth):**
1. **Raw Cotton/Raw Devilstrand** - Harvested from plants (replaces vanilla cloth harvest)
2. **Cloth** - Processed fabric using cloth spinner buildings

### Naming Conventions

- Custom defNames use `PE_` prefix (Production Expanded)
- Bulk recipe variants use `x4` suffix (e.g., `SmeltPigIronBulk`)
- Mineable ores use `Mineable` prefix (e.g., `MineableSteel` which actually produces PE_Iron)
- Building defNames use mod prefix (e.g., `PE_FueledAlloyFurnace`, `EM_ElectricAlloyFurnace`)

## XML Patching Strategy

The mod uses PatchOperations to modify vanilla game behavior without replacing core files:

**Patch Types:**
- `PatchOperationReplace` - Changes existing values (market prices, texture paths)
- `PatchOperationRemove` - Removes elements (deep drill resources for steel/plasteel)
- `PatchOperationAdd` - Adds new elements (trader stock items)

**XPath Targeting:**
Patches use XPath queries to target specific XML elements:
```xml
<xpath>Defs/ThingDef[defName="Steel"]/statBases/MarketValue</xpath>
```

**Existing Patches:**
- `pricesChange.xml` - Adjusts vanilla steel/plasteel market values
- `removeDeepDrillResources.xml` - Removes steel/plasteel from deep drill loot tables
- `change_steel_texture.xml` - Updates steel texture references
- `removeTraderPlasteel.xml` - Modifies trader inventories

## Thing Definitions

Resources are defined as ThingDef with ParentName="ResourceBase":

**Key Properties:**
- `statBases` - Market value, mass, armor properties, damage multipliers
- `graphicData` - Texture paths (uses _a, _b, _c variants for stack sizes via Graphic_StackCount)
- `stuffProps` - If usable as a building/crafting material (categories, appearance, stat factors)
- `deepCommonality/deepCountPerPortion/deepLumpSizeRange` - Deep drill mining properties
- `thingSetMakerTags` - Controls trader stock inclusion (e.g., TraderStock)

**Building Definitions:**
Buildings use ParentName="BenchBase" with:
- `comps` - Components for refueling, power, lighting, heat
- `recipeUsers` - Referenced by recipes to determine which buildings can craft them
- `researchPrerequisites` - Technologies required to unlock

## Recipe System

The mod implements two distinct recipe systems:

**1. Standard RecipeDef (Manual Workbenches)**

Used for buildings where pawns manually perform crafting work:
- Base recipes (e.g., `SmeltPigIron`) with standard inputs/outputs
- Bulk variants with `x4` suffix (4x inputs/outputs, higher skill requirements)
- `displayPriority` controls UI ordering (higher = earlier in list)
- `recipeUsers` lists which workbenches can use the recipe
- `workAmount` defines base work time (modified by pawn skills/stats)

Examples: PE_FueledAlloyFurnace, EM_ElectricAlloyFurnace, PE_SawTable, CraftingSpot

**2. ProcessorRecipeDef (Automated Buildings)**

Custom recipe type (defined in ProcessorRecipeDef.cs) for automated processor buildings:
- Uses `ProductionExpanded.ProcessorRecipeDef` as XML element name
- Properties:
  - `ticksPerItem` - Base processing time per input item
  - `cycles` - Number of processing cycles required (multi-stage processing)
  - `ratio` - Input:output conversion ratio (e.g., 1.0 for 1:1)
  - `inputType` - ThingDef for input material
  - `outputType` - ThingDef for output material
- Requires buildings with `thingClass="ProductionExpanded.Building_Processor"`
- Used with CompResourceProcessor component

Example: PE_AutoSawTable (automated lumber processing)

## Automated Processor System Architecture

The automated processor system (used by PE_AutoSawTable and similar buildings) is implemented in C# with several interconnected components:

**CompResourceProcessor** (CompResourceProcessor.cs)
- Core component attached to processor buildings via `CompProperties_ResourceProcessor`
- Manages processing state: idle, processing, waiting for cycle continuation, finished
- Tracks progress through multi-cycle processing operations
- Handles power/fuel consumption during processing
- Properties configured in XML:
  - `maxCapacity` - Maximum items that can be added to the processor
  - `cycles` - Number of processing stages
  - `usesOnTexture` - Whether to show "_on" texture variant when processing
  - `hasIdlePowerCost` - Whether the building consumes power when idle

**ProcessorsCache** (ProcessorsCache.cs)
- Map-level component (`MapComponent_ProcessorTracker`) that tracks all processors
- Maintains lists of processors by state:
  - `processorsNeedingFill` - Can accept more input materials
  - `processorsNeedingEmpty` - Finished processing, need output extraction
  - `processorsNeedingCycleStart` - Multi-cycle processors waiting for pawn to advance cycle
- Used by WorkGivers to efficiently find processors that need pawn interaction

**Job System**
- Custom JobDefs define processor interactions (fill, empty, start cycle)
- JobDrivers (in JobDrivers/) implement the pawn behavior for each job
- WorkGivers (in WorkGivers/) scan ProcessorsCache lists to assign jobs to pawns

**Processing Flow:**
1. Pawn delivers materials via FillProcessor job → CompResourceProcessor.AddMaterials()
2. Processor automatically processes over time (CompTickRare)
3. For multi-cycle processors: pawn must interact via StartNextCycle job between cycles
4. When finished: pawn extracts output via EmptyProcessor job → CompResourceProcessor.EmptyBuilding()

## Development Workflow

### Building the C# Code

```bash
cd Source/ProductionExpanded
dotnet restore
dotnet build
```

Output DLL is written to `1.6/Assemblies/ProductionExpanded.dll`

The .csproj references RimWorld DLLs from the Linux Steam installation path. For macOS, uncomment the macOS paths and comment out the Linux paths.

### Testing

1. Launch RimWorld with developer mode enabled (`-logfile` flag recommended for detailed logs)
2. Check dev console (Ctrl+F12) for XML errors on game load
3. Use "Open debug actions menu" (developer mode) to:
   - Spawn test materials instantly
   - Force processor state changes (debug gizmos when DebugSettings.ShowDevGizmos is enabled)
4. Verify:
   - Processors accept materials and process them
   - Multi-cycle processors properly wait for pawn interaction
   - Power/fuel consumption matches configuration
   - Output items spawn at interaction cells
   - Patches apply correctly (check vanilla item textures, labels, harvest yields)

## Common Development Tasks

**Adding a new resource:**
1. Create ThingDef in `1.6/Defs/ThingDefs_Items/<resourcename>.xml`
2. Use `PE_` prefix for defName
3. Add mineable variant in `1.6/Defs/ThingDefs_Ores/Mineable<Name>.xml` if it should spawn as ore
4. Create textures in `Textures/Things/Item/Resource/<ResourceName>/` with _a, _b, _c variants
5. Add `thingSetMakerTags` with TraderStock if it should be tradeable

**Adding a new recipe:**
1. Create RecipeDef in `1.6/Defs/Recipes/<recipename>.xml`
2. Define ingredients (filter/thingDefs/count), products, workAmount, and skillRequirements
3. Add to recipeUsers list (PE_FueledAlloyFurnace, EM_ElectricAlloyFurnace, etc.)
4. Create bulk variant (x4) with 4x amounts and higher skill requirement
5. Set displayPriority for UI ordering (higher = earlier in list)

**Patching vanilla content:**
1. Create new XML file in `1.6/Patches/<patchname>.xml`
2. Use XPath to target specific elements: `Defs/ThingDef[defName="ItemName"]/path/to/element`
3. Comment out patches during development to test incrementally
4. Test patch applies correctly by checking in-game values/behavior

**Adding a manual workbench building:**
1. Create ThingDef with ParentName="BenchBase" in `1.6/Defs/ThingDefs_Buildings/`
2. Define graphicData with texture path and graphic class
3. Add comps for functionality (refuelable, power, glower, heat)
4. Set construction requirements (costList, skillPrerequisite, researchPrerequisites)
5. Create standard RecipeDefs that reference this building in recipeUsers

**Adding an automated processor building:**
1. Create ThingDef in `1.6/Defs/ThingDefs_Buildings/` with:
   - `thingClass="ProductionExpanded.Building_Processor"`
   - CompProperties_ResourceProcessor in comps section (maxCapacity, cycles, usesOnTexture, hasIdlePowerCost)
2. Create ProcessorRecipeDef in `1.6/Defs/Recipes/` with inputType/outputType/ticksPerItem/cycles/ratio
3. Create WorkGiverDef in `1.6/Defs/WorkGiverDefs/` that targets your specific processor defName
   - Use existing work givers as templates (work_at_manual_saw_table.xml, etc.)
   - Ensure the workGiver class matches the processor type (e.g., WorkGiver_FillProcessor)

**Patching vanilla harvest yields:**
- Create PatchOperationReplace targeting `Plants/PlantDef[defName="..."]/plant/harvestedThingDef`
- Used to redirect wood/cotton/devilstrand harvests to raw versions (WoodRaw, RawCotton, etc.)
- See change_harvested_wood.xml, change_harvested_cotton.xml for examples

**Patching vanilla item properties:**
- Use PatchOperationReplace to change existing ThingDef properties (textures, labels, market values)
- Use PatchOperationAttributeSet to change ParentName attribute
- See change_wood.xml for complex multi-operation patches (texture, label, color, deterioration rate)
