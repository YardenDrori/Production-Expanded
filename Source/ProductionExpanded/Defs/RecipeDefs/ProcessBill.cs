using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProductionExpanded
{
  public enum ProcessRepeatMode
  {
    Forever,
    DoUntillX,
    DoXTimes,
  }

  public enum ActionWithOutput
  {
    DropOnFloor,
    HaulToBestStockpile,
    HaulToSpecificStockpile,
  }

  public enum AllowedWorker
  {
    Any,
    Slave,
    Mech,
    SpecificPawn,
  }

  public class ProcessBill : IExposable
  {
    public Thing Parent = null;
    public ProcessDef processDef = null;
    public ProcessFilter processFilter = null;
    public ProcessRepeatMode repeatMode = ProcessRepeatMode.Forever;
    public ActionWithOutput actionWithOutput = ActionWithOutput.HaulToBestStockpile;
    public AllowedWorker allowedWorker = AllowedWorker.Any;
    public Zone_Stockpile destinationStockpile = null;
    public Pawn worker = null;
    public string label = null;
    public bool isSuspended = false;
    public int ingredientSearchRadius = 9999;
    public int x = 10;

    public bool IsFulfilled()
    {
      if (isSuspended)
        return true;
      if (repeatMode == ProcessRepeatMode.Forever)
      {
        return false;
      }
      if (repeatMode == ProcessRepeatMode.DoXTimes)
      {
        // x represents remaining times to do. If <= 0, we are done.
        return x <= 0;
      }
      if (repeatMode == ProcessRepeatMode.DoUntillX)
      {
        if (Parent == null || Parent.Map == null)
          return false;

        // Determine what to count
        ThingDef thingToCount = processDef.outputDef;

        // If dynamic output (null outputDef), we can't easily count specific items
        // without user selecting a target. For now, we assume this mode isn't valid so we treat as unfulfilled.
        if (thingToCount == null)
        {
          Log.Error("tried doing a do untill x bill for generic recipe");
          return false;
        }

        int currentCount = Parent.Map.resourceCounter.GetCount(thingToCount);
        return currentCount >= x;
      }
      return false;
    }

    public void ExposeData()
    {
      Scribe_Defs.Look(ref processDef, "processDef");
      Scribe_Deep.Look(ref processFilter, "processFilter");
      Scribe_Values.Look(ref repeatMode, "repeatMode", ProcessRepeatMode.Forever);
      Scribe_Values.Look(ref x, "x", 10);

      // Ensure filter is initialized if loading failed or new
      if (Scribe.mode == LoadSaveMode.PostLoadInit)
      {
        if (processFilter == null && processDef != null)
        {
          processFilter = new ProcessFilter();
          processFilter.processDef = processDef; // Ensure link
        }
      }
    }
  }
}
