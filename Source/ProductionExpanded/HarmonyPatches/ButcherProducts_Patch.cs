using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace ProductionExpanded.HarmonyPatches
{
  /// <summary>
  /// Harmony patch that intercepts Pawn.ButcherProducts to replace finished
  /// leather drops with raw leather versions.
  ///
  /// Patched method: Verse.Pawn.ButcherProducts(Pawn butcher, float efficiency)
  /// </summary>
  [HarmonyPatch(typeof(Pawn), nameof(Pawn.ButcherProducts))]
  public static class ButcherProducts_Patch
  {
    /// <summary>
    /// Postfix patch that runs after ButcherProducts generates items.
    /// Replaces any finished leather with corresponding raw leather.
    /// </summary>
    [HarmonyPostfix]
    public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, Pawn __instance, Pawn butcher, float efficiency)
    {
      foreach (var thing in __result)
      {
        // Check if this is a finished leather
        var rawLeather = RawToFinishedRegistry.GetRaw(thing.def);

        if (rawLeather != null)
        {
          // Replace with raw leather version
          Thing rawThing = ThingMaker.MakeThing(rawLeather);
          rawThing.stackCount = thing.stackCount;  // Keep same quantity

          if (Prefs.DevMode && Prefs.LogVerbose)
          {
            Log.Message($"[Production Expanded] Butchering {__instance.LabelShort}: Replaced {thing.stackCount}x {thing.def.label} with {rawThing.stackCount}x {rawLeather.label}");
          }

          yield return rawThing;
        }
        else
        {
          // Not a leather, return unchanged (meat, special body parts, etc.)
          yield return thing;
        }
      }
    }
  }
}
