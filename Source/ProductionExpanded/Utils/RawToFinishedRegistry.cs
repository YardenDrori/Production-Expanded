using System.Collections.Generic;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Central registry for all raw→finished material mappings.
  /// All generators (leather, wool, etc.) register their mappings here.
  /// This allows ProcessDef to look up any dynamic output without
  /// knowing about specific generators.
  ///
  /// Uses defName strings as keys because ThingDef.GetHashCode() is based on
  /// shortHash, which isn't assigned until after def resolution. Since we
  /// register during GenerateImpliedDefs_PreResolve, using ThingDef directly
  /// as keys would cause all entries to hash to 0, then fail lookup later
  /// when shortHash is populated.
  /// </summary>
  public static class RawToFinishedRegistry
  {
    private static Dictionary<string, ThingDef> RawToFinishedMap { get; } =
      new Dictionary<string, ThingDef>();
    private static Dictionary<string, ThingDef> FinishedToRawMap { get; } =
      new Dictionary<string, ThingDef>();

    public static int Count => RawToFinishedMap.Count;

    public static void Register(ThingDef rawDef, ThingDef finishedDef)
    {
      if (rawDef == null || finishedDef == null)
      {
        Log.Warning(
          "[Production Expanded] RawToFinishedRegistry: Attempted to register null ThingDef"
        );
        return;
      }

      RawToFinishedMap[rawDef.defName] = finishedDef;
      FinishedToRawMap[finishedDef.defName] = rawDef;
    }

    /// <summary>
    /// Gets the finished version of a raw material.
    /// </summary>
    /// <returns>The finished ThingDef, or null if not found</returns>
    public static ThingDef GetFinished(ThingDef rawDef)
    {
      if (rawDef != null && RawToFinishedMap.TryGetValue(rawDef.defName, out var finished))
      {
        return finished;
      }
      return null;
    }

    /// <summary>
    /// Gets the raw version of a finished material.
    /// </summary>
    /// <returns>The raw ThingDef, or null if not found</returns>
    public static ThingDef GetRaw(ThingDef finishedDef)
    {
      if (finishedDef != null && FinishedToRawMap.TryGetValue(finishedDef.defName, out var raw))
      {
        return raw;
      }
      return null;
    }
  }
}
