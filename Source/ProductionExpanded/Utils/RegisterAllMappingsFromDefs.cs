using System.Collections.Generic;
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
    public List<ThingDef> raw;
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
      foreach (ThingDef input in mapping.raw)
      {
        RawToFinishedRegistry.Register(input, mapping.finished);
      }
    }
  }
}
