using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Dynamically generates raw leather ThingDefs for all finished leathers
  /// (vanilla + modded) at game startup. Creates a 1:1 mapping where each
  /// finished leather gets a corresponding raw version.
  /// </summary>
  [StaticConstructorOnStartup]
  public static class RawLeatherDefGenerator
  {
    // Cached mapping: finished leather -> raw leather
    public static Dictionary<ThingDef, ThingDef> FinishedToRawMap =
      new Dictionary<ThingDef, ThingDef>();
    public static Dictionary<ThingDef, ThingDef> RawToFinishedMap =
      new Dictionary<ThingDef, ThingDef>();

    static RawLeatherDefGenerator()
    {
      // Run after defs are loaded but before game starts
      GenerateRawLeatherDefs();
    }

    private static void GenerateRawLeatherDefs()
    {
      Log.Message("[Production Expanded] Generating raw leather definitions...");

      int generated = 0;
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

        // Add to DefDatabase WITHOUT initializing graphics yet
        // RimWorld will call PostLoad/ResolveReferences during its normal def resolution phase
        DefGenerator.AddImpliedDef(rawLeather);

        // Cache the mapping
        FinishedToRawMap[finishedLeather] = rawLeather;
        RawToFinishedMap[rawLeather] = finishedLeather;

        generated++;
      }

      Log.Message(
        $"[Production Expanded] Generated {generated} raw leather definitions from {allLeathers.Count} finished leathers."
      );
    }

    private static ThingDef CreateRawLeatherDef(ThingDef finishedLeather)
    {
      // Determine category and size for this leather
      var category = LeatherTypeHelper.GetLeatherCategory(finishedLeather);
      var size = LeatherTypeHelper.GetSizeCategory(finishedLeather);
      string texturePath = LeatherTypeHelper.GetTexturePath(category, size);

      if (Prefs.DevMode)
      {
        Log.Message(
          $"[Production Expanded] Creating raw leather for {finishedLeather.defName}: category={category}, size={size}, texPath={texturePath}"
        );
      }

      // Create new ThingDef
      var rawLeather = new ThingDef
      {
        defName = $"PE_RawLeather_{finishedLeather.defName.Replace("Leather_", "")}",
        label = LeatherTypeHelper.GetRawLeatherLabel(finishedLeather),
        description = LeatherTypeHelper.GetRawLeatherDescription(finishedLeather),

        // Categories
        thingCategories = new List<ThingCategoryDef>
        {
          DefDatabase<ThingCategoryDef>.GetNamed("PE_RawLeathers", true),
        },

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

      return rawLeather;
    }

    /// <summary>
    /// Gets the raw version of a finished leather. Returns null if not found.
    /// </summary>
    public static ThingDef GetRawLeather(ThingDef finishedLeather)
    {
      FinishedToRawMap.TryGetValue(finishedLeather, out var raw);
      return raw;
    }

    /// <summary>
    /// Gets the finished version of a raw leather. Returns null if not found.
    /// </summary>
    public static ThingDef GetFinishedLeather(ThingDef rawLeather)
    {
      RawToFinishedMap.TryGetValue(rawLeather, out var finished);
      return finished;
    }
  }
}
