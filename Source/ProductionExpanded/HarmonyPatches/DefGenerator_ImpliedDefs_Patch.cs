using HarmonyLib;
using RimWorld;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Patches DefGenerator.GenerateImpliedDefs_PreResolve to inject our dynamically
  /// generated raw leather and wool defs BEFORE category resolution occurs.
  /// This ensures they properly integrate with ThingCategoryDef filter systems.
  /// </summary>
  [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
  public static class DefGenerator_ImpliedDefs_Patch
  {
    [HarmonyPostfix]
    public static void Postfix()
    {
      Log.Message("[Production Expanded] Generating implied defs (pre-resolve phase)...");

      int leatherCount = 0;
      foreach (var def in RawLeatherDefGenerator.ImpliedRawLeatherDefs())
      {
        DefGenerator.AddImpliedDef(def);
        leatherCount++;
      }

      int woolCount = 0;
      foreach (var def in RawWoolDefGenerator.ImpliedRawWoolDefs())
      {
        DefGenerator.AddImpliedDef(def);
        woolCount++;
      }

      Log.Message(
        $"[Production Expanded] Generated {leatherCount} raw leather defs and {woolCount} raw wool defs."
      );
    }
  }
}
