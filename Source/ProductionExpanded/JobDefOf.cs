using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
    [DefOf]
    public static class JobDefOf_ProductionExpanded
    {
        public static JobDef PE_FillProcessor;
        public static JobDef PE_EmptyProcessor;

        static JobDefOf_ProductionExpanded()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_ProductionExpanded));
        }
    }
}
