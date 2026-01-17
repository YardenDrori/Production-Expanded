using HarmonyLib;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Main mod class that initializes Harmony patches.
  /// Uses the Mod class constructor instead of [StaticConstructorOnStartup] to ensure
  /// patches are applied BEFORE def loading and resolution, which is required for
  /// our DefGenerator.GenerateImpliedDefs_PreResolve patch to work.
  ///
  /// Initialization order:
  /// 1. Mod constructor (this class) - Harmony patches applied here
  /// 2. XML defs loaded
  /// 3. DefGenerator.GenerateImpliedDefs_PreResolve() - our patch injects raw leather/wool defs
  /// 4. Cross-references resolved - thingCategories populated
  /// 5. DefGenerator.GenerateImpliedDefs_PostResolve()
  /// 6. [StaticConstructorOnStartup] classes run
  /// </summary>
  public class ProductionExpandedMod : Mod
  {
    public ProductionExpandedMod(ModContentPack content) : base(content)
    {
      // Initialize Harmony early so patches are applied before def loading
      var harmony = new Harmony("blacksparrow.productionexpanded");
      harmony.PatchAll();
    }
  }

  /// <summary>
  /// Late initialization for tasks that need to run after all defs are loaded.
  /// </summary>
  [StaticConstructorOnStartup]
  public static class ProductionExpandedStartup
  {
    static ProductionExpandedStartup()
    {
      // Reset resource counter to include our dynamically generated defs
      RimWorld.ResourceCounter.ResetDefs();

      Log.Message("[Production Expanded] Mod initialized successfully!");
    }
  }
}
