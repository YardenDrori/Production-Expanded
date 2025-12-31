using RimWorld;
using Verse;

namespace ProductionExpanded
{
  [DefOf]
  public static class JobDefOf_ProductionExpanded
  {
    public static JobDef PE_FillProcessor;
    public static JobDef PE_EmptyProcessor;
    public static JobDef PE_StartNextProcessorCycle;

    static JobDefOf_ProductionExpanded()
    {
      DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_ProductionExpanded));
    }
  }
}
