using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Applies <see cref="VariableOutputByIngredient"/>: rescales a product stack by the market
  /// value of the ingredient actually used, relative to the recipe's baseline ingredient.
  ///
  /// When both ingredients are crops, the ratio is further divided by how fast each plant
  /// yields. Without that, a slow-growing but valuable crop would be rewarded twice — once
  /// for its price, and again for the effort already priced into it.
  /// </summary>
  [HarmonyPatch(typeof(GenRecipe), "PostProcessProduct")]
  public static class GenRecipe_PostProcessProduct_Patch
  {
    // harvestedThingDef -> the plant that yields it. Built once, on first use, after defs
    // have loaded; the alternative is a full DefDatabase scan on every crafted item.
    private static Dictionary<ThingDef, ThingDef> plantByHarvest;

    [HarmonyPostfix]
    public static void Postfix(RecipeDef recipeDef, ref Thing __result)
    {
      if (__result == null)
      {
        return;
      }

      VariableOutputByIngredient ext =
        recipeDef?.GetModExtension<VariableOutputByIngredient>();
      if (ext?.baseline == null)
      {
        return;
      }

      CompIngredients comp = __result.TryGetComp<CompIngredients>();
      if (comp == null || comp.ingredients.NullOrEmpty())
      {
        return;
      }

      ThingDef used = comp.ingredients.FirstOrDefault(x =>
        ext.exclusions == null || !ext.exclusions.Contains(x)
      );
      if (used == null)
      {
        return;
      }

      float baselineValue = ext.baseline.BaseMarketValue;
      if (baselineValue <= 0f)
      {
        Log.WarningOnce(
          $"[Production Expanded] {recipeDef.defName} has baseline {ext.baseline.defName} "
            + "with no market value; skipping variable output.",
          recipeDef.shortHash
        );
        return;
      }

      float usedValue = ext.useCap
        ? Mathf.Min(used.BaseMarketValue, baselineValue * ext.capPriceInfluenceMultiplier)
        : used.BaseMarketValue;

      float scale = usedValue / baselineValue;

      ThingDef baselinePlant = PlantYielding(ext.baseline);
      ThingDef usedPlant = PlantYielding(used);
      if (baselinePlant != null && usedPlant != null && usedPlant != ThingDefOf.Plant_Ambrosia)
      {
        float baselineRate = YieldPerDay(baselinePlant);
        float usedRate = YieldPerDay(usedPlant);
        if (usedRate > 0f)
        {
          scale *= baselineRate / usedRate;
        }
      }

      __result.stackCount = Mathf.Max(1, (int)(__result.stackCount * scale));
    }

    private static float YieldPerDay(ThingDef plant) =>
      plant.plant.growDays > 0f ? plant.plant.harvestYield / plant.plant.growDays : 0f;

    private static ThingDef PlantYielding(ThingDef harvested)
    {
      if (plantByHarvest == null)
      {
        plantByHarvest = new Dictionary<ThingDef, ThingDef>();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
          ThingDef yielded = def.plant?.harvestedThingDef;
          if (yielded != null && !plantByHarvest.ContainsKey(yielded))
          {
            plantByHarvest[yielded] = def;
          }
        }
      }

      return plantByHarvest.TryGetValue(harvested, out ThingDef plant) ? plant : null;
    }
  }
}
