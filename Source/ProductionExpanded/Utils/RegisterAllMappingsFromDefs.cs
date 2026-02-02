using Verse;

namespace ProductionExpanded;

[StaticConstructorOnStartup]
public static class RegisterAllMappingsFromDefs
{
  /// <summary>
  /// Def for writing down the raw -> finished stuff
  /// </summary>
  public class RawMappingDef : Def
  {
    public ThingDef raw;
    public ThingDef finished;
  }

  static RegisterAllMappingsFromDefs()
  {
    RegisterMyShit();
  }

  private static void RegisterMyShit()
  {
    foreach (var mapping in DefDatabase<RawMappingDef>.AllDefs)
    {
      RawToFinishedRegistry.Register(mapping.raw, mapping.finished);
    }
  }
}
