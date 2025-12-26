using RimWorld;
using Verse;

namespace ProductionExpanded
{
    public class CompProperties_ResourceProcessor : CompProperties
    {
        // How many units of material this building can hold
        public int maxCapacity = 50;

        // CONSTRUCTOR IS REQUIRED!
        // This tells RimWorld which Comp class to create
        public CompProperties_ResourceProcessor()
        {
            this.compClass = typeof(CompResourceProcessor);
        }
    }

    public class CompResourceProcessor : ThingComp
    {
        private CompProperties_ResourceProcessor Props =>
            (CompProperties_ResourceProcessor)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
        }

        public override string CompInspectStringExtra()
        {
            return "Processor Status: Idle";
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SAVE/LOAD - This will be needed in Step 2
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Called when saving/loading the game.
        ///
        /// CRITICAL: Any state that needs to persist across saves must be
        /// serialized here using Scribe_* methods.
        ///
        /// Think of this like writing to/reading from a file, but RimWorld handles
        /// the actual file I/O for you.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            // In Step 2, we'll save/load our state here
            // For now, nothing to save (we have no state)
        }
    }
}
