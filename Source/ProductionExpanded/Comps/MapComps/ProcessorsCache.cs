using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProductionExpanded
{
  public class MapComponent_ProcessorTracker : MapComponent
  {
    // csharpier-ignore-start
    public HashSet<Building_Processor> allProcessors = new HashSet<Building_Processor>();
    public HashSet<Building_Processor> processorsNeedingFill = new HashSet<Building_Processor>();
    public HashSet<Building_Processor> processorsNeedingCycleStart = new HashSet<Building_Processor>();
    public HashSet<Building_Processor> processorsNeedingEmpty = new HashSet<Building_Processor>();
    // csharpier-ignore-end

    public MapComponent_ProcessorTracker(Map map)
      : base(map) { }
  }
}
