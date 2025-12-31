using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProductionExpanded
{
  public class CompProperties_ResourceProcessor : CompProperties
  {
    // How many units of material this building can hold by default
    public int maxCapacity = 50;
    public int cycles = 1;
    public bool usesOnTexture = false;
    public bool hasIdlePowerCost = false;
    public ThingDef input = null;
    public ThingDef output = null;

    // CONSTRUCTOR IS REQUIRED!
    // This tells RimWorld which Comp class to create
    public CompProperties_ResourceProcessor()
    {
      this.compClass = typeof(CompResourceProcessor);
    }
  }

  public class CompResourceProcessor : ThingComp
  {
    private CompProperties_ResourceProcessor Props => (CompProperties_ResourceProcessor)props;
    private bool isProcessing = false;
    private bool isFinished = false;
    private bool isWaitingForCycleInteraction = false;
    private bool inspectStringDirty = true;
    private bool previousCanContinue = false;
    private int progressTicks = 0;
    private int totalTicksPerCycle = 0;
    private int cycles = 1;
    private int currentCycle = 0;
    private int inputCount = 0;
    private int outputCount = 0;
    private int capacityRemaining = 0;
    private string cachedInfoString = null;
    private ThingDef inputType = null;
    private ThingDef outputType = null;
    private CompPowerTrader powerTrader = null;
    private CompRefuelable refuelable = null;
    private CompHeatPusher heatPusher = null;
    private CompGlower glower = null;
    private MapComponent_ProcessorTracker processorTracker = null;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
      base.PostSpawnSetup(respawningAfterLoad);
      powerTrader = parent.GetComp<CompPowerTrader>();
      refuelable = parent.GetComp<CompRefuelable>();
      heatPusher = parent.GetComp<CompHeatPusher>();
      glower = parent.GetComp<CompGlower>();
      capacityRemaining = Props.maxCapacity;
      processorTracker = parent.Map?.GetComponent<MapComponent_ProcessorTracker>();
      processorTracker.allProcessors.Add((Building_WorkTable)parent);
      processorTracker.processorsNeedingFill.Add((Building_WorkTable)parent);

      // Validate building configuration - log once on spawn
      if (processorTracker == null)
      {
        Log.Error(
          "[Production Expanded] {parent.def.defName} is not on any map (MapComponent_ProcessorTracker is null)"
        );
      }

      // Initialize glower state
      UpdateGlower();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
      previousMap
        ?.GetComponent<MapComponent_ProcessorTracker>()
        ?.allProcessors.Remove((Building_WorkTable)parent);

      base.PostDestroy(mode, previousMap);
      // Clean up the glower when building is destroyed
      if (glower != null && previousMap != null)
      {
        glower.UpdateLit(previousMap);
      }
    }

    private void UpdateGlower()
    {
      if (glower != null && parent.Spawned)
      {
        glower.UpdateLit(parent.Map);
      }
    }

    public override void CompTickRare()
    {
      base.CompTickRare();
      if (isProcessing)
      {
        bool currentCanContinue = CanContinueProcessing();

        // Update glower if power/fuel state changed
        if (currentCanContinue != previousCanContinue)
        {
          previousCanContinue = currentCanContinue;
          UpdateGlower();
        }

        if (currentCanContinue)
        {
          if (heatPusher != null)
          {
            heatPusher.enabled = true;
          }
          if (getCapacityRemaining() > 0)
          {
            processorTracker.processorsNeedingFill.Add((Building_WorkTable)parent);
          }
          else
          {
            processorTracker.processorsNeedingFill.Remove((Building_WorkTable)parent);
          }
          inspectStringDirty = true;
          progressTicks += 250;
          if (refuelable != null)
          {
            // equivalent of *60,000 / 250 aka fuelconsumption rate per tick per day *250 as this method gets called every TickRare
            refuelable.ConsumeFuel(refuelable.Props.fuelConsumptionRate / 240);
          }
          else if (powerTrader != null)
          {
            powerTrader.PowerOutput = -powerTrader.Props.PowerConsumption;
          }
          if (progressTicks >= totalTicksPerCycle)
          {
            CompleteProcessingCycle();
          }
        }
        else
        {
          if (heatPusher != null)
          {
            heatPusher.enabled = false;
          }
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
          if (powerTrader != null && Props.hasIdlePowerCost)
          {
            powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;
          }
          processorTracker.processorsNeedingFill.Remove((Building_WorkTable)parent);
        }
      }
      else
      {
        if (CanContinueProcessing())
        {
          processorTracker.processorsNeedingFill.Add((Building_WorkTable)parent);
        }
        else
        {
          processorTracker.processorsNeedingFill.Remove((Building_WorkTable)parent);
        }
        if (heatPusher != null)
        {
          heatPusher.enabled = false;
        }
        inspectStringDirty = true;
        if (powerTrader != null && Props.hasIdlePowerCost)
        {
          powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;
        }
      }
    }

    public void StartNextCycle()
    {
      isWaitingForCycleInteraction = false;
      processorTracker.processorsNeedingCycleStart.Remove((Building_WorkTable)parent);

      if (getCapacityRemaining() > 0 && CanContinueProcessing())
      {
        processorTracker.processorsNeedingFill.Add((Building_WorkTable)parent);
      }

      UpdateGlower();
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
        Log.Warning($"[Production Expanded] Tried adding items to Finished processor");
        return;
      }

      capacityRemaining -= inputCount;
      if (capacityRemaining < 0)
      {
        Log.Warning(
          $"[Production Expanded] Atttempted to add items to {parent.def.defName} with insufficient capacity"
        );
      }

      // Update fill list based on new capacity
      if (getCapacityRemaining() > 0 && CanContinueProcessing())
      {
        processorTracker.processorsNeedingFill.Add((Building_WorkTable)parent);
      }
      else
      {
        processorTracker.processorsNeedingFill.Remove((Building_WorkTable)parent);
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

        return; // Keep progressTicks unchanged
      }
      if (heatPusher != null)
      {
        heatPusher.enabled = true;
      }
      isProcessing = true;
      parent.DirtyMapMesh(parent.Map);
      UpdateGlower();
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
      processorTracker.processorsNeedingFill.Remove((Building_WorkTable)parent);
      if (currentCycle >= cycles)
      {
        isProcessing = false;
        parent.DirtyMapMesh(parent.Map);
        UpdateGlower();
        isWaitingForCycleInteraction = false;
        processorTracker.processorsNeedingEmpty.Add((Building_WorkTable)parent);
        isFinished = true;
        inputCount = 0;
        inputType = null;
        currentCycle = 0;
        totalTicksPerCycle = 0;
        cycles = 0;
        return;
      }
      processorTracker.processorsNeedingCycleStart.Add((Building_WorkTable)parent);
      isWaitingForCycleInteraction = true;
      UpdateGlower();
    }

    public void EmptyBuilding()
    {
      inspectStringDirty = true;
      if (isFinished)
      {
        if (outputType == null)
        {
          Log.Error(
            $"[Production Expanded] {parent.def.defName} tried to empty but outputType is null! isFinished={isFinished}, outputCount={outputCount}"
          );
          isFinished = false;
          return;
        }

        // Create the output item
        Thing item = ThingMaker.MakeThing(outputType);
        item.stackCount = outputCount;

        // Spawn it at the building's interaction cell
        GenSpawn.Spawn(item, parent.InteractionCell, parent.Map);

        // Reset state
        isFinished = false;
        if (heatPusher != null)
        {
          heatPusher.enabled = false;
        }
        isProcessing = false;
        isWaitingForCycleInteraction = false;
        outputType = null;
        outputCount = 0;
        capacityRemaining = Props.maxCapacity;

        if (CanContinueProcessing())
        {
          processorTracker.processorsNeedingFill.Add((Building_WorkTable)parent);
        }
        processorTracker.processorsNeedingEmpty.Remove((Building_WorkTable)parent);
        UpdateGlower();
      }
      else
      {
        Log.Warning(
          "[Production Expanded] tried to empty items of {parent.def.defName} despite it not being finished"
        );
      }
    }

    public override string CompInspectStringExtra()
    {
      if (inspectStringDirty)
      {
        if (isFinished)
        {
          inspectStringDirty = false;
          cachedInfoString = "Finished. Waiting for colonist to extract materials";
          return cachedInfoString;
        }
        // If idle, show that
        if (!isProcessing)
        {
          inspectStringDirty = false;
          cachedInfoString = "Status: Idle";
          return cachedInfoString;
        }
        // If processing, show progress
        float progressPercent = (float)progressTicks / totalTicksPerCycle;
        if (cycles > 1 && isWaitingForCycleInteraction)
        {
          cachedInfoString =
            $"{inputCount} units of {inputType?.label ?? "unknown"}\n{cycles - currentCycle} cycles remaining\nWaiting for colonist interaction to continue refining";
          inspectStringDirty = false;
          return cachedInfoString;
        }
        else if (cycles > 1)
        {
          cachedInfoString =
            $"Processing: {progressPercent:P0} ({inputCount} units of {inputType?.label ?? "unknown"})\n{cycles - currentCycle} cycles remaining";
          inspectStringDirty = false;
          return cachedInfoString;
        }
        cachedInfoString =
          $"Processing: {progressPercent:P0} ({inputCount} units of {inputType?.label ?? "unknown"})";
        inspectStringDirty = false;
      }
      return cachedInfoString;
    }

    public override void PostExposeData()
    {
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
      Scribe_Values.Look(ref outputCount, "outputCount", 0);
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
          },
        };
        yield return new Command_Action
        {
          defaultLabel = "DEBUG: Finish cycle",
          action = delegate
          {
            StartNextCycle();
          },
        };
        yield return new Command_Action
        {
          defaultLabel = "DEBUG: Finish Current Cycle",
          action = delegate
          {
            progressTicks = totalTicksPerCycle - 60;
          },
        };
        // yield return new Command_Action
        // {
        //     defaultLabel = "DEBUG: Print State",
        //     action = delegate
        //     {
        //         Log.Message($"[{parent.def.defName}] State Variables:\n" +
        //             $"  isFinished: {isFinished}\n" +
        //             $"  isProcessing: {isProcessing}\n" +
        //             $"  isWaitingForCycleInteraction: {isWaitingForCycleInteraction}\n" +
        //             $"  progressTicks: {progressTicks}/{totalTicksPerCycle}\n" +
        //             $"  currentCycle: {currentCycle}/{cycles}\n" +
        //             $"  inputCount: {inputCount}\n" +
        //             $"  outputCount: {outputCount}\n" +
        //             $"  inputType: {inputType?.defName ?? "null"}\n" +
        //             $"  outputType: {outputType?.defName ?? "null"}\n" +
        //             $"  CanContinueProcessing: {CanContinueProcessing()}");
        //     }
        // };
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
      return this.capacityRemaining;
    }

    public bool getIsReady()
    {
      return CanContinueProcessing();
    }

    public bool getIsWaitingForNextCycle()
    {
      return isWaitingForCycleInteraction;
    }

    public CompProperties_ResourceProcessor getProps()
    {
      return this.Props;
    }
  }
}
