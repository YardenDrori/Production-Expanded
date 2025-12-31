using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Utility class for ProcessBill operations including clipboard support.
  /// </summary>
  public static class ProcessBillUtility
  {
    /// <summary>
    /// Clipboard for copy/paste functionality between buildings.
    /// </summary>
    public static ProcessBill Clipboard { get; set; }

    /// <summary>
    /// Opens a FloatMenu to select repeat mode for a bill.
    /// </summary>
    public static void MakeRepeatModeConfigFloatMenu(ProcessBill bill)
    {
      var options = new System.Collections.Generic.List<FloatMenuOption>
      {
        new FloatMenuOption("Do Forever", delegate
        {
          bill.repeatMode = ProcessRepeatMode.Forever;
        }),
        new FloatMenuOption("Do X Times", delegate
        {
          bill.repeatMode = ProcessRepeatMode.DoXTimes;
          if (bill.x <= 0) bill.x = 1;
        })
      };

      // Only show "Do Until X" if there's a countable output
      if (bill.processDef?.outputDef != null)
      {
        options.Add(new FloatMenuOption("Do Until You Have X", delegate
        {
          bill.repeatMode = ProcessRepeatMode.DoUntillX;
          if (bill.x <= 0) bill.x = 10;
        }));
      }

      Find.WindowStack.Add(new FloatMenu(options));
    }

    /// <summary>
    /// Gets the display label for a repeat mode.
    /// </summary>
    public static string GetRepeatModeLabel(ProcessRepeatMode mode)
    {
      switch (mode)
      {
        case ProcessRepeatMode.Forever:
          return "Forever";
        case ProcessRepeatMode.DoXTimes:
          return "Do X times";
        case ProcessRepeatMode.DoUntillX:
          return "Do until X";
        default:
          return "Unknown";
      }
    }

    /// <summary>
    /// Gets the repeat info text for display in bill row.
    /// </summary>
    public static string GetRepeatInfoText(ProcessBill bill)
    {
      switch (bill.repeatMode)
      {
        case ProcessRepeatMode.Forever:
          return "Forever";
        case ProcessRepeatMode.DoXTimes:
          return $"{bill.x}x";
        case ProcessRepeatMode.DoUntillX:
          if (bill.Parent != null && bill.Parent is Thing thing && thing.Map != null && bill.processDef?.outputDef != null)
          {
            int current = thing.Map.resourceCounter.GetCount(bill.processDef.outputDef);
            return $"{current} / {bill.x}";
          }
          return $"Until {bill.x}";
        default:
          return "";
      }
    }
  }
}
