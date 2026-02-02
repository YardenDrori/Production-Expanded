using System.Collections.Generic;
using Verse;

namespace ProductionExpanded;

public class RawMappingDef : Def
{
  public List<ThingDef> raw;
  public List<ThingDef> finished;
  public bool isCartesian = false;
}

[StaticConstructorOnStartup]
public static class RegisterAllMappingsFromDefs
{
  static RegisterAllMappingsFromDefs() => RegisterMyShit();

  private static void RegisterMyShit()
  {
    foreach (var mapping in DefDatabase<RawMappingDef>.AllDefs)
    {
      if (mapping.raw.NullOrEmpty() || mapping.finished.NullOrEmpty())
      {
        Log.Error($"[Production Expanded] {mapping.defName} has null or empty lists.");
        continue;
      }

      if (mapping.isCartesian)
      {
        foreach (ThingDef input in mapping.raw)
        {
          foreach (ThingDef output in mapping.finished)
            RawToFinishedRegistry.Register(input, output);
        }
      }
      else
      {
        if (mapping.raw.Count == mapping.finished.Count)
        {
          for (int i = 0; i < mapping.raw.Count; i++)
          {
            RawToFinishedRegistry.Register(mapping.raw[i], mapping.finished[i]);
          }
        }
        else
        {
          Log.Error(
            $"[Production Expanded] {mapping.defName} count mismatch (Raw: {mapping.raw.Count}, Finished: {mapping.finished.Count}). did you mean to enable isCartesian?"
          );
        }
      }
    }
  }
}
