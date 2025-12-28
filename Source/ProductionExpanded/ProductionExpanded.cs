using HarmonyLib;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Mod initialization class.
  /// RimWorld calls this when the mod loads.
  /// </summary>
  [StaticConstructorOnStartup]
  public static class ProductionExpandedMod
  {
    static ProductionExpandedMod()
    {
      // Initialize Harmony (mod framework for patching game code)
      var harmony = new Harmony("blacksparrow.productionexpanded");
      harmony.PatchAll();

      Log.Message("[Production Expanded] Mod initialized successfully!");
    }
  }
}
