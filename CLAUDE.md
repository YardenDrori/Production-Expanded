# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is a RimWorld mod called "Production Expanded" that overhauls production chains by introducing multi-stage metal processing. The mod is XML-only, using definitions and patches to modify RimWorld's core game mechanics without C# code.

## Mod Structure

RimWorld mods follow a specific directory structure:
- `About/` - Mod metadata (About.xml, preview images, PublishedFileId.txt for Steam Workshop)
- `1.6/` - Version-specific content for RimWorld 1.6
  - `Defs/` - New game definitions organized by type:
    - `ThingDefs_Items/` - Resources (iron.xml, pig_iron.xml, unrefined_plasteel.xml)
    - `ThingDefs_Buildings/` - Production buildings (smelters.xml)
    - `ThingDefs_Ores/` - Mineable ore deposits (MineableSteel.xml, MineablePlasteel.xml)
    - `Recipes/` - Crafting recipes (steel_recipe.xml)
  - `Patches/` - XML patches that modify existing vanilla game definitions
- `Textures/` - Graphics organized by category (Things/Item/Resource/..., Things/Building/...)
- `Source/` - C# source code directory (currently empty, mod is XML-only)

## Production Chain Architecture

The mod implements a multi-stage metal production system:

**Iron → Steel:**
1. **Iron Ore (PE_Iron)** - Mined from iron ore veins (replaces vanilla steel veins via MineableSteel)
2. **Pig Iron (PE_PigIron)** - Intermediate product from smelting iron with wood/logs
3. **Steel** - Final product (vanilla item, production method changed)

**Unrefined Plasteel → Plasteel:**
1. **Unrefined Plasteel (PE_UnrefinedPlasteel)** - Mined from ore deposits or obtained from traders
2. **Plasteel** - Refined using steel/gold and chemfuel (vanilla item, production method changed)

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

## Recipe Patterns

Recipes follow consistent patterns:

**Standard Structure:**
- Base recipes (e.g., `SmeltPigIron`) with standard inputs/outputs
- Bulk variants with `x4` suffix, 4x inputs/outputs, higher skill requirements
- `displayPriority` controls UI ordering (higher numbers appear first)
- `recipeUsers` lists which workbenches can use the recipe
- `workAmount` defines base work time (modified by pawn skills/stats)

**Recipe Workbenches:**
- `PE_FueledAlloyFurnace` - Wood-fueled smelting (early game)
- `EM_ElectricAlloyFurnace` - Electric smelting (mid game)
- Commented-out advanced recipes reference `PE_ArcSmelter` (planned future content)

## Development Workflow

### Mod Dependencies

The mod requires Harmony (brrainz.harmony) as specified in About/About.xml. This is a core library for RimWorld modding.

### Testing

Since this is an XML-only mod:
1. Launch RimWorld with developer mode enabled (-logfile flag recommended)
2. Check dev console (Ctrl+F12) for XML errors on game load
3. Use "Open debug actions menu" to test recipes quickly
4. Verify:
   - Iron ore veins spawn on maps (replace steel veins)
   - Recipes appear at correct workbenches with proper ingredients
   - Market values match patch definitions
   - Deep drill doesn't produce vanilla steel/plasteel

### Code Style

C# files (when added) should use:
- 2-space indentation
- IDE0051/IDE0052 warnings enabled for unused private members

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

**Adding a new building:**
1. Create ThingDef with ParentName="BenchBase" in `1.6/Defs/ThingDefs_Buildings/`
2. Define graphicData with texture path and graphic class
3. Add comps for functionality (refuelable, power, glower, heat)
4. Set construction requirements (costList, skillPrerequisite, researchPrerequisites)
5. Reference in recipes via recipeUsers list

## Planned Features

From todo.txt, the mod plans to add:
- Coal resource with textures
- Log/plank processing system (neolithic and industrial methods)
- Potentially: leather tanning, lead resource with poisoning mechanics, spools/loom system

Many features are already partially implemented in commented-out XML (see steel_recipe.xml for plasteel refinement recipes).
