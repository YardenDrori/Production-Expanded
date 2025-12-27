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
        private bool isWaitingForCycleInteraction = false;
        private bool inspectStringDirty = true;
        private int progressTicks = 0;
        private int totalTicksPerCycle = 0;
        private int cycles = 1;
        private int currentCycle = 0;
        private int inputCount = 0;
        private int outputCount = 0;
        private string cachedInfoString = null;
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
                    inspectStringDirty = true;
                    progressTicks += 250;
                    if (refuelable != null)
                    {
                        refuelable.ConsumeFuel(refuelable.Props.fuelConsumptionRate / 60000 * 250);
                    }
                    else
                    {
                        powerTrader.PowerOutput = -powerTrader.Props.PowerConsumption;
                    }
                    //temp complete processing
                    if (progressTicks >= totalTicksPerCycle)
                    {
                        CompleteProcessingCycle();
                    }
                }
                else
                {
                    inspectStringDirty = true;
                    // remove progress if the building is unfueled/powered
                    if (progressTicks > 750)
                    {
                        progressTicks -= 750;
                    }
                    else
                    {
                        progressTicks = 0;
                    }
                    if (powerTrader != null)
                    {
                        powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;
                    }
                }
            }
            else
            {
                inspectStringDirty = true;
                if (powerTrader != null)
                {
                    powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;
                }
            }
        }


        public void StartNextCycle()
        {
            isWaitingForCycleInteraction = false;
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

            if (isWaitingForCycleInteraction)
            {
                return false; //waiting for cycle
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
            inspectStringDirty = true;
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
            isWaitingForCycleInteraction = false;
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
            inspectStringDirty = true;
            currentCycle++;
            progressTicks = 0;
            if (currentCycle >= cycles)
            {
                isProcessing = false;
                isWaitingForCycleInteraction = false;
                isFinished = true;
                inputCount = 0;
                inputType = null;
                currentCycle = 0;
                totalTicksPerCycle = 0;
                cycles = 0;
                return;
            }
            isWaitingForCycleInteraction = true;
        }

        public void EmptyBuilding()
        {
            inspectStringDirty = true;
            if (isFinished)
            {
                // Create the output item
                Thing item = ThingMaker.MakeThing(outputType);
                item.stackCount = outputCount;

                // Spawn it at the building's interaction cell
                GenSpawn.Spawn(item, parent.InteractionCell, parent.Map);

                // Reset state
                isFinished = false;
                isWaitingForCycleInteraction = false;
                outputType = null;
                outputCount = 0;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (inspectStringDirty)
            {
                // If idle, show that
                if (!isProcessing)
                {
                    inspectStringDirty = false;
                    cachedInfoString = "Furnace Status: Idle";
                    return cachedInfoString;
                }

                // If processing, show progress
                float progressPercent = (float)progressTicks / totalTicksPerCycle;
                if (cycles > 1 && isWaitingForCycleInteraction)
                {
                    cachedInfoString = $"Processing: {progressPercent:P0} ({inputCount} units of {inputType?.label ?? "unknown"})\nCycle: {currentCycle} of {cycles}\nWaiting for colonist interaction to continue refining";
                    inspectStringDirty = false;
                    return cachedInfoString;
                }
                else if (cycles > 1)
                {
                    cachedInfoString = $"Processing: {progressPercent:P0} ({inputCount} units of {inputType?.label ?? "unknown"})\nCycle: {currentCycle} of {cycles}";
                    inspectStringDirty = false;
                    return cachedInfoString;
                }
                cachedInfoString = $"Processing: {progressPercent:P0} ({inputCount} units of {inputType?.label ?? "unknown"})";
                inspectStringDirty = false;
            }
            return cachedInfoString;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            base.PostExposeData();

            // Save/load all our state variables
            Scribe_Values.Look(ref isProcessing, "isProcessing", false);
            Scribe_Values.Look(ref isFinished, "isFinished", false);
            Scribe_Values.Look(ref isWaitingForCycleInteraction, "isWaitingForCycleInteraction", false);
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
            if (DebugSettings.ShowDevGizmos)
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
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Finish cycle",
                    action = delegate
                    {
                        StartNextCycle();
                    }
                };
            }
        }

        public bool getIsProcessing()
        {
            return this.isProcessing;
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
