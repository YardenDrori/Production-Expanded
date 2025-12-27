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
        private bool isFinished = false;
        private int progressTicks = 0;
        private int totalTicksPerCycle = 0;
        private int cycles = 1;
        private int currentCycle = 0;
        private int inputCount = 0;
        private int outputCount = 0;
        private ThingDef inputType = null;
        private ThingDef outputType = null;
        private CompPowerTrader powerTrader = null;
        private CompRefuelable refuelable = null;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerTrader = parent.GetComp<CompPowerTrader>();
            refuelable = parent.GetComp<CompRefuelable>();

            // Validate building configuration - log once on spawn
            if (powerTrader == null && refuelable == null)
            {
                Log.Warning($"[Production Expanded] {parent.def.defName} has CompResourceProcessor but no CompPowerTrader or CompRefuelable.");
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (isProcessing)
            {

                if (CanContinueProcessing())
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
                    // remove progress if the building is unfueled/powered
                    if (progressTicks > 750)
                    {
                        progressTicks -= 750;
                    }
                    else
                    {
                        progressTicks = 0;
                    }
                }
            }
            else
            {
                //todo
            }
        }

        public bool CanContinueProcessing()
        {
            // Check power requirement if building has power
            if (powerTrader != null && !powerTrader.PowerOn)
            {
                return false; //no power
            }

            // Check fuel requirement if building has fuel
            if (refuelable != null && !refuelable.HasFuel)
            {
                return false; // no fuel
            }

            // If we get here, either:
            // - Building has power AND it's on
            // - Building has fuel AND it has fuel
            return true; // we gucci
        }

        // public void StartProcessing()
        // {
        //
        // }

        public void AddMaterials(Bill_Production bill, int inputCount)
        {
            if (bill.recipe == null)
            {
                Log.Warning("[Production Expanded] Bill doesnt have a recipe");
                return;
            }
            ProcessorRecipeDef recipe = bill.recipe as ProcessorRecipeDef;
            if (recipe == null)
            {
                Log.Error($"[Production Expanded] Bill recipe is not a ProcessorRecipeDef!");
                return;
            }

            if (isFinished)
            {
                return;
            }

            int ticksPerItem = recipe.ticksPerItem;
            if (isProcessing)
            {
                int totalTicksPassed = totalTicksPerCycle * currentCycle + progressTicks;

                // Add to existing batch
                this.inputCount += inputCount;
                this.outputCount += (int)(inputCount / recipe.ratio);

                // Recalculate total time with new amount
                totalTicksPerCycle = ticksPerItem * this.inputCount;
                if (totalTicksPerCycle < ticksPerItem * 10)
                {
                    totalTicksPerCycle = ticksPerItem * 10;
                }

                currentCycle = 0;
                while (totalTicksPassed > totalTicksPerCycle)
                {
                    totalTicksPassed -= totalTicksPerCycle;
                    currentCycle++;
                }
                progressTicks = totalTicksPassed;

                return;  // Keep progressTicks unchanged
            }
            isProcessing = true;
            progressTicks = 0;
            currentCycle = 0;
            totalTicksPerCycle = ticksPerItem * inputCount;
            if (totalTicksPerCycle < ticksPerItem * 10)
            {
                totalTicksPerCycle = ticksPerItem * 10;
            }
            this.inputType = recipe.inputType;
            this.outputType = recipe.outputType;
            this.cycles = recipe.cycles;
            this.inputCount = inputCount;
            this.outputCount = (int)(inputCount / recipe.ratio);
            // StartProcessing();
        }

        public void CompleteProcessingCycle()
        {
            currentCycle++;
            progressTicks = 0;
            if (currentCycle >= cycles)
            {
                isProcessing = false;
                isFinished = true;
                inputCount = 0;
                inputType = null;
                currentCycle = 0;
                totalTicksPerCycle = 0;
                cycles = 0;
            }
        }

        public void EmptyBuilding()
        {
            if (isFinished)
            {
                // Create the output item
                Thing item = ThingMaker.MakeThing(outputType);
                item.stackCount = outputCount;

                // Spawn it at the building's interaction cell
                GenSpawn.Spawn(item, parent.InteractionCell, parent.Map);

                // Reset state
                isFinished = false;
                outputType = null;
                outputCount = 0;
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
            base.PostExposeData();

            // Save/load all our state variables
            Scribe_Values.Look(ref isProcessing, "isProcessing", false);
            Scribe_Values.Look(ref progressTicks, "progressTicks", 0);
            Scribe_Values.Look(ref totalTicksPerCycle, "totalTicksPerCycle", 0);
            Scribe_Values.Look(ref cycles, "cycles", 1);
            Scribe_Values.Look(ref currentCycle, "currentCycle", 0);
            Scribe_Values.Look(ref inputCount, "inputCount", 0);
            Scribe_Defs.Look(ref inputType, "inputType");
            Scribe_Defs.Look(ref outputType, "outputType");
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
                        totalTicksPerCycle = 2000;
                        cycles = 3;
                        Log.Message("Started test processing!");
                    }
                };
            }
        }


        public bool getIsFinished()
        {
            return this.isFinished;
        }
        public int getCapacityRemaining()
        {
            return this.Props.maxCapacity - this.inputCount;
        }
    }
}
