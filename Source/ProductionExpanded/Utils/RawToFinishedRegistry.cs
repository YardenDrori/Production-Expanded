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
    /// <summary>
    /// Maps raw/unprocessed defNames to their finished/processed ThingDefs.
    /// Example: "PE_RawLeather_Dog" → Leather_Dog
    /// </summary>
    private static Dictionary<string, ThingDef> RawToFinishedMap { get; } =
      new Dictionary<string, ThingDef>();

    /// <summary>
    /// Reverse mapping: finished defName → raw ThingDef.
    /// Example: "Leather_Dog" → PE_RawLeather_Dog
    /// </summary>
    private static Dictionary<string, ThingDef> FinishedToRawMap { get; } =
      new Dictionary<string, ThingDef>();

    /// <summary>
    /// Gets the number of registered mappings.
    /// </summary>
    public static int Count => RawToFinishedMap.Count;

    /// <summary>
    /// Registers a raw→finished mapping. Called by generators during startup.
    /// </summary>
    /// <param name="rawDef">The raw/unprocessed ThingDef</param>
    /// <param name="finishedDef">The finished/processed ThingDef</param>
    public static void Register(ThingDef rawDef, ThingDef finishedDef)
    {
      if (rawDef == null || finishedDef == null)
      {
        Log.Warning("[Production Expanded] RawToFinishedRegistry: Attempted to register null ThingDef");
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
