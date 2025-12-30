using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded.HarmonyPatches
{
  [HarmonyPatch(
    typeof(CompHasGatherableBodyResource),
    nameof(CompHasGatherableBodyResource.Gathered)
  )]
  public static class ShearProducts_Patch
  {
    [HarmonyPrefix]
    public static bool Prefix(CompHasGatherableBodyResource __instance, Pawn doer)
    {
      ThingDef finishedWool = Traverse
        .Create(__instance)
        .Property("ResourceDef")
        .GetValue<ThingDef>();
      ThingDef rawWool = RawWoolDefGenerator.GetRawWool(finishedWool);
      if (rawWool != null)
      {
        //extract internal values
        bool active = Traverse.Create(__instance).Property("Active").GetValue<bool>();
        ThingWithComps parent = Traverse
          .Create(__instance)
          .Property("Parent")
          .GetValue<ThingWithComps>();
        int ResourceAmount = Traverse.Create(__instance).Property("ResourceAmount").GetValue<int>();

        if (!active)
        {
          Log.Error(doer?.ToString() + " gathered body resources while not Active: " + parent);
        }
        if (!Rand.Chance(doer.GetStatValue(StatDefOf.AnimalGatherYield)))
        {
          MoteMaker.ThrowText(
            (doer.DrawPos + parent.DrawPos) / 2f,
            parent.Map,
            "TextMote_ProductWasted".Translate(),
            3.65f
          );
        }
        else
        {
          int num = GenMath.RoundRandom((float)ResourceAmount * __instance.Fullness);
          while (num > 0)
          {
            int num2 = Mathf.Clamp(num, 1, rawWool.stackLimit);
            num -= num2;
            Thing thing = ThingMaker.MakeThing(rawWool);
            thing.stackCount = num2;
            GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near);
          }
        }
        Traverse.Create(__instance).Field("fullness").SetValue(0f);
        return false;
      }
      Log.Warning(
        $"[Production Expanded] {finishedWool.defName} did not generate corresponding raw wool variant"
      );
      return true;
    }
  }
}
