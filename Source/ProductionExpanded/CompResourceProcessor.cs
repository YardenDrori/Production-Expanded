using RimWorld;
using Verse;

namespace ProductionExpanded
{
    public class CompProperties_ResourceProcessor : CompProperties
    {
        // How many units of material this building can hold by default
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
        private compproperties_resourceprocessor props =>
            (compproperties_resourceprocessor)props;

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

        public override void PostExposeData()
        {
            base.PostExposeData();

            // In Step 2, we'll save/load our state here
            // For now, nothing to save (we have no state)
        }
    }
}
