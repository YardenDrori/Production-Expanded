using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProductionExpanded
{
  public class MapComponent_ProcessorTracker : MapComponent
  {
    // csharpier-ignore-start
    public HashSet<Building_WorkTable> allProcessors = new HashSet<Building_WorkTable>();
    public HashSet<Building_WorkTable> processorsNeedingFill = new HashSet<Building_WorkTable>();
    public HashSet<Building_WorkTable> processorsNeedingCycleStart = new HashSet<Building_WorkTable>();
    public HashSet<Building_WorkTable> processorsNeedingEmpty = new HashSet<Building_WorkTable>();
    // csharpier-ignore-end

    public MapComponent_ProcessorTracker(Map map)
      : base(map) { }
  }
}
