using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
  public static class RecipeProducts_DynamicOutput_Patch
  {
    private static readonly Func<
      Thing,
      RecipeDef,
      Pawn,
      Precept_ThingStyle,
      ThingStyleDef,
      int?,
      Thing
    > PostProcessProduct = AccessTools.MethodDelegate<
      Func<Thing, RecipeDef, Pawn, Precept_ThingStyle, ThingStyleDef, int?, Thing>
    >(AccessTools.Method(typeof(GenRecipe), "PostProcessProduct"));

    [HarmonyPostfix]
    public static IEnumerable<Thing> Postfix(
      IEnumerable<Thing> __result,
      RecipeDef recipeDef,
      Pawn worker,
      List<Thing> ingredients,
      Thing dominantIngredient,
      IBillGiver billGiver,
      Precept_ThingStyle precept = null,
      ThingStyleDef style = null,
      int? overrideGraphicIndex = null
    )
    {
      var settings = recipeDef.GetModExtension<RecipeExtension_Processor>();
      if (settings == null || !settings.useDynamicOutput)
      {
        foreach (var thing in __result)
        {
          yield return thing;
        }
        yield break;
      }

      ThingDef ingredient = dominantIngredient?.def;
      if (ingredient != null)
      {
        ThingDef actuallProduct = RawToFinishedRegistry.GetFinished(ingredient);
        if (actuallProduct == null)
        {
          Log.Error(
            $"[Production Expanded] attempted to make dynamic recipe output but the ingredient was not registered in the RawToFinishedRegistry"
          );
          yield break;
        }
        Thing productThing = ThingMaker.MakeThing(actuallProduct, null);

        float efficiency = (
          (recipeDef.efficiencyStat != null) ? worker.GetStatValue(recipeDef.efficiencyStat) : 1f
        );
        if (recipeDef.workTableEfficiencyStat != null && billGiver is Building_WorkTable thing)
        {
          efficiency *= thing.GetStatValue(recipeDef.workTableEfficiencyStat);
        }

        if (recipeDef.products == null || recipeDef.products.Count == 0)
        {
          Log.Error($"[Production Expanded] Recipe {recipeDef.defName} has no products!");
          yield break;
        }

        productThing.stackCount = Mathf.CeilToInt((float)recipeDef.products[0].count * efficiency);

        yield return PostProcessProduct(
          productThing,
          recipeDef,
          worker,
          precept,
          style,
          overrideGraphicIndex
        );

        yield break;
      }
      else
      {
        Log.Error(
          $"[Production Expanded] attempted to make dynamic recipe product but the dominantIngredient was invalid {(ingredient != null ? ingredient.defName : "ingredient was null")}"
        );
        yield break;
      }
    }
  }
}
