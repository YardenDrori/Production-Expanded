using System.Collections.Generic;
using System.Linq;
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
    public int ticksToRuin = 9500;

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

    // State
    private bool isProcessing = false;
    private bool isFinished = false;
    private bool isWaitingForCycleInteraction = false;
    private RuinReason previousRuinReason = RuinReason.None;
    private bool isInspectStringDirty = true;
    private RuinReason isRuinReason = RuinReason.None;
    private int ruinTicks = 0;
    private int punishRareTicks = 0;
    private int prevPunishRareTicks = 0;

    // Tracker cache (avoid redundant HashSet add/remove)
    private bool cachedNeedsFill = false;
    private bool cachedNeedsEmpty = false;
    private bool cachedNeedsCycleStart = false;

    // Progress
    private int progressTicks = 0;
    private int totalTicksPerCycle = 0;
    private int cycles = 1;
    private int currentCycle = 0;

    // Output tracking
    private int highestStackOutputCount = 0;
    private ThingDef highestStackOutputDef = null;
    private int capacityRemaining = 0;
    private string cachedLabel = null;

    // Ingredient storage
    private ThingOwner<Thing> staticIngredientsContainer;
    private ThingOwner<Thing> dynamicIngredientsContainer;

    // Active bill
    private Bill_Production activeBill = null;

    // Calculated outputs
    private List<ThingDef> outputs = new List<ThingDef>();
    private List<int> outputsCount = new List<int>();

    // Cached comps
    private CompPowerTrader powerTrader = null;
    private CompRefuelable refuelable = null;
    private CompHeatPusher heatPusher = null;
    private CompGlower glower = null;
    private MapComponent_ProcessorTracker processorTracker = null;

    private string inspectMessageCahce;

    // === Public accessors ===

    public bool getIsProcessing() => isProcessing;
    public bool getIsBadTemp() =>
      previousRuinReason == RuinReason.TooCold || previousRuinReason == RuinReason.TooHot;
    public bool getIsFinished() => isFinished;
    public int getCapacityRemaining() => capacityRemaining;
    public bool getIsReady() => CanContinueProcessing() == RuinReason.None;
    public bool getIsWaitingForNextCycle() => isWaitingForCycleInteraction;
    public Bill_Production GetActiveBill() => activeBill;
    public CompProperties_ResourceProcessor getProps() => Props;
    public ThingDef GetHighestOutputDef() => highestStackOutputDef;
    public int GetHighestOutputCount() => highestStackOutputCount;

    // === Lifecycle ===

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
      base.PostSpawnSetup(respawningAfterLoad);

      cachedProps = (CompProperties_ResourceProcessor)props;

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
        UpdateTrackerState(needsFill: getCapacityRemaining() > 0 && getIsReady());
      }

      UpdateGlower();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
      previousMap
        ?.GetComponent<MapComponent_ProcessorTracker>()
        ?.allProcessors.Remove((Building_Processor)parent);

      staticIngredientsContainer?.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
      dynamicIngredientsContainer?.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);

      base.PostDestroy(mode, previousMap);
      if (glower != null && previousMap != null)
      {
        glower.UpdateLit(previousMap);
      }
    }

    // === Punishment system ===

    public void PunishProcessor()
    {
      if (prevPunishRareTicks > 60)
      {
        prevPunishRareTicks = 60;
      }
      else
      {
        prevPunishRareTicks = (int)(prevPunishRareTicks * 1.2 + 0.9f);
      }
      punishRareTicks = prevPunishRareTicks;
    }

    public void forgivePunishment()
    {
      prevPunishRareTicks = 1;
      punishRareTicks = 0;
    }

    // === Tracker state ===

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
        {
          if (punishRareTicks == 0)
            processorTracker.processorsNeedingFill.Add(processor);
        }
        else
        {
          processorTracker.processorsNeedingFill.Remove(processor);
        }
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

    private void UpdateGlower()
    {
      if (glower != null && parent.Spawned)
      {
        glower.UpdateLit(parent.Map);
      }
    }

    // === Tick ===

    public override void CompTickRare()
    {
      base.CompTickRare();
      if (punishRareTicks > 0)
      {
        punishRareTicks--;
      }
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
        UpdateTrackerState(needsFill: getIsReady());

        if (heatPusher != null)
          heatPusher.enabled = false;
        if (powerTrader != null && Props.hasIdlePowerCost)
          powerTrader.PowerOutput = -powerTrader.Props.idlePowerDraw;
      }
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

    // === Ingredient matching helpers ===

    /// <summary>
    /// Check if a ThingDef matches a ProcessorIngredient slot
    /// (either by specific thingDefs list or by category membership).
    /// </summary>
    public bool MatchesSlot(ProcessorIngredient ingredient, ThingDef def)
    {
      if (ingredient.IsSpecific)
      {
        for (int i = 0; i < ingredient.thingDefs.Count; i++)
        {
          if (ingredient.thingDefs[i] == def)
            return true;
        }
      }
      if (ingredient.IsCategory)
      {
        for (int i = 0; i < ingredient.categories.Count; i++)
        {
          if (def.IsWithinCategory(ingredient.categories[i]))
            return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Find which ingredient slot a ThingDef belongs to in the active recipe.
    /// </summary>
    public ProcessorIngredient FindMatchingSlot(ThingDef def, RecipeExtension_Processor settings = null)
    {
      if (settings == null)
        settings = activeBill?.recipe?.GetModExtension<RecipeExtension_Processor>();
      if (settings?.ingredients == null)
        return null;
      for (int i = 0; i < settings.ingredients.Count; i++)
      {
        if (MatchesSlot(settings.ingredients[i], def))
          return settings.ingredients[i];
      }
      return null;
    }

    /// <summary>
    /// How many items of this ingredient slot are currently stored in the processor.
    /// </summary>
    public int GetCurrentCountFor(ProcessorIngredient ingredient)
    {
      int count = 0;
      var container = ingredient.dynamic ? dynamicIngredientsContainer : staticIngredientsContainer;
      if (container == null)
        return 0;
      for (int i = 0; i < container.Count; i++)
      {
        if (MatchesSlot(ingredient, container[i].def))
          count += container[i].stackCount;
      }
      return count;
    }

    /// <summary>
    /// How many more items of this ingredient are needed.
    /// For fixed: count - current. For scaling: max - current.
    /// </summary>
    public int GetAmountNeeded(ProcessorIngredient ingredient)
    {
      int current = GetCurrentCountFor(ingredient);
      if (ingredient.IsFixed)
        return Mathf.Max(0, ingredient.count - current);
      int max = MaxCountOfIngredientInRecipe(ingredient);
      return Mathf.Max(0, max - current);
    }

    /// <summary>
    /// Max count of a single ingredient type that the processor can hold for the active recipe,
    /// accounting for capacity shared with other scaling ingredients.
    /// </summary>
    public int MaxCountOfIngredientInRecipe(ProcessorIngredient ingredient)
    {
      var settings = activeBill?.recipe?.GetModExtension<RecipeExtension_Processor>();
      if (settings == null)
        return 0;

      if (ingredient.IsFixed)
        return ingredient.count;

      // Subtract fixed ingredient capacity from total
      float availableCapacity = Props.maxCapacity;
      for (int i = 0; i < settings.ingredients.Count; i++)
      {
        var ing = settings.ingredients[i];
        if (ing.IsFixed)
          availableCapacity -= ing.count * ing.capacityPerItem;
      }

      // Calculate capacity cost per recipe unit across all scaling ingredients
      float capacityPerUnit = 0f;
      for (int i = 0; i < settings.ingredients.Count; i++)
      {
        var ing = settings.ingredients[i];
        if (ing.IsScaling)
          capacityPerUnit += ing.ratio * ing.capacityPerItem;
      }
      if (capacityPerUnit <= 0f)
        return 0;

      int maxUnits = (int)(availableCapacity / capacityPerUnit);
      return (int)(maxUnits * ingredient.ratio);
    }

    /// <summary>
    /// Check if all ingredient requirements are met to begin processing.
    /// Fixed ingredients must be at their required count.
    /// Scaling ingredients must have at least 1 item present.
    /// </summary>
    private bool ShouldStartProcessing(RecipeExtension_Processor settings)
    {
      if (settings?.ingredients == null)
        return false;
      for (int i = 0; i < settings.ingredients.Count; i++)
      {
        var ing = settings.ingredients[i];
        int current = GetCurrentCountFor(ing);
        if (ing.IsFixed && current < ing.count)
          return false;
        if (ing.IsScaling && current <= 0)
          return false;
      }
      return true;
    }

    // === Capacity ===

    private int CalculateCapacityForThing(Thing thing, ProcessorIngredient slot)
    {
      return (int)(thing.stackCount * (slot?.capacityPerItem ?? 1f));
    }

    // === Output calculation ===

    private void CalculateOutputs(RecipeExtension_Processor settings)
    {
      outputs.Clear();
      outputsCount.Clear();
      highestStackOutputCount = 0;
      highestStackOutputDef = null;

      if (settings?.UsesDynamicOutput == true && dynamicIngredientsContainer != null)
      {
        for (int i = 0; i < dynamicIngredientsContainer.Count; i++)
        {
          Thing input = dynamicIngredientsContainer[i];
          ThingDef finished = RawToFinishedRegistry.GetFinished(input.def);
          float ratio = settings.ratioDynamic;
          int outputCount = (int)(input.stackCount * ratio);
          outputs.Add(finished);
          outputsCount.Add(outputCount);
          if (outputCount > highestStackOutputCount)
          {
            highestStackOutputCount = outputCount;
            highestStackOutputDef = finished;
          }
        }
      }
      if (!settings?.products.NullOrEmpty() == true)
      {
        for (int i = 0; i < settings.products.Count; i++)
        {
          var product = settings.products[i];
          outputs.Add(product.thingDef);
          outputsCount.Add(product.count);
          if (product.count > highestStackOutputCount)
          {
            highestStackOutputCount = product.count;
            highestStackOutputDef = product.thingDef;
          }
        }
      }
    }

    // === Timing ===

    private void RecalculateTiming(RecipeExtension_Processor settings, bool preserveProgress)
    {
      int totalTicksPassed = 0;
      if (preserveProgress)
        totalTicksPassed = totalTicksPerCycle * currentCycle + progressTicks;

      CalculateOutputs(settings);

      int ticksPerOut = settings.ticksPerItemOut;
      int capacityUsed = Props.maxCapacity - capacityRemaining;
      float minCapacity = Props.maxCapacity * Props.minimumItemsPrecentageForWorkTime;

      if (capacityUsed >= minCapacity)
        this.totalTicksPerCycle = ticksPerOut * highestStackOutputCount;
      else
        this.totalTicksPerCycle = ticksPerOut * (int)minCapacity;

      if (preserveProgress && totalTicksPerCycle > 0)
      {
        currentCycle = 0;
        while (totalTicksPassed > totalTicksPerCycle)
        {
          totalTicksPassed -= totalTicksPerCycle;
          currentCycle++;
        }
        progressTicks = totalTicksPassed;
      }
    }

    // === Core: Add materials ===

    /// <summary>
    /// Add a single ingredient to the processor. The processor determines which slot
    /// it belongs to based on the recipe. One pawn trip = one call to this method.
    /// </summary>
    public void AddMaterials(Bill_Production bill, Thing ingredient)
    {
      if (bill == null)
      {
        Log.Warning("[Production Expanded] AddMaterials called with null bill");
        return;
      }
      if (ingredient == null || ingredient.stackCount <= 0)
      {
        Log.Warning("[Production Expanded] AddMaterials called with null/empty ingredient");
        return;
      }

      var settings = bill.recipe?.GetModExtension<RecipeExtension_Processor>();
      if (settings == null)
      {
        Log.Error($"[Production Expanded] Recipe {bill.recipe?.defName} has no RecipeExtension_Processor");
        return;
      }

      // Set active bill if not yet set
      if (activeBill == null)
        activeBill = bill;

      // Ensure containers exist
      if (dynamicIngredientsContainer == null)
        dynamicIngredientsContainer = new ThingOwner<Thing>(this);
      if (staticIngredientsContainer == null)
        staticIngredientsContainer = new ThingOwner<Thing>(this);

      // Find which recipe slot this ingredient belongs to
      ProcessorIngredient slot = FindMatchingSlot(ingredient.def, settings);
      if (slot == null)
      {
        Log.Error($"[Production Expanded] No matching ingredient slot for {ingredient.def.defName} in recipe {bill.recipe.defName}");
        GenSpawn.Spawn(ingredient, parent.Position, parent.Map);
        return;
      }

      var container = slot.dynamic ? dynamicIngredientsContainer : staticIngredientsContainer;

      // If already processing, only allow topping up scaling ingredients of matching type
      if (isProcessing)
      {
        if (!slot.IsScaling)
        {
          Log.Warning($"[Production Expanded] Cannot add fixed ingredient {ingredient.def.defName} to already-processing batch");
          GenSpawn.Spawn(ingredient, parent.Position, parent.Map);
          return;
        }
        bool hasMatchingType = false;
        for (int i = 0; i < container.Count; i++)
        {
          if (MatchesSlot(slot, container[i].def))
          {
            hasMatchingType = true;
            break;
          }
        }
        if (!hasMatchingType)
        {
          Log.Warning($"[Production Expanded] Ingredient {ingredient.def.defName} doesn't match any existing ingredient in processor");
          GenSpawn.Spawn(ingredient, parent.Position, parent.Map);
          return;
        }
      }

      // Check capacity
      int capacityNeeded = CalculateCapacityForThing(ingredient, slot);
      if (capacityRemaining < capacityNeeded)
      {
        Log.Warning($"[Production Expanded] Processor at {parent.Position} doesn't have enough capacity");
        GenSpawn.Spawn(ingredient, parent.Position, parent.Map);
        return;
      }

      // Add to container
      if (!container.TryAdd(ingredient, true))
      {
        Log.Error($"[Production Expanded] Failed to add {ingredient.def.defName} to container");
        GenSpawn.Spawn(ingredient, parent.Position, parent.Map);
        return;
      }

      // Update capacity
      capacityRemaining -= capacityNeeded;
      if (capacityRemaining < 0)
        capacityRemaining = 0;

      // Sound
      if (Props.soundInput != null)
        Props.soundInput.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

      if (isProcessing)
      {
        // Already processing — recalculate timing preserving progress
        RecalculateTiming(settings, true);
      }
      else if (ShouldStartProcessing(settings))
      {
        // All requirements met — begin processing
        isProcessing = true;
        progressTicks = 0;
        currentCycle = 0;
        cycles = settings.cycles;
        isWaitingForCycleInteraction = false;

        if (heatPusher != null)
          heatPusher.enabled = true;
        parent.DirtyMapMesh(parent.Map);
        UpdateGlower();

        RecalculateTiming(settings, false);

        if (outputs.NullOrEmpty())
        {
          Log.Error($"[Production Expanded] No outputs calculated for {bill.recipe.defName}");
          isProcessing = false;
          return;
        }
      }

      UpdateTrackerState(needsFill: capacityRemaining > 0 && getIsReady());
      isInspectStringDirty = true;
    }

    // === Cycle management ===

    public void StartNextCycle()
    {
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

    // === Ruin ===

    private void RuinBatch()
    {
      dynamicIngredientsContainer?.ClearAndDestroyContents();
      staticIngredientsContainer?.ClearAndDestroyContents();

      isFinished = true;
      if (heatPusher != null)
        heatPusher.enabled = false;
      isProcessing = true;
      isWaitingForCycleInteraction = false;
      outputs.Clear();
      outputsCount.Clear();
      isInspectStringDirty = true;
      cycles = 1;

      UpdateTrackerState(needsFill: false, needsEmpty: true, needsCycleStart: false);
      UpdateGlower();
    }

    // === Empty ===

    public void EmptyBuilding()
    {
      if (!isFinished)
        return;

      if (!outputs.NullOrEmpty())
      {
        for (int i = 0; i < outputs.Count; i++)
        {
          Thing item = ThingMaker.MakeThing(outputs[i]);
          item.stackCount = outputsCount[i];
          GenSpawn.Spawn(item, parent.InteractionCell, parent.Map);
        }
        if (Props.soundExtract != null)
        {
          Props.soundExtract.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        }
        if (activeBill != null)
        {
          activeBill.Notify_IterationCompleted(null, new List<Thing>());
        }
        else if (isProcessing)
        {
          Log.Error("[Production Expanded] Completed processing but bill was removed.");
        }
      }

      // Clear everything
      dynamicIngredientsContainer?.ClearAndDestroyContents();
      staticIngredientsContainer?.ClearAndDestroyContents();
      outputs.Clear();
      outputsCount.Clear();

      parent.DirtyMapMesh(parent.Map);
      isFinished = false;
      if (heatPusher != null)
        heatPusher.enabled = false;
      isProcessing = false;
      activeBill = null;
      isWaitingForCycleInteraction = false;
      isInspectStringDirty = true;
      capacityRemaining = Props.maxCapacity;
      ruinTicks = 0;
      isRuinReason = RuinReason.None;
      previousRuinReason = RuinReason.None;

      UpdateTrackerState(needsFill: getIsReady(), needsEmpty: false);
      UpdateGlower();
    }

    // === Inspect string ===

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
        string label = highestStackOutputDef?.label ?? "items";
        inspectMessageCahce =
          $"Finished. Waiting for colonist to extract {label} x{highestStackOutputCount}";
      }
      else if (isWaitingForCycleInteraction)
      {
        inspectMessageCahce =
          $"Waiting for colonist interaction to resume processing.\ncycles remaining: {cycles - currentCycle}";
      }
      else if (isProcessing)
      {
        string label = highestStackOutputDef?.label ?? "Material";
        float progress = totalTicksPerCycle > 0 ? (float)progressTicks / totalTicksPerCycle : 0f;
        inspectMessageCahce =
          $"Processing {label} x{highestStackOutputCount}: {progress:P0}";
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
            inspectMessageCahce = "Ruined by temperature";
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

    // === Save/Load ===

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
      Scribe_Values.Look(ref capacityRemaining, "capacityRemaining", Props.maxCapacity);
      Scribe_Values.Look(ref cachedLabel, "cachedLabel", null);
      Scribe_Values.Look(ref isRuinReason, "isRuinReason", RuinReason.None);
      Scribe_Values.Look(ref ruinTicks, "ruinTicks", 0);
      Scribe_Values.Look(ref highestStackOutputCount, "highestStackOutputCount", 0);
      Scribe_Defs.Look(ref highestStackOutputDef, "highestStackOutputDef");

      Scribe_References.Look(ref activeBill, "activeBill");

      Scribe_Deep.Look(ref dynamicIngredientsContainer, "dynamicIngredientsContainer", this);
      Scribe_Deep.Look(ref staticIngredientsContainer, "staticIngredientsContainer", this);

      Scribe_Collections.Look(ref outputs, "outputs", LookMode.Def);
      Scribe_Collections.Look(ref outputsCount, "outputsCount", LookMode.Value);

      if (Scribe.mode == LoadSaveMode.PostLoadInit)
      {
        if (dynamicIngredientsContainer == null)
          dynamicIngredientsContainer = new ThingOwner<Thing>(this);
        if (staticIngredientsContainer == null)
          staticIngredientsContainer = new ThingOwner<Thing>(this);
        if (outputs == null)
          outputs = new List<ThingDef>();
        if (outputsCount == null)
          outputsCount = new List<int>();

        if (isProcessing && activeBill == null)
        {
          Log.Warning(
            $"[Production Expanded] Processor at {parent.Position} lost its bill reference on load. Processing will complete but bill won't be decremented."
          );
        }
      }
    }

    // === IThingHolder ===

    public ThingOwner GetDirectlyHeldThings()
    {
      return dynamicIngredientsContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren) { }

    // === Gizmos ===

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
      foreach (Gizmo gizmo in base.CompGetGizmosExtra())
      {
        yield return gizmo;
      }

      if (Find.Selector.SingleSelectedThing == parent)
      {
        yield return new Gizmo_ProcessorStatus(this);
      }
    }
  }
}
