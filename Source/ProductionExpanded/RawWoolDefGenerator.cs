using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  [StaticConstructorOnStartup]
  public static class RawWoolDefGenerator
  {
    // Cached mapping: finished leather -> raw leather
    public static Dictionary<ThingDef, ThingDef> FinishedToRawMap =
      new Dictionary<ThingDef, ThingDef>();
    public static Dictionary<ThingDef, ThingDef> RawToFinishedMap =
      new Dictionary<ThingDef, ThingDef>();

    static RawWoolDefGenerator()
    {
      // Run after defs are loaded but before game starts
      GenerateRawLeatherDefs();
    }

    private static void GenerateRawLeatherDefs()
    {
      Log.Message("[Production Expanded] Generating raw wool definitions...");

      int generated = 0;
      var allWool = DefDatabase<ThingDef>
        .AllDefs.Where(def =>
          def.stuffProps != null
          && def.stuffProps.categories != null
          && def.stuffProps.categories.Contains(StuffCategoryDefOf.Fabric)
          && def.defName.Contains("wool")
        )
        .ToList();

      foreach (var finishedWool in allWool)
      {
        // Skip if already a raw leather (prevents infinite loops)
        if (finishedWool.defName.StartsWith("PE_RawWool_"))
          continue;

        var rawWool = CreateRawLeatherDef(finishedWool);

        // Add to DefDatabase WITHOUT initializing graphics yet
        // RimWorld will call PostLoad/ResolveReferences during its normal def resolution phase
        DefGenerator.AddImpliedDef(rawWool);

        // Cache the mapping
        FinishedToRawMap[finishedWool] = rawWool;
        RawToFinishedMap[rawWool] = finishedWool;

        generated++;
      }

      Log.Message(
        $"[Production Expanded] Generated {generated} raw wool definitions from {allWool.Count} finished wools."
      );
    }

    private static ThingDef CreateRawLeatherDef(ThingDef finishedWool)
    {
      string texturePath = $"Things/Item/Resource/PE_Wool";
      // Create new ThingDef
      var rawWool = new ThingDef
      {
        defName = $"PE_RawWool_{finishedWool.defName.Replace("Leather_", "")}",
        label = $"{finishedWool.label.Replace(" wool", " fleece")}",
        description =
          $"Raw wool freshly sheared from an animal. Still contains natural oils, dirt, and debris that make it unsuitable for weaving. Must be cleaned and spun into usable wool fabric. <link=\"{finishedWool.defName}\">{finishedWool.label}</link>",

        // Graphics - use category-based texture with color tint
        graphicData = new GraphicData
        {
          texPath = texturePath,
          graphicClass = typeof(Graphic_StackCount),
          color = finishedWool.graphicData?.color ?? Color.white,
          colorTwo = finishedWool.graphicData?.colorTwo ?? Color.white,
        },

        // Sound effects (copy from finished leather or use defaults)
        soundDrop = finishedWool.soundDrop ?? SoundDefOf.Standard_Drop,
        soundPickup = finishedWool.soundPickup ?? SoundDefOf.Standard_Pickup,
        soundInteract = finishedWool.soundInteract,

        // Base stats (raw leather is heavier, deteriorates faster than finished)
        statBases = new List<StatModifier>
        {
          new StatModifier { stat = StatDefOf.MaxHitPoints, value = 40 },
          new StatModifier { stat = StatDefOf.DeteriorationRate, value = 4 }, // 2x vanilla leather
          new StatModifier { stat = StatDefOf.Mass, value = 0.04f }, // Slightly heavier
          new StatModifier { stat = StatDefOf.Flammability, value = 1f }, // More flammable (not treated)
          new StatModifier
          {
            stat = StatDefOf.MarketValue,
            value = finishedWool.BaseMarketValue * 0.4f,
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
            daysToRotStart = 180, // Rots faster than finished leather (not preserved)
            rotDestroys = true,
          },
        },
      };

      // Copy some properties from finished leather
      if (finishedWool.burnableByRecipe)
        finishedWool.burnableByRecipe = true;

      return rawWool;
    }

    /// <summary>
    /// Gets the raw version of a finished leather. Returns null if not found.
    /// </summary>
    public static ThingDef GetRawWool(ThingDef finishedWool)
    {
      FinishedToRawMap.TryGetValue(finishedWool, out var raw);
      return raw;
    }

    /// <summary>
    /// Gets the finished version of a raw leather. Returns null if not found.
    /// </summary>
    public static ThingDef GetFinishedWool(ThingDef rawWool)
    {
      RawToFinishedMap.TryGetValue(rawWool, out var finished);
      return finished;
    }
  }
}
