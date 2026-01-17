using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Dynamically generates raw wool ThingDefs for all finished wools
  /// (vanilla + modded) during the implied defs phase. Creates a 1:1 mapping
  /// where each finished wool gets a corresponding raw version.
  ///
  /// Called by DefGenerator_ImpliedDefs_Patch during GenerateImpliedDefs_PreResolve,
  /// which ensures the defs are created BEFORE category resolution occurs.
  /// </summary>
  public static class RawWoolDefGenerator
  {
    /// <summary>
    /// Generates raw wool ThingDefs for all wool materials in the game.
    /// Uses yield return to integrate with the implied defs system.
    /// </summary>
    public static IEnumerable<ThingDef> ImpliedRawWoolDefs()
    {
      var allWool = DefDatabase<ThingDef>
        .AllDefs.Where(def =>
          def.stuffProps != null
          && def.stuffProps.categories != null
          && def.stuffProps.categories.Contains(StuffCategoryDefOf.Fabric)
          && (def.defName.ToLower().Contains("wool") || def.label.ToLower().Contains("wool"))
        )
        .ToList();

      foreach (var finishedWool in allWool)
      {
        // Skip if already a raw wool (prevents infinite loops)
        if (finishedWool.defName.StartsWith("PE_RawWool_"))
          continue;

        var rawWool = CreateRawWoolDef(finishedWool);

        // Register with central registry for product replacement patches
        RawToFinishedRegistry.Register(rawWool, finishedWool);

        yield return rawWool;
      }
    }

    private static ThingDef CreateRawWoolDef(ThingDef finishedWool)
    {
      string texturePath = "Things/Item/Resource/PE_Wool";

      // Create new ThingDef
      var rawWool = new ThingDef
      {
        defName = $"PE_RawWool_{finishedWool.defName.Replace("Wool", "")}",
        label = finishedWool.label.Replace(" wool", " fleece"),
        description =
          "Raw wool freshly sheared from an animal. Still contains natural oils, dirt, and debris that make it unsuitable for weaving. Must be cleaned and spun into usable wool fabric.",

        // Hyperlinks to finished wool
        descriptionHyperlinks = new List<DefHyperlink>
        {
          new DefHyperlink(finishedWool),
        },

        // Initialize empty list - cross-ref loader will populate it
        thingCategories = new List<ThingCategoryDef>(),

        // Graphics - use category-based texture with color tint
        graphicData = new GraphicData
        {
          texPath = texturePath,
          graphicClass = typeof(Graphic_StackCount),
          color = finishedWool.graphicData?.color ?? Color.white,
          colorTwo = finishedWool.graphicData?.colorTwo ?? Color.white,
        },

        soundDrop = finishedWool.soundDrop ?? SoundDefOf.Standard_Drop,
        soundPickup = finishedWool.soundPickup ?? SoundDefOf.Standard_Pickup,
        soundInteract = finishedWool.soundInteract,

        statBases = new List<StatModifier>
        {
          new StatModifier { stat = StatDefOf.MaxHitPoints, value = 40 },
          new StatModifier { stat = StatDefOf.DeteriorationRate, value = 4 },
          new StatModifier { stat = StatDefOf.Mass, value = 0.04f }, // Slightly heavier
          new StatModifier { stat = StatDefOf.Flammability, value = 1f },
          new StatModifier
          {
            stat = StatDefOf.MarketValue,
            value = finishedWool.BaseMarketValue * 0.4f,
          }, // 40% value of finished
        },

        // NOT usable as stuff (must be processed first)
        stuffProps = null,

        // Misc properties
        stackLimit = 75,
        resourceReadoutPriority = ResourceCountPriority.Middle,
        useHitPoints = true,
        healthAffectsPrice = false,
        minRewardCount = 10,
        drawGUIOverlay = true,
        alwaysHaulable = true,
        rotatable = false,
        pathCost = DefGenerator.StandardItemPathCost,
        hiddenWhileUndiscovered = false,

        // Thing class
        thingClass = typeof(ThingWithComps),
        category = ThingCategory.Item,
        altitudeLayer = AltitudeLayer.Item,
        selectable = true,
        tickerType = TickerType.Rare, // For deterioration

        // Comps
        comps = new List<CompProperties>
        {
          new CompProperties_Forbiddable(),
          new CompProperties_Rottable { daysToRotStart = 180, rotDestroys = true },
        },
      };

      // Copy burnable property from finished wool
      if (finishedWool.burnableByRecipe)
        rawWool.burnableByRecipe = true;

      // Register category cross-reference for proper filter integration
      // This tells the cross-ref system to add PE_RawWools to thingCategories during resolution
      DirectXmlCrossRefLoader.RegisterListWantsCrossRef(
        rawWool.thingCategories,
        "PE_RawWools",
        rawWool
      );

      return rawWool;
    }
  }
}
