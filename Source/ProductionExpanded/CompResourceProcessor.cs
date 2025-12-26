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
        public int cycles = 1;

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
        private int totalTicksPerCycle = 0;
        private int cycles = 1;
        private int currentCycle = 0;
        private int inputCount = 0;
        private ThingDef inputType = null;
        private ThingDef outputType = null;

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
                if (progressTicks >= totalTicksPerCycle)
                {
                    CompleteProcessingCycle();
                }
            }
            else
            {
                //todo
            }
        }

        public void CompleteProcessingCycle()
        {
            currentCycle++;
            progressTicks = 0;
            if (currentCycle >= cycles)
            {
                isProcessing = false;
                inputCount = 0;
                inputType = null;
                outputType = null;
                currentCycle = 0;
                totalTicksPerCycle = 0;
            }
        }

        public override string CompInspectStringExtra()
        {
            // If idle, show that
            if (!isProcessing)
                return "Furnace Status: Idle";

            // If processing, show progress
            float progressPercent = (float)progressTicks / totalTicksPerCycle;
            if (cycles > 1)
            {
                return $"Processing: {progressPercent:P0} ({inputCount} units of {inputType?.label ?? "unknown"})\nCycle: {currentCycle} of {cycles}";
            }
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
                        totalTicksPerCycle = 300;
                        cycles = 3;
                        Log.Message("Started test processing!");
                    }
                };
            }
        }
    }
}
