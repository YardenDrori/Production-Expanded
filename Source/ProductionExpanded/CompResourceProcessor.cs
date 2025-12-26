using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProductionExpanded
{
    public class CompProperties_ResourceProcessor : CompProperties
    {
        // How many units of material this building can hold by default
        public int maxCapacity = 50;
        public ThingDef input = null;
        public ThingDef output = null;
        public int cycles = 0; // 0 - means done in 1 go 1 means need to do 1 checkups etc

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
        private bool isProcessing = false;
        private int progressTicks = 0;
        private int inputCount = 0;
        private ThingDef inputType = null;
        private ThingDef outputTyoe = null;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (isProcessing)
            {
                progressTicks += 250;

                //temp complete processing
                if (progressTicks >= 2000)
                {
                    CompleteProcessing();
                }
            }
            else
            {
                //todo
            }
        }

        public void CompleteProcessing()
        {
            isProcessing = false;
            progressTicks = 0;
            inputCount = 0;
            inputType = null;
            inputType = null;
        }

        public override string CompInspectStringExtra()
        {
            // If idle, show that
            if (!isProcessing)
                return "Furnace Status: Idle";

            // If processing, show progress
            float progressPercent = (float)progressTicks / (inputCount * 60000);
            return $"Processing: {progressPercent:P0} ({inputCount} units of {inputType?.label ?? "unknown"})";
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            // In Step 2, we'll save/load our state here
            // For now, nothing to save (we have no state)
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Only show in dev mode
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Start Processing",
                    action = delegate
                    {
                        isProcessing = true;
                        inputCount = 10;
                        inputType = ThingDefOf.Steel;
                        progressTicks = 0;
                        Log.Message("Started test processing!");
                    }
                };
            }
        }
    }
}
