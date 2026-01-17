using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProductionExpanded
{
  public class CompProperties_ResourceProcessor : CompProperties
  {
    public float minimumItemsPrecentageForWorkTime = 0.25f;
    public int maxCapacity = 50;
    public bool usesOnTexture = false;
    public bool keepOnTextureOnFinish = false;
    public bool hasIdlePowerCost = false;
    public bool shouldDecayOnStopped = false;
    public bool hasTempRequirements = false;
    public int maxTempC = 0;
    public int minTempC = 0;
    public int ticksToRuin = 15000;

    // Sound effects
    public SoundDef soundInput;
    public SoundDef soundStartCycle;
    public SoundDef soundExtract;

    public CompProperties_ResourceProcessor()
    {
      this.compClass = typeof(CompResourceProcessor);
    }
  }

  public class CompResourceProcessor : ThingComp, IThingHolder
  {
    private enum RuinReason
    {
      TooHot,
      TooCold,
      Paused,
      None,
    };

    private CompProperties_ResourceProcessor cachedProps;
    private CompProperties_ResourceProcessor Props => cachedProps;

    // State variables
    private bool isProcessing = false;
    private bool isFinished = false;
    private bool isWaitingForCycleInteraction = false;
    private RuinReason previousRuinReason = RuinReason.None;
    private bool isInspectStringDirty = true;
    private RuinReason isRuinReason = RuinReason.None;
    private int ruinTicks = 0;

    // State tracking for HashSet operations (avoid redundant add/remove)
    private bool cachedNeedsFill = false;
    private bool cachedNeedsEmpty = false;
    private bool cachedNeedsCycleStart = false;

    // Progress
    private int progressTicks = 0;
    private int totalTicksPerCycle = 0;
    private int cycles = 1;
    private int currentCycle = 0;
    private int inputCount = 0;
    private int outputCount = 0;
    private int capacityRemaining = 0;

    private string cachedLabel = null; // Label of the thing being processed

    private ThingOwner<Thing> ingredientContainer;

    // Track the active vanilla bill
    private Bill_Production activeBill = null;

    private ThingDef inputType = null;
    private ThingDef outputType = null;

    private CompPowerTrader powerTrader = null;
    private CompRefuelable refuelable = null;
    private CompHeatPusher heatPusher = null;
    private CompGlower glower = null;
    private MapComponent_ProcessorTracker processorTracker = null;

    private string inspectMessageCahce;

    // Standard accessors
    public bool getIsProcessing() => isProcessing;

    public bool getIsBadTemp() =>
      previousRuinReason == RuinReason.TooCold || previousRuinReason == RuinReason.TooHot;

    public bool getIsFinished() => isFinished;

    public int getCapacityRemaining() => capacityRemaining;

    public bool getIsReady() => CanContinueProcessing() == RuinReason.None;

    public bool getIsWaitingForNextCycle() => isWaitingForCycleInteraction;

    public Bill_Production GetActiveBill() => activeBill;

    public ThingDef getInputItem() => inputType;

    public CompProperties_ResourceProcessor getProps() => Props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
      base.PostSpawnSetup(respawningAfterLoad);

      // Cache props to avoid repeated casting
      cachedProps = (CompProperties_ResourceProcessor)props;

      // Initialize ingredient container
      if (ingredientContainer == null)
      {
        ingredientContainer = new ThingOwner<Thing>(this);
      }

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
        // Initialize fill state
        UpdateTrackerState(needsFill: getCapacityRemaining() > 0 && getIsReady());
      }

      UpdateGlower();
    }

    private void UpdateTrackerState(
      bool? needsFill = null,
      bool? needsEmpty = null,
      bool? needsCycleStart = null
    )
    {
      if (processorTracker == null)
        return;

      var processor = (Building_Processor)parent;

      if (needsFill.HasValue && needsFill.Value != cachedNeedsFill)
      {
        cachedNeedsFill = needsFill.Value;
        if (cachedNeedsFill)
          processorTracker.processorsNeedingFill.Add(processor);
        else
          processorTracker.processorsNeedingFill.Remove(processor);
      }

      if (needsEmpty.HasValue && needsEmpty.Value != cachedNeedsEmpty)
      {
        cachedNeedsEmpty = needsEmpty.Value;
        if (cachedNeedsEmpty)
          processorTracker.processorsNeedingEmpty.Add(processor);
        else
          processorTracker.processorsNeedingEmpty.Remove(processor);
      }

      if (needsCycleStart.HasValue && needsCycleStart.Value != cachedNeedsCycleStart)
      {
        cachedNeedsCycleStart = needsCycleStart.Value;
        if (cachedNeedsCycleStart)
          processorTracker.processorsNeedingCycleStart.Add(processor);
        else
          processorTracker.processorsNeedingCycleStart.Remove(processor);
      }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
      previousMap
        ?.GetComponent<MapComponent_ProcessorTracker>()
        ?.allProcessors.Remove((Building_Processor)parent);

      // Drop ingredients if building destroyed
      if (ingredientContainer != null && ingredientContainer.Count > 0)
      {
        ingredientContainer.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
      }

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
        isInspectStringDirty = true;
        RuinReason currentRuinReason = CanContinueProcessing();
        if (currentRuinReason != previousRuinReason)
        {
          previousRuinReason = currentRuinReason;
          UpdateGlower();
        }

        if (currentRuinReason == RuinReason.None)
        {
          if (heatPusher != null)
            heatPusher.enabled = true;

          UpdateTrackerState(needsFill: getCapacityRemaining() > 0);

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
          if (heatPusher != null)
            heatPusher.enabled = false;
          if (Props.shouldDecayOnStopped)
          {
            // Regress progress if unpowered
            if (progressTicks > 750)
              progressTicks -= 750;
            else
              progressTicks = 0;
          }
          if (Props.hasTempRequirements && currentRuinReason != RuinReason.Paused)
          {
            if (ruinTicks >= Props.ticksToRuin)
            {
              RuinBatch();
            }
            else
            {
              ruinTicks += 250;
            }
          }

          if (powerTrader != null && Props.hasIdlePowerCost)
            powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;

          UpdateTrackerState(needsFill: false);
        }
      }
      else
      {
        // Idle state updates
        UpdateTrackerState(needsFill: getIsReady());

        if (heatPusher != null)
          heatPusher.enabled = false;
        if (powerTrader != null && Props.hasIdlePowerCost)
          powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;
      }
    }

    public void StartNextCycle()
    {
      // Play cycle start sound when advancing to next cycle
      if (Props.soundStartCycle != null)
      {
        Props.soundStartCycle.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
      }
      isWaitingForCycleInteraction = false;
      isInspectStringDirty = true;
      UpdateTrackerState(
        needsCycleStart: false,
        needsFill: getCapacityRemaining() > 0 && getIsReady()
      );
      UpdateGlower();
    }

    private RuinReason CanContinueProcessing()
    {
      if (powerTrader != null && !powerTrader.PowerOn)
        return RuinReason.Paused;
      if (refuelable != null && !refuelable.HasFuel)
        return RuinReason.Paused;
      if (isWaitingForCycleInteraction)
        return RuinReason.Paused;
      if (Props.hasTempRequirements)
      {
        if (parent.AmbientTemperature > Props.maxTempC)
          return RuinReason.TooHot;
        if (parent.AmbientTemperature < Props.minTempC)
          return RuinReason.TooCold;
      }
      return RuinReason.None;
    }

    // UPDATED: Now takes Vanilla Bill and Thing
    public void AddMaterials(Bill_Production bill, Thing ingredient, int count)
    {
      // Validation: Check for null inputs
      if (bill == null)
      {
        Log.Warning("[Production Expanded] AddMaterials called with null bill");
        return;
      }

      if (ingredient == null)
      {
        Log.Error("[Production Expanded] AddMaterials called with null ingredient");
        return;
      }

      if (bill.recipe == null)
      {
        Log.Error("[Production Expanded] Bill has null recipe");
        return;
      }

      // Validation: Count must be positive
      if (count <= 0)
      {
        Log.Warning($"[Production Expanded] AddMaterials called with invalid count: {count}");
        return;
      }

      // Validation: Check capacity
      if (capacityRemaining <= 0)
      {
        Log.Warning(
          $"[Production Expanded] Processor at {parent.Position} is full, cannot add materials"
        );
        return;
      }

      // Validation: If already processing, ingredient must match current input type
      if (isProcessing && ingredient.def != inputType)
      {
        Log.Warning(
          $"[Production Expanded] Cannot add {ingredient.def.defName} to processor currently processing {inputType?.defName}"
        );
        return;
      }

      // Read settings from Recipe ModExtension
      var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
      float capacityFactor = settings?.capacityFactor ?? 1f;

      // Store ingredient in container instead of destroying it
      if (ingredientContainer == null)
      {
        ingredientContainer = new ThingOwner<Thing>(this);
      }

      // Split off the exact count we need and add to container
      Thing thingToAdd = ingredient.SplitOff(count);
      if (!ingredientContainer.TryAdd(thingToAdd, false))
      {
        Log.Error(
          $"[Production Expanded] Failed to add {count} {ingredient.def.defName} to processor container"
        );
        // Spawn the thing we couldn't add so it doesn't disappear
        GenSpawn.Spawn(thingToAdd, parent.Position, parent.Map);
      }
      else
      {
        // Play input sound when materials are successfully added
        if (Props.soundInput != null)
        {
          Props.soundInput.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        }
      }

      capacityRemaining -= (int)(count * capacityFactor);
      if (capacityRemaining < 0)
        capacityRemaining = 0;

      // Update Fill Tracker
      UpdateTrackerState(needsFill: getCapacityRemaining() > 0 && getIsReady());

      bool isDynamic = settings?.useDynamicOutput ?? false;
      int ticksPerItem = settings?.ticksPerItem ?? 2500;
      int extensionCycles = settings?.cycles ?? 1;

      if (isProcessing)
      {
        // Add to existing batch
        int totalTicksPassed = totalTicksPerCycle * currentCycle + progressTicks;
        this.inputCount += count;

        float ratio =
          settings?.ratio
          ?? (
            isDynamic
              ? 1.0f
              : (float)bill.recipe.ingredients[0].GetBaseCount() / bill.recipe.products[0].count
          );
        int additionalOutput = Mathf.Max(1, (int)(count * ratio));
        this.outputCount += additionalOutput;

        totalTicksPerCycle = ticksPerItem * this.inputCount;

        // Recalculate progress
        currentCycle = 0;
        while (totalTicksPassed > totalTicksPerCycle)
        {
          totalTicksPassed -= totalTicksPerCycle;
          currentCycle++;
        }
        progressTicks = totalTicksPassed;
        isInspectStringDirty = true;
        return;
      }

      // New Batch
      if (heatPusher != null)
        heatPusher.enabled = true;
      isProcessing = true;
      activeBill = bill;

      parent.DirtyMapMesh(parent.Map);
      UpdateGlower();
      isWaitingForCycleInteraction = false;
      progressTicks = 0;
      currentCycle = 0;

      this.inputType = ingredient.def;
      this.cachedLabel = ingredient.Label;

      if (!isDynamic)
      {
        if (bill.recipe.products != null && bill.recipe.products.Count > 0)
        {
          this.outputType = bill.recipe.products[0].thingDef;
          // Use modExtension ratio if specified, otherwise calculate from recipe
          float ratio =
            settings?.ratio
            ?? ((float)bill.recipe.ingredients[0].GetBaseCount() / bill.recipe.products[0].count);

          this.outputCount = Mathf.Max(1, (int)(count * ratio));
        }
        else
        {
          Log.Error(
            $"[Production Expanded] Static recipe {bill.recipe.defName} has no products defined!"
          );
          // Fallback or return
        }
      }
      else
      {
        // DYNAMIC: Look up in registry
        ThingDef potentialOutput = RawToFinishedRegistry.GetFinished(ingredient.def);
        if (potentialOutput != null)
        {
          this.outputType = potentialOutput;
        }
        else
        {
          this.outputType = ingredient.def;
          Log.Warning(
            $"[Production Expanded] No output mapping found for {ingredient.def.defName}"
          );
        }

        float ratio = settings?.ratio ?? 1.0f;
        this.outputCount = Mathf.Max(1, (int)(count * ratio));
      }

      this.cycles = extensionCycles;
      this.inputCount = count;
      if (count >= Props.maxCapacity * Props.minimumItemsPrecentageForWorkTime)
        this.totalTicksPerCycle = ticksPerItem * count;
      else
        this.totalTicksPerCycle =
          ticksPerItem * (int)(Props.maxCapacity * Props.minimumItemsPrecentageForWorkTime);

      isInspectStringDirty = true;

      // Validate final state
      if (this.outputCount <= 0)
      {
        Log.Error(
          $"[Production Expanded] Invalid outputCount ({this.outputCount}) calculated for {ingredient.def.defName}"
        );
        this.outputCount = 1; // Emergency fallback
      }
    }

    public void CompleteProcessingCycle()
    {
      currentCycle++;
      progressTicks = 0;
      UpdateTrackerState(needsFill: false);

      if (currentCycle >= cycles)
      {
        parent.DirtyMapMesh(parent.Map);
        isWaitingForCycleInteraction = false;
        UpdateTrackerState(needsEmpty: true);
        isFinished = true;
        isInspectStringDirty = true;
        UpdateGlower();
        return;
      }

      UpdateTrackerState(needsCycleStart: true);
      isWaitingForCycleInteraction = true;
      isInspectStringDirty = true;
      UpdateGlower();
    }

    private void RuinBatch()
    {
      if (ingredientContainer != null)
      {
        ingredientContainer.ClearAndDestroyContents();
      }
      // Reset
      isFinished = true;
      if (heatPusher != null)
        heatPusher.enabled = false;
      isProcessing = true;
      isWaitingForCycleInteraction = false;
      outputType = null;
      outputCount = 0;
      isInspectStringDirty = true;
      cycles = 1;

      UpdateTrackerState(needsFill: false, needsEmpty: true, needsCycleStart: false);
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

          // Play extract sound when items are extracted
          if (Props.soundExtract != null)
          {
            Props.soundExtract.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
          }

          // Notify Bill (Decrement count)
          if (activeBill != null)
          {
            activeBill.Notify_IterationCompleted(null, new List<Thing>());
          }
          else if (isProcessing)
          {
            Log.Error($"[Production Expanded] Completed processing but bill was removed.");
          }
        }

        // Clear stored ingredients (they've been consumed during processing)
        if (ingredientContainer != null)
        {
          ingredientContainer.ClearAndDestroyContents();
        }

        // Reset
        isFinished = false;
        if (heatPusher != null)
          heatPusher.enabled = false;
        isProcessing = false;
        activeBill = null;
        isWaitingForCycleInteraction = false;
        outputType = null;
        outputCount = 0;
        isInspectStringDirty = true;
        capacityRemaining = Props.maxCapacity;
        ruinTicks = 0;
        isRuinReason = RuinReason.None;
        previousRuinReason = RuinReason.None;

        UpdateTrackerState(needsFill: getIsReady(), needsEmpty: false);
        UpdateGlower();
      }
    }

    private void cleanInspectString()
    {
      if (!parent.Spawned)
      {
        inspectMessageCahce =
          $"Well this is awkward... \nso how is your day going? personally im decent but tbh could be better. \nI assume yours isnt that fun if you are seeing this string in game... \nWell im truly sorry about that! but think about the bright side, \natleast its more interesting than me writing \"ERROR COMP HAS NO PARENT\" right? \nwell anyway ive gotta get back to coding so cya XD";
      }
      else if (!isProcessing)
      {
        inspectMessageCahce = "";
      }
      else if (isFinished && Props.ticksToRuin > ruinTicks)
      {
        inspectMessageCahce =
          $"Finished. Waiting for colonist to extract {outputType.label} x{outputCount}";
      }
      else if (isWaitingForCycleInteraction)
      {
        inspectMessageCahce =
          $"Waiting for colonist interaction to resume processing.\ncycles remaining: {cycles - currentCycle}";
      }
      else if (isProcessing)
      {
        inspectMessageCahce =
          $"Processing {inputType.label ?? "Material"} x{inputCount}: {(float)progressTicks / totalTicksPerCycle:P0}";
        if (cycles != 1)
        {
          inspectMessageCahce += $"\ncycles remaining: {cycles - currentCycle}";
        }
      }

      if (Props.hasTempRequirements)
      {
        if (previousRuinReason == RuinReason.TooCold || previousRuinReason == RuinReason.TooHot)
        {
          if (ruinTicks < Props.ticksToRuin)
          {
            if (!string.IsNullOrEmpty(inspectMessageCahce))
              inspectMessageCahce += "\n";
            if (previousRuinReason == RuinReason.TooCold)
            {
              inspectMessageCahce += "Freezing ";
            }
            else
            {
              inspectMessageCahce += "Overheating ";
            }
            inspectMessageCahce += $"({(float)ruinTicks / Props.ticksToRuin:P0})";
          }
          else
          {
            inspectMessageCahce = $"Ruined by temperature";
          }
        }
        if (!string.IsNullOrEmpty(inspectMessageCahce))
          inspectMessageCahce += "\n";
        inspectMessageCahce += $"Ideal temperature: {Props.minTempC}~{Props.maxTempC}";
      }
      isInspectStringDirty = false;
    }

    public override string CompInspectStringExtra()
    {
      if (isInspectStringDirty)
      {
        cleanInspectString();
      }
      return inspectMessageCahce;
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
      Scribe_Values.Look(ref capacityRemaining, "capacityRemaining", Props.maxCapacity);
      Scribe_Values.Look(ref cachedLabel, "cachedLabel", null);
      Scribe_Values.Look(ref isRuinReason, "isRuinReason", RuinReason.None);
      Scribe_Values.Look(ref ruinTicks, "ruinTicks", 0);
      Scribe_Defs.Look(ref inputType, "inputType");
      Scribe_Defs.Look(ref outputType, "outputType");

      // Save reference to the bill
      Scribe_References.Look(ref activeBill, "activeBill");

      // Save ingredient container
      Scribe_Deep.Look(ref ingredientContainer, "ingredientContainer", this);

      // After loading, validate and initialize
      if (Scribe.mode == LoadSaveMode.PostLoadInit)
      {
        if (ingredientContainer == null)
        {
          ingredientContainer = new ThingOwner<Thing>(this);
        }

        if (isProcessing && activeBill == null)
        {
          Log.Warning(
            $"[Production Expanded] Processor at {parent.Position} lost its bill reference on load. Processing will complete but bill won't be decremented."
          );
        }
      }
    }

    // IThingHolder implementation
    public ThingOwner GetDirectlyHeldThings()
    {
      return ingredientContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
      ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
      foreach (Gizmo gizmo in base.CompGetGizmosExtra())
      {
        yield return gizmo;
      }

      // Only show for selected single object
      if (Find.Selector.SingleSelectedThing == parent)
      {
        yield return new Gizmo_ProcessorStatus(this);
      }
    }
  }
}
