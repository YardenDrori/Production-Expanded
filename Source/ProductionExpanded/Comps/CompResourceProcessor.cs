using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  public class CompProperties_ResourceProcessor : CompProperties
  {
    public int maxCapacity = 50;
    public bool usesOnTexture = false;
    public bool hasIdlePowerCost = false;

    // These props might be redundant if we strictly use RecipeDef ModExtensions, 
    // but useful for defaults or non-recipe constraints.

    public CompProperties_ResourceProcessor()
    {
      this.compClass = typeof(CompResourceProcessor);
    }
  }

  public class CompResourceProcessor : ThingComp
  {
    private CompProperties_ResourceProcessor Props => (CompProperties_ResourceProcessor)props;

    // State variables
    private bool isProcessing = false;
    private bool isFinished = false;
    private bool isWaitingForCycleInteraction = false;
    private bool previousCanContinue = false;

    // Progress
    private int progressTicks = 0;
    private int totalTicksPerCycle = 0;
    private int cycles = 1;
    private int currentCycle = 0;
    private int inputCount = 0;
    private int outputCount = 0;
    private int capacityRemaining = 0;

    private string cachedLabel = null; // Label of the thing being processed

    // Track the active vanilla bill
    private Bill_Production activeBill = null;

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

      if (!respawningAfterLoad)
      {
        capacityRemaining = Props.maxCapacity;
      }

      processorTracker = parent.Map?.GetComponent<MapComponent_ProcessorTracker>();
      if (processorTracker != null)
      {
        processorTracker.allProcessors.Add((Building_Processor)parent);
        if (getCapacityRemaining() > 0 && CanContinueProcessing())
        {
          processorTracker.processorsNeedingFill.Add((Building_Processor)parent);
        }
      }

      UpdateGlower();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
      previousMap?.GetComponent<MapComponent_ProcessorTracker>()?.allProcessors.Remove((Building_Processor)parent);
      base.PostDestroy(mode, previousMap);
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
        if (currentCanContinue != previousCanContinue)
        {
          previousCanContinue = currentCanContinue;
          UpdateGlower();
        }

        if (currentCanContinue)
        {
          if (heatPusher != null) heatPusher.enabled = true;

          if (getCapacityRemaining() > 0)
            processorTracker.processorsNeedingFill.Add((Building_Processor)parent);
          else
            processorTracker.processorsNeedingFill.Remove((Building_Processor)parent);

          progressTicks += 250;

          if (refuelable != null)
            refuelable.ConsumeFuel(refuelable.Props.fuelConsumptionRate / 240);
          else if (powerTrader != null)
            powerTrader.PowerOutput = -powerTrader.Props.PowerConsumption;

          if (progressTicks >= totalTicksPerCycle)
          {
            CompleteProcessingCycle();
          }
        }
        else
        {
          if (heatPusher != null) heatPusher.enabled = false;
          // Regress progress if unpowered
          if (progressTicks > 750) progressTicks -= 750;
          else progressTicks = 0;

          if (powerTrader != null && Props.hasIdlePowerCost)
            powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;

          processorTracker.processorsNeedingFill.Remove((Building_Processor)parent);
        }
      }
      else
      {
        // Idle state updates
        if (CanContinueProcessing())
          processorTracker.processorsNeedingFill.Add((Building_Processor)parent);
        else
          processorTracker.processorsNeedingFill.Remove((Building_Processor)parent);

        if (heatPusher != null) heatPusher.enabled = false;
        if (powerTrader != null && Props.hasIdlePowerCost)
          powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;
      }
    }

    public void StartNextCycle()
    {
      isWaitingForCycleInteraction = false;
      processorTracker.processorsNeedingCycleStart.Remove((Building_Processor)parent);
      if (getCapacityRemaining() > 0 && CanContinueProcessing())
      {
        processorTracker.processorsNeedingFill.Add((Building_Processor)parent);
      }
      UpdateGlower();
    }

    public bool CanContinueProcessing()
    {
      if (powerTrader != null && !powerTrader.PowerOn) return false;
      if (refuelable != null && !refuelable.HasFuel) return false;
      if (isWaitingForCycleInteraction) return false;
      return true;
    }

    // UPDATED: Now takes Vanilla Bill and Thing
    public void AddMaterials(Bill_Production bill, Thing ingredient, int count)
    {
      if (bill == null) return;

      // Validation against current input type
      if (isProcessing && ingredient.def != inputType) return;

      capacityRemaining -= count;
      if (capacityRemaining < 0) capacityRemaining = 0;

      // Update Fill Tracker
      if (getCapacityRemaining() > 0 && CanContinueProcessing())
        processorTracker.processorsNeedingFill.Add((Building_Processor)parent);
      else
        processorTracker.processorsNeedingFill.Remove((Building_Processor)parent);

      // Read settings from Recipe ModExtension
      var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
      int ticksPerItem = settings?.ticksPerItem ?? 2500; // Default
      int extensionCycles = settings?.cycles ?? 1;

      if (isProcessing)
      {
        // Add to existing batch
        int totalTicksPassed = totalTicksPerCycle * currentCycle + progressTicks;
        this.inputCount += count;
        // Calculate output (assume 1:1 if no dynamic mapping found, usually handled inside ProcessDef logic previously)
        // We need new logic for Output here.

        this.outputCount += count; // Simplified for now, or check Recipe products
                                   // If Recipe has products, use them. 
                                   // If Dynamic, we need to replicate 'GetOutputFor'. 
                                   // For now, assuming 1:1 raw resource mapping or based on Recipe.

        totalTicksPerCycle = ticksPerItem * this.inputCount;
        // Min time clamps...

        // Recalculate progress
        currentCycle = 0;
        while (totalTicksPassed > totalTicksPerCycle)
        {
          totalTicksPassed -= totalTicksPerCycle;
          currentCycle++;
        }
        progressTicks = totalTicksPassed;
        return;
      }

      // New Batch
      if (heatPusher != null) heatPusher.enabled = true;
      isProcessing = true;
      activeBill = bill;

      parent.DirtyMapMesh(parent.Map);
      UpdateGlower();
      isWaitingForCycleInteraction = false;
      progressTicks = 0;
      currentCycle = 0;

      this.inputType = ingredient.def;
      this.cachedLabel = ingredient.Label;

      // Determine Output
      if (bill.recipe.products != null && bill.recipe.products.Count > 0)
      {
        this.outputType = bill.recipe.products[0].thingDef;
        // Calculate ratio based on recipe
        float ratio = (float)bill.recipe.ingredients[0].GetBaseCount() / bill.recipe.products[0].count;
        this.outputCount = (int)(count / ratio);
      }
      else
      {
        // Dynamic Output
        ThingDef potentialOutput = RawToFinishedRegistry.GetFinished(ingredient.def);
        if (potentialOutput != null)
        {
          this.outputType = potentialOutput;
        }
        else
        {
          this.outputType = ingredient.def; // Fallback
        }

        float ratio = settings?.ratio ?? 1.0f;
        this.outputCount = (int)(count * ratio);
        if (this.outputCount < 1) this.outputCount = 1; // Ensure strictly positive output for now
      }

      this.cycles = extensionCycles;
      this.totalTicksPerCycle = ticksPerItem * inputCount;
      if (this.totalTicksPerCycle < ticksPerItem * 10) this.totalTicksPerCycle = ticksPerItem * 10;

      this.inputCount = count;
    }

    public void CompleteProcessingCycle()
    {
      currentCycle++;
      progressTicks = 0;
      processorTracker.processorsNeedingFill.Remove((Building_Processor)parent);

      if (currentCycle >= cycles)
      {
        isProcessing = false;
        parent.DirtyMapMesh(parent.Map);
        UpdateGlower();
        isWaitingForCycleInteraction = false;
        processorTracker.processorsNeedingEmpty.Add((Building_Processor)parent);
        isFinished = true;
        return;
      }
      processorTracker.processorsNeedingCycleStart.Add((Building_Processor)parent);
      isWaitingForCycleInteraction = true;
      UpdateGlower();
    }

    public void EmptyBuilding()
    {
      if (isFinished)
      {
        if (outputType != null)
        {
          Thing item = ThingMaker.MakeThing(outputType);
          item.stackCount = outputCount;
          GenSpawn.Spawn(item, parent.InteractionCell, parent.Map);

          // Notify Bill (Decrement count)
          if (activeBill != null)
          {
            activeBill.Notify_IterationCompleted(null, new List<Thing>());
          }
        }

        // Reset
        isFinished = false;
        if (heatPusher != null) heatPusher.enabled = false;
        isProcessing = false;
        activeBill = null;
        isWaitingForCycleInteraction = false;
        outputType = null;
        outputCount = 0;
        capacityRemaining = Props.maxCapacity;

        if (CanContinueProcessing())
          processorTracker.processorsNeedingFill.Add((Building_Processor)parent);
        processorTracker.processorsNeedingEmpty.Remove((Building_Processor)parent);
        UpdateGlower();
      }
    }

    // Standard accessors
    public bool getIsProcessing() => isProcessing;
    public bool getIsFinished() => isFinished;
    public int getCapacityRemaining() => capacityRemaining;
    public bool getIsReady() => CanContinueProcessing();
    public bool getIsWaitingForNextCycle() => isWaitingForCycleInteraction;
    public Bill_Production GetActiveBill() => activeBill;
    public ThingDef getInputItem() => inputType;
    public CompProperties_ResourceProcessor getProps() => Props;

    public override string CompInspectStringExtra()
    {
      if (!parent.Spawned) return "";

      System.Text.StringBuilder str = new System.Text.StringBuilder();

      if (isFinished)
      {
        str.AppendLine("Finished. Waiting for emptying.");
      }
      else if (isProcessing)
      {
        str.AppendLine($"Processing {cachedLabel ?? "Material"}: {(float)progressTicks / totalTicksPerCycle:P0}");
        if (cycles > 1)
        {
          str.AppendLine("Cycles remaining: " + (cycles - currentCycle));
        }
      }
      else
      {
        str.AppendLine("Status: Idle");
      }

      return str.ToString().TrimEndNewlines();
    }

    public override void PostExposeData()
    {
      base.PostExposeData();
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

      // We don't save activeBill ref strictly because it's transient in the BillStack?
      // Actually vanilla tracks bill IDs. For now, we risk losing the link on load if not handled.
      // But typically we just need to know we ARE processing. Usage of activeBill is mainly for decrementing on finish.
      // If we load and activeBill is null, we just won't decrement. Acceptable for now.
    }
  }
}
