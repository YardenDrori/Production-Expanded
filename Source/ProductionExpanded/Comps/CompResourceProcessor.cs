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

    // State variables
    private bool isProcessing = false;
    private bool isFinished = false;
    private bool isWaitingForCycleInteraction = false;
    private RuinReason previousRuinReason = RuinReason.None;
    private bool isInspectStringDirty = true;
    private RuinReason isRuinReason = RuinReason.None;
    private int ruinTicks = 0;
    private int punishRareTicks = 0;
    private int prevPunishRareTicks = 0;

    // State tracking for HashSet operations (avoid redundant add/remove)
    private bool cachedNeedsFill = false;
    private bool cachedNeedsEmpty = false;
    private bool cachedNeedsCycleStart = false;

    // Progress
    private int progressTicks = 0;
    private int totalTicksPerCycle = 0;
    private int cycles = 1;
    private int currentCycle = 0;

    // private int inputCount = 0;
    // private int outputCount = 0;
    private int highestStackOutputCount = 0;
    private ThingDef highestStackOutputDef = null;
    private int capacityRemaining = 0;

    private string cachedLabel = null; // Label of the thing being processed

    // private ThingOwner<Thing> ingredientContainer;
    private ThingOwner<Thing> staticIngredientsContainer;
    private ThingOwner<Thing> dynamicIngredientsContainer;
    private Dictionary<ProcessorIngredient, int> allIngredientsAndCounts = new();

    // Track the active vanilla bill
    private Bill_Production activeBill = null;

    private List<ThingDef> outputs = new List<ThingDef>();
    private List<int> outputsCount = new List<int>();

    //comps
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

    // public ThingDef getInputItem() => inputType;

    public CompProperties_ResourceProcessor getProps() => Props;

    public List<ProcessorIngredient> GetAllIngredients() => allIngredientsAndCounts.Keys.ToList();

    public Dictionary<ProcessorIngredient, int> GetAllIngredientsAndTheirCounts() =>
      allIngredientsAndCounts;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
      base.PostSpawnSetup(respawningAfterLoad);

      BuildIngredientCountDictionary();

      // Cache props to avoid repeated casting
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
        // Initialize fill state
        UpdateTrackerState(needsFill: getCapacityRemaining() > 0 && getIsReady());
      }

      UpdateGlower();
    }

    //tbh thinking about this the punishment system is kinda dumb as 99% of the time the prevPunishmentRareTicks will be at 60
    public void PunishProcessor()
    {
      if (prevPunishRareTicks > 60)
      {
        prevPunishRareTicks = 60;
      }
      else
      {
        prevPunishRareTicks = (int)(prevPunishRareTicks * 1.2);
      }
      punishRareTicks = prevPunishRareTicks;
    }

    public void forgivePunishment()
    {
      prevPunishRareTicks /= 1;
      punishRareTicks = 0;
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
        {
          if (punishRareTicks == 0)
          {
            processorTracker.processorsNeedingFill.Add(processor);
            return;
          }
        }
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
      if (!staticIngredientsContainer.NullOrEmpty())
      {
        staticIngredientsContainer.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
      }
      if (!dynamicIngredientsContainer.NullOrEmpty())
      {
        dynamicIngredientsContainer.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
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

    private ProcessorIngredient FindMatchingSlot(ThingDef def)
    {
      RecipeExtension_Processor settings = GetActiveBill()
        .recipe.GetModExtension<RecipeExtension_Processor>();
      if (settings == null)
        return null;
      for (int i = 0; i < settings.ingredients.Count; i++)
      {
        var ingredient = settings.ingredients[i];
        if (ingredient.IsSpecific && ingredient.thingDef == def)
          return ingredient;
        if (ingredient.IsCategory && def.IsWithinCategory(ingredient.category))
          return ingredient;
      }
      return null;
    }

    private int CalculateCapacityNeeded(List<Thing> things)
    {
      float total = 0f;
      foreach (Thing thing in things)
      {
        ProcessorIngredient slot = FindMatchingSlot(thing.def);
        total += thing.stackCount * (slot?.capacityPerItem ?? 1f);
      }
      return (int)total;
    }

    private void CalculateOutputs(RecipeExtension_Processor settings)
    {
      outputs.Clear();
      outputsCount.Clear();
      highestStackOutputCount = 0;
      highestStackOutputDef = null;
      if (settings?.UsesDynamicOutput == true)
      {
        foreach (Thing input in dynamicIngredientsContainer)
        {
          outputs.Add(RawToFinishedRegistry.GetFinished(input.def));
          float ratio = settings?.ratioDynamic ?? 1.0f;
          int outputCount = (int)(input.stackCount * ratio);
          outputsCount.Add(outputCount);
          if (outputCount > highestStackOutputCount)
          {
            highestStackOutputCount = outputCount;
            highestStackOutputDef = RawToFinishedRegistry.GetFinished(input.def);
          }
        }
      }
      if (settings?.products.NullOrEmpty() == false)
      {
        foreach (ProcessorProduct output in settings?.products)
        {
          outputs.Add(output.thingDef);
          outputsCount.Add(output.count);
          if (output.count > highestStackOutputCount)
          {
            highestStackOutputCount = output.count;
            highestStackOutputDef = output.thingDef;
          }
        }
      }
    }

    public void BuildIngredientCountDictionary()
    {
      if (!dynamicIngredientsContainer.NullOrEmpty())
      {
        dynamicIngredientsContainer.Clear();
        foreach (Thing thing in dynamicIngredientsContainer)
        {
          ProcessorIngredient ingredient = FindMatchingSlot(thing.def);
          if (allIngredientsAndCounts.ContainsKey(ingredient))
          {
            allIngredientsAndCounts[ingredient] += thing.stackCount;
          }
          else
          {
            allIngredientsAndCounts.Add(ingredient, thing.stackCount);
          }
        }
      }
      if (!staticIngredientsContainer.NullOrEmpty())
      {
        staticIngredientsContainer.Clear();
        foreach (Thing thing in staticIngredientsContainer)
        {
          ProcessorIngredient ingredient = FindMatchingSlot(thing.def);
          if (allIngredientsAndCounts.ContainsKey(ingredient))
          {
            allIngredientsAndCounts[ingredient] += thing.stackCount;
          }
          else
          {
            allIngredientsAndCounts.Add(ingredient, thing.stackCount);
          }
        }
      }
    }

    public void AddMaterials(
      Bill_Production bill,
      List<Thing> staticIngredients,
      List<Thing> dynamicIngredients
    )
    {
      // Validation: Check for null inputs
      if (bill == null)
      {
        Log.Warning("[Production Expanded] AddMaterials called with null bill");
        return;
      }

      if (staticIngredients.NullOrEmpty() && dynamicIngredients.NullOrEmpty())
      {
        Log.Error("[Production Expanded] AddMaterials called with null ingredients");
        return;
      }

      if (bill.recipe == null)
      {
        Log.Error("[Production Expanded] Bill has null recipe");
        return;
      }

      List<Thing> allInputs = new();
      staticIngredients.CopyToList(allInputs);
      allInputs.AddRange(dynamicIngredients);
      int inputCount = 0;

      foreach (Thing thing in allInputs)
      {
        if (thing.stackCount <= 0)
        {
          Log.Warning(
            $"[Production Expanded] AddMaterials called with invalid count: {thing.stackCount}"
          );
          return;
        }
        else
        {
          inputCount += thing.stackCount;
        }
      }

      var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
      int capacityNeededForInput = CalculateCapacityNeeded(allInputs);

      // Validation: Check capacity
      if (capacityRemaining <= 0 || capacityRemaining < capacityNeededForInput)
      {
        Log.Warning(
          $"[Production Expanded] Processor at {parent.Position} is full, cannot add materials"
        );
        return;
      }

      // make ThingOwners if dont exist
      // and Validation: If already processing, ingredient must match current input type
      bool foundMatch = false;
      if (settings?.UsesDynamicOutput == true)
      {
        if (dynamicIngredientsContainer.NullOrEmpty())
        {
          dynamicIngredientsContainer = new ThingOwner<Thing>(this);
        }
        if (isProcessing)
        {
          foreach (Thing newInput in dynamicIngredients)
          {
            if (dynamicIngredientsContainer.Any(t => t.def == newInput.def))
            {
              foundMatch = true;
              if (!dynamicIngredientsContainer.TryAdd(newInput, true))
              {
                Log.Error(
                  $"[Production Expande] failed to add {newInput.def.defName} to ThingOwner"
                );
                GenSpawn.Spawn(newInput, parent.Position, parent.Map);
                return;
              }
            }
          }
        }
      }
      if (settings?.UsesStaticInput == true)
      {
        if (staticIngredientsContainer.NullOrEmpty())
        {
          staticIngredientsContainer = new ThingOwner<Thing>(this);
        }

        if (isProcessing)
        {
          foreach (Thing newInput in staticIngredients)
          {
            if (staticIngredientsContainer.Any(t => t.def == newInput.def))
            {
              foundMatch = true;
              if (!staticIngredientsContainer.TryAdd(newInput, true))
              {
                Log.Error(
                  $"[Production Expande] failed to add {newInput.def.defName} to ThingOwner"
                );
                GenSpawn.Spawn(newInput, parent.Position, parent.Map);
                return;
              }
            }
          }
        }
      }

      if (!foundMatch && isProcessing)
      {
        Log.Warning(
          $"[Production Expanded] Cannot no ingredients match processors current ingredients"
        );
        return;
      }

      if (Props.soundInput != null)
      {
        Props.soundInput.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
      }

      // Update Fill Tracker
      UpdateTrackerState(needsFill: getCapacityRemaining() > 0 && getIsReady());

      int ticksPerItemOut = settings?.ticksPerItemOut ?? 2500;
      int extensionCycles = settings?.cycles ?? 1;

      if (isProcessing)
      {
        capacityRemaining -= capacityNeededForInput;
        if (capacityRemaining < 0)
          capacityRemaining = 0;
        // Add to existing batch
        int totalTicksPassed = totalTicksPerCycle * currentCycle + progressTicks;

        CalculateOutputs(settings);

        int capacityUsed = Props.maxCapacity - capacityRemaining;
        float minCapacity = Props.maxCapacity * Props.minimumItemsPrecentageForWorkTime;
        if (capacityUsed >= minCapacity)
          this.totalTicksPerCycle = ticksPerItemOut * highestStackOutputCount;
        else
          this.totalTicksPerCycle = ticksPerItemOut * (int)minCapacity;

        BuildIngredientCountDictionary();

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

      //=== New Batch ===
      foreach (Thing staticIn in staticIngredients)
      {
        if (!staticIngredientsContainer.TryAdd(staticIn))
        {
          Log.Error($"[Production Expande] failed to add {staticIn.def.defName} to ThingOwner");
          GenSpawn.Spawn(staticIn, parent.Position, parent.Map);
          return;
        }
      }
      foreach (Thing dynamicIn in dynamicIngredients)
      {
        if (!dynamicIngredientsContainer.TryAdd(dynamicIn))
        {
          Log.Error($"[Production Expande] failed to add {dynamicIn.def.defName} to ThingOwner");
          GenSpawn.Spawn(dynamicIn, parent.Position, parent.Map);
          return;
        }
      }

      capacityRemaining -= capacityNeededForInput;
      if (capacityRemaining < 0)
        capacityRemaining = 0;

      if (heatPusher != null)
        heatPusher.enabled = true;
      activeBill = bill;

      parent.DirtyMapMesh(parent.Map);
      UpdateGlower();
      isWaitingForCycleInteraction = false;
      progressTicks = 0;
      currentCycle = 0;

      CalculateOutputs(settings);

      this.cycles = extensionCycles;
      // this.inputCount = count;

      BuildIngredientCountDictionary();

      int capacityUsedNew = Props.maxCapacity - capacityRemaining;
      float minCapacityNew = Props.maxCapacity * Props.minimumItemsPrecentageForWorkTime;
      if (capacityUsedNew >= minCapacityNew)
        this.totalTicksPerCycle = ticksPerItemOut * highestStackOutputCount;
      else
        this.totalTicksPerCycle = ticksPerItemOut * (int)minCapacityNew;

      isInspectStringDirty = true;

      // Validate final state
      if (this.outputs.NullOrEmpty())
      {
        Log.Error($"[Production Expanded] Invalid outputs calculated for {bill.recipe.defName}");
        return;
      }
      isProcessing = true;
    }

    ///<summary>
    /// method to get the max amount of an item a building should ever request for with a given recipe
    ///</summary>
    public int MaxCountOfIngredientInRecipe(ProcessorIngredient ingredient)
    {
      RecipeExtension_Processor recipeSettings =
        activeBill.recipe.GetModExtension<RecipeExtension_Processor>();
      int capacity = Props.maxCapacity;
      if (recipeSettings == null)
        return 0;

      int ingredientCount = 0;
      foreach (ProcessorIngredient recipeIngredient in recipeSettings.ingredients)
      {
        if (recipeIngredient.count != -1)
        {
          if (
            recipeIngredient.thingDef == ingredient.thingDef
            || ingredient.category == recipeIngredient.category
          )
          {
            ingredientCount++;
          }
          capacity -= recipeIngredient.count;
        }
      }

      while (capacity > 0)
      {
        int capacityTemp = capacity;
        int tempIngredientCount = ingredientCount;
        bool broke = false;
        foreach (ProcessorIngredient recipeIngredient in recipeSettings.ingredients)
        {
          if (recipeIngredient.count == -1)
          {
            capacity -= (int)(recipeIngredient.capacityPerItem * recipeIngredient.ratio);
            if (capacity < 0)
            {
              broke = true;
              break;
            }
            if (
              recipeIngredient.thingDef == ingredient.thingDef
              || recipeIngredient.category == ingredient.category
            )
            {
              tempIngredientCount += (int)(recipeIngredient.ratio + 0.3f); //im paranoid about float imprecision this might be much tho
            }
          }
        }
        if (!broke)
        {
          capacity = capacityTemp;
          ingredientCount = tempIngredientCount;
        }
        else
        {
          break;
        }
      }
      return ingredientCount;
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
      dynamicIngredientsContainer?.ClearAndDestroyContents();
      staticIngredientsContainer?.ClearAndDestroyContents();
      // Reset
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

    public void EmptyBuilding()
    {
      if (isFinished)
      {
        if (!outputs.NullOrEmpty())
        {
          for (int i = 0; i < outputs.Count; i++)
          {
            Thing item = ThingMaker.MakeThing(outputs[i]);
            item.stackCount = outputsCount[i];
            GenSpawn.Spawn(item, parent.InteractionCell, parent.Map);
          }
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
        dynamicIngredientsContainer?.ClearAndDestroyContents();
        staticIngredientsContainer?.ClearAndDestroyContents();
        outputs.Clear();
        outputsCount.Clear();
        BuildIngredientCountDictionary();

        // Reset
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
          $"Finished. Waiting for colonist to extract {highestStackOutputDef.label} x{highestStackOutputCount}";
      }
      else if (isWaitingForCycleInteraction)
      {
        inspectMessageCahce =
          $"Waiting for colonist interaction to resume processing.\ncycles remaining: {cycles - currentCycle}";
      }
      else if (isProcessing)
      {
        inspectMessageCahce =
          $"Processing {highestStackOutputDef.label ?? "Material"} x{highestStackOutputCount}: {(float)progressTicks / totalTicksPerCycle:P0}";
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

    // IThingHolder implementation
    // RimWorld requires a single ThingOwner here. We return the dynamic container
    // as the primary one; both containers are saved/loaded in PostExposeData.
    public ThingOwner GetDirectlyHeldThings()
    {
      return dynamicIngredientsContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren) { }

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
