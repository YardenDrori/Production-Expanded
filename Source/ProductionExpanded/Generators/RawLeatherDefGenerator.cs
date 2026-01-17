using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Dynamically generates raw leather ThingDefs for all finished leathers
  /// (vanilla + modded) during the implied defs phase. Creates a 1:1 mapping
  /// where each finished leather gets a corresponding raw version.
  ///
  /// Called by DefGenerator_ImpliedDefs_Patch during GenerateImpliedDefs_PreResolve,
  /// which ensures the defs are created BEFORE category resolution occurs.
  /// </summary>
  public static class RawLeatherDefGenerator
  {
    /// <summary>
    /// Generates raw leather ThingDefs for all leathery materials in the game.
    /// Uses yield return to integrate with the implied defs system.
    /// </summary>
    public static IEnumerable<ThingDef> ImpliedRawLeatherDefs()
    {
      var allLeathers = DefDatabase<ThingDef>
        .AllDefs.Where(def =>
          def.stuffProps != null
          && def.stuffProps.categories != null
          && def.stuffProps.categories.Contains(StuffCategoryDefOf.Leathery)
        )
        .ToList();

      foreach (var finishedLeather in allLeathers)
      {
        // Skip if already a raw leather (prevents infinite loops)
        if (finishedLeather.defName.StartsWith("PE_RawLeather_"))
          continue;

        var rawLeather = CreateRawLeatherDef(finishedLeather);

        // Register with central registry for product replacement patches
        RawToFinishedRegistry.Register(rawLeather, finishedLeather);

        yield return rawLeather;
      }
    }

    private static ThingDef CreateRawLeatherDef(ThingDef finishedLeather)
    {
      // Determine category and size for this leather
      var category = LeatherTypeHelper.GetLeatherCategory(finishedLeather);
      var size = LeatherTypeHelper.GetSizeCategory(finishedLeather);
      string texturePath = LeatherTypeHelper.GetTexturePath(category, size);

      // Create new ThingDef
      var rawLeather = new ThingDef
      {
        defName = $"PE_RawLeather_{finishedLeather.defName.Replace("Leather_", "")}",
        label = LeatherTypeHelper.GetRawLeatherLabel(finishedLeather),
        description = LeatherTypeHelper.GetRawLeatherDescription(finishedLeather),

        // Hyperlinks to finished leather
        descriptionHyperlinks = new List<DefHyperlink>
        {
          new DefHyperlink(finishedLeather),
        },

        // Initialize empty list - cross-ref loader will populate it
        thingCategories = new List<ThingCategoryDef>(),

        // Graphics - use category-based texture with color tint
        graphicData = new GraphicData
        {
          texPath = texturePath,
          graphicClass = typeof(Graphic_StackCount),
          color = finishedLeather.graphicData?.color ?? Color.white,
          colorTwo = finishedLeather.graphicData?.colorTwo ?? Color.white,
        },

        // Sound effects (copy from finished leather or use defaults)
        soundDrop = finishedLeather.soundDrop ?? SoundDefOf.Standard_Drop,
        soundPickup = finishedLeather.soundPickup ?? SoundDefOf.Standard_Pickup,
        soundInteract = finishedLeather.soundInteract,

        // Base stats (raw leather is heavier, deteriorates faster than finished)
        statBases = new List<StatModifier>
        {
          new StatModifier { stat = StatDefOf.MaxHitPoints, value = 40 },
          new StatModifier { stat = StatDefOf.DeteriorationRate, value = 4 }, // 2x vanilla leather
          new StatModifier { stat = StatDefOf.Mass, value = 0.04f }, // Slightly heavier
          new StatModifier { stat = StatDefOf.Flammability, value = 1.2f }, // More flammable (not treated)
          new StatModifier
          {
            stat = StatDefOf.MarketValue,
            value = finishedLeather.BaseMarketValue * 0.4f,
          }, // 40% value of finished
        },

        // NOT usable as stuff (must be tanned first)
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
          new CompProperties_Rottable
          {
            daysToRotStart = 14, // Rots faster than finished leather (not preserved)
            rotDestroys = true,
          },
        },
      };

      // Copy some properties from finished leather
      if (finishedLeather.burnableByRecipe)
        rawLeather.burnableByRecipe = true;

      // Register category cross-reference for proper filter integration
      // This tells the cross-ref system to add PE_RawLeathers to thingCategories during resolution
      DirectXmlCrossRefLoader.RegisterListWantsCrossRef(
        rawLeather.thingCategories,
        "PE_RawLeathers",
        rawLeather
      );

      return rawLeather;
    }
  }
}
