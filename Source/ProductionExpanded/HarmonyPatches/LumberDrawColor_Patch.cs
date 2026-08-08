using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// The plank sprite is pre-coloured, so lumber is meant to draw untinted (graphicData.color is
  /// left at white). A stack that carries a draw colour of its own — a stuff reference, a
  /// CompColorable, an ideo style — gets that colour multiplied over the sprite and renders much
  /// darker than its neighbours, which is what wood-as-stuff (a brown) does.
  ///
  /// Rather than chase every path that can spawn wood, pin the draw colour to the def's own.
  /// </summary>
  [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.DrawColor), MethodType.Getter)]
  public static class LumberDrawColor_Patch
  {
    [HarmonyPostfix]
    public static void Postfix(ThingWithComps __instance, ref Color __result)
    {
      if (__instance.def != ThingDefOf.WoodLog || __instance.def.graphicData == null)
      {
        return;
      }

      Color defColor = __instance.def.graphicData.color;
      if (__result == defColor)
      {
        return;
      }

      // Worth knowing about: something handed this stack a colour we didn't set. Once per source.
      Log.WarningOnce(
        $"[Production Expanded] lumber stack asked to draw as {__result} instead of {defColor} "
          + $"(stuff={__instance.Stuff?.defName ?? "none"}, "
          + $"style={__instance.StyleDef?.defName ?? "none"}); pinning to the def colour.",
        __instance.def.shortHash ^ 0x10C0
      );

      __result = defColor;
    }
  }
}
