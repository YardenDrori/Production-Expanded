using System.Collections.Generic;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Central registry for all raw→finished material mappings.
  /// All generators (leather, wool, etc.) register their mappings here.
  /// This allows ProcessDef to look up any dynamic output without
  /// knowing about specific generators.
  /// </summary>
  public static class RawToFinishedRegistry
  {
    /// <summary>
    /// Maps raw/unprocessed ThingDefs to their finished/processed versions.
    /// Example: PE_RawLeather_Dog → Leather_Dog
    /// </summary>
    public static Dictionary<ThingDef, ThingDef> RawToFinishedMap { get; } =
      new Dictionary<ThingDef, ThingDef>();

    /// <summary>
    /// Reverse mapping: finished → raw.
    /// Example: Leather_Dog → PE_RawLeather_Dog
    /// </summary>
    public static Dictionary<ThingDef, ThingDef> FinishedToRawMap { get; } =
      new Dictionary<ThingDef, ThingDef>();

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

      RawToFinishedMap[rawDef] = finishedDef;
      FinishedToRawMap[finishedDef] = rawDef;
    }

    /// <summary>
    /// Gets the finished version of a raw material.
    /// </summary>
    /// <returns>The finished ThingDef, or null if not found</returns>
    public static ThingDef GetFinished(ThingDef rawDef)
    {
      if (rawDef != null && RawToFinishedMap.TryGetValue(rawDef, out var finished))
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
      if (finishedDef != null && FinishedToRawMap.TryGetValue(finishedDef, out var raw))
      {
        return raw;
      }
      return null;
    }
  }
}
