using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProductionExpanded
{
  // .NET 4.8 compatibility extension for GetValueOrDefault
  internal static class DictionaryExtensions
  {
    public static TValue GetValueOrDefault<TKey, TValue>(
      this Dictionary<TKey, TValue> dict,
      TKey key,
      TValue defaultValue = default(TValue)
    )
    {
      return dict != null && dict.TryGetValue(key, out TValue value) ? value : defaultValue;
    }
  }

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
    private static int punishRareTicksCap = 3;

    private enum RuinReason
    {
      TooHot,
      TooCold,
      Paused,
      Finished,
      None,
    };

    private CompProperties_ResourceProcessor cachedProps;
    private CompProperties_ResourceProcessor Props => cachedProps;

    // State variables
    private bool isProcessing = false;
    private bool isFinished = false;
    private bool isWaitingForCycleInteraction = false;
    private bool isInspectStringDirty = true;
    private int ruinTicks = 0;
    private RuinReason previousRuinReason = RuinReason.None;
    private RuinReason isRuinReason = RuinReason.None;
    private int punishRareTicksLeft = 1;
    private int prevPunishRareTicks = 1;

    // Recipe type and parameters
    private bool isStaticRecipe = false; // Track which recipe type
    private float ratio = 1.0f; // For ratio recipes only

    // STATIC recipe ingredient tracking (index-based)
    private Dictionary<int, int> ingredientsNeeded; // ingredientIndex -> required count
    private Dictionary<int, int> ingredientsReceived; // ingredientIndex -> total count received

    // Planned outputs (for ALL recipe types)
    private Dictionary<ThingDef, int> plannedOutputs; // thingDef -> count

    // State tracking for HashSet operations (avoid redundant add/remove)
    private bool cachedNeedsFill = false;
    private bool cachedNeedsEmpty = false;
    private bool cachedNeedsCycleStart = false;

    // Progress
    private int progressTicks = 0;
    private int totalTicksPerCycle = 0;
    private int cycles = 1;
    private int currentCycle = 0;
    private int capacityRemaining = 0;

    private string cachedLabel = null; // Label of the thing being processed

    private ThingOwner<Thing> ingredientContainer;

    // Track the active vanilla bill
    private Bill_Production activeBill = null;

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

    public CompProperties_ResourceProcessor getProps() => Props;

    //============ Lifecycle ============
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
        if (needsFill.Value)
        {
          // Only cache as true if we actually add it (not punished)
          if (punishRareTicksLeft == 1)
          {
            cachedNeedsFill = true;
            processorTracker.processorsNeedingFill.Add(processor);
          }
          // If punished, don't update cache so we retry next tick
        }
        else
        {
          cachedNeedsFill = false;
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

    // ============ Punishment System ============
    public void PunishProcessor()
    {
      punishRareTicksLeft = (int)(prevPunishRareTicks * 1.3f + 0.9f);

      if (punishRareTicksLeft >= punishRareTicksCap)
        punishRareTicksLeft = punishRareTicksCap;

      prevPunishRareTicks = punishRareTicksLeft;
    }

    public void ForgiveProcessor()
    {
      prevPunishRareTicks = 1;
      punishRareTicksLeft = 1;
    }

    // ============ Core Logic ============
    public override void CompTickRare()
    {
      base.CompTickRare();

      if (punishRareTicksLeft > 1)
        punishRareTicksLeft--;

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

      // STATIC recipe safety check - eject if bill deleted or ingredients unavailable
      if (
        isStaticRecipe
        && ingredientContainer != null
        && ingredientContainer.Count > 0
        && !isProcessing
      )
      {
        // Check if bill was deleted
        if (activeBill != null && parent is Building_WorkTable workTable)
        {
          if (!workTable.BillStack.Bills.Contains(activeBill))
          {
            Log.Message("[Production Expanded] Bill deleted, ejecting collected ingredients");
            EjectIngredients();
            return;
          }
        }

        var settings = activeBill?.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings != null && settings.ingredients != null)
        {
          // Check if all needed ingredients still exist on map
          for (int i = 0; i < settings.ingredients.Count; i++)
          {
            int needed = ingredientsNeeded.GetValueOrDefault(i, 0);
            int received = ingredientsReceived.GetValueOrDefault(i, 0);
            int stillNeeded = needed - received;

            if (stillNeeded <= 0)
              continue; // This slot satisfied

            var procIng = settings.ingredients[i];
            bool found = false;

            // Check if ANY option exists on map (not forbidden)
            if (procIng.thingDefs != null)
            {
              foreach (var def in procIng.thingDefs)
              {
                if (
                  parent
                    .Map.listerThings.ThingsOfDef(def)
                    .Any(t => !t.IsForbidden(Faction.OfPlayer))
                )
                {
                  found = true;
                  break;
                }
              }
            }

            // Check categories if not found in thingDefs
            if (!found && procIng.categoryDefs != null)
            {
              found = parent
                .Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)
                .Any(t =>
                  !t.IsForbidden(Faction.OfPlayer)
                  && procIng.categoryDefs.Any(cat => cat.ContainedInThisOrDescendant(t.def))
                );
            }

            if (!found)
            {
              Log.Warning(
                $"[Production Expanded] Processor at {parent.Position} ejecting ingredients - slot {i} ingredient no longer available"
              );
              EjectIngredients();
              break;
            }
          }
        }
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
      // Early initialization check
      if (Props == null)
        return RuinReason.None;

      if (isFinished)
        return RuinReason.Finished;
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

    private void StartProcessing()
    {
      isProcessing = true;
      progressTicks = 0;
      currentCycle = 0;

      var settings = activeBill.recipe.GetModExtension<RecipeExtension_Processor>();
      cycles = settings?.cycles ?? 1;

      // Calculate total output count
      int totalOutput = 0;
      if (plannedOutputs != null)
      {
        foreach (var kvp in plannedOutputs)
        {
          totalOutput += kvp.Value;
        }
      }

      // Both STATIC and RATIO scale by output count
      totalTicksPerCycle = (settings?.ticksPerItemOut ?? 2500) * Mathf.Max(1, totalOutput);

      if (heatPusher != null)
        heatPusher.enabled = true;

      parent.DirtyMapMesh(parent.Map);
      UpdateGlower();
      isInspectStringDirty = true;
    }

    public void AddMaterials(Bill_Production bill, Thing ingredient, int count)
    {
      // ========== VALIDATION ==========
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

      if (count <= 0)
      {
        Log.Warning($"[Production Expanded] AddMaterials called with invalid count: {count}");
        return;
      }

      var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
      if (settings == null)
      {
        Log.Error(
          $"[Production Expanded] Recipe {bill.recipe.defName} missing RecipeExtension_Processor"
        );
        return;
      }

      // ========== FIRST-TIME SETUP ==========
      if (!isProcessing && (ingredientContainer == null || ingredientContainer.Count == 0))
      {
        activeBill = bill;
        isStaticRecipe = settings.isStaticRecipe;
        ratio = settings.ratio;

        if (isStaticRecipe)
        {
          // STATIC: Initialize ingredient tracking from extension
          ingredientsNeeded = new Dictionary<int, int>();
          ingredientsReceived = new Dictionary<int, int>();

          for (int i = 0; i < settings.ingredients.Count; i++)
          {
            ingredientsNeeded[i] = settings.ingredients[i].count;
          }

          // Initialize outputs from extension
          plannedOutputs = new Dictionary<ThingDef, int>();
          foreach (var product in settings.products)
          {
            plannedOutputs[product.output] = product.count;
          }
        }
        else
        {
          // RATIO: Calculate first output
          plannedOutputs = new Dictionary<ThingDef, int>();

          ThingDef outputDef;
          if (settings.useDynamicOutput)
          {
            outputDef = RawToFinishedRegistry.GetFinished(ingredient.def);
            if (outputDef == null)
            {
              Log.Error($"[Production Expanded] No registry mapping for {ingredient.def.defName}");
              outputDef = ingredient.def; // Fallback
            }
          }
          else
          {
            if (settings.products == null || settings.products.Count == 0)
            {
              Log.Error(
                $"[Production Expanded] Ratio recipe {bill.recipe.defName} has no products!"
              );
              return;
            }
            outputDef = settings.products[0].output;
          }

          int outputCount = Mathf.Max(1, (int)(count * ratio));
          plannedOutputs[outputDef] = outputCount;
        }
      }

      // ========== ADD INGREDIENT TO CONTAINER ==========
      if (ingredientContainer == null)
      {
        ingredientContainer = new ThingOwner<Thing>(this);
      }

      Thing thingToAdd = ingredient.SplitOff(count);
      if (!ingredientContainer.TryAdd(thingToAdd, false))
      {
        Log.Error(
          $"[Production Expanded] Failed to add {count} {ingredient.def.defName} to container"
        );
        GenSpawn.Spawn(thingToAdd, parent.Position, parent.Map);
        return;
      }

      // Play input sound
      Props.soundInput?.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

      // ========== UPDATE TRACKING BY RECIPE TYPE ==========
      if (isStaticRecipe)
      {
        // STATIC: Find matching ingredient slot
        int ingredientIndex = -1;

        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          var procIng = settings.ingredients[i];

          // Check if this ingredient matches this slot
          bool matches = procIng.thingDefs?.Contains(ingredient.def) ?? false;

          if (!matches && procIng.categoryDefs != null)
          {
            matches = procIng.categoryDefs.Any(cat =>
              cat.ContainedInThisOrDescendant(ingredient.def)
            );
          }

          if (matches)
          {
            // Prefer unsatisfied slots
            int received = ingredientsReceived.GetValueOrDefault(i, 0);
            int needed = ingredientsNeeded.GetValueOrDefault(i, 0);

            if (received < needed)
            {
              ingredientIndex = i;
              break; // Use first unsatisfied matching slot
            }
            else if (ingredientIndex == -1)
            {
              ingredientIndex = i; // Remember first match even if satisfied
            }
          }
        }

        if (ingredientIndex == -1)
        {
          Log.Error(
            $"[Production Expanded] Ingredient {ingredient.def.defName} doesn't match any recipe slots"
          );
          return;
        }

        // Update received count for this slot
        ingredientsReceived[ingredientIndex] =
          ingredientsReceived.GetValueOrDefault(ingredientIndex, 0) + count;

        // Check if all slots satisfied
        bool hasAll = ingredientsNeeded.All(kvp =>
          ingredientsReceived.GetValueOrDefault(kvp.Key, 0) >= kvp.Value
        );

        if (hasAll && !isProcessing)
        {
          StartProcessing();
        }

        // Update tracker
        UpdateTrackerState(needsFill: !hasAll && getIsReady());
        isInspectStringDirty = true;
      }
      else
      {
        // RATIO RECIPE
        if (!isProcessing)
        {
          // Start processing immediately
          StartProcessing();
        }
        else
        {
          // Add to existing batch - determine output for this ingredient
          ThingDef outputDef;

          if (settings.useDynamicOutput)
          {
            outputDef = RawToFinishedRegistry.GetFinished(ingredient.def);
            if (outputDef == null)
            {
              Log.Warning(
                $"[Production Expanded] No registry mapping for {ingredient.def.defName}"
              );
              outputDef = ingredient.def;
            }
          }
          else
          {
            // Regular ratio - use first (only) planned output type
            outputDef = plannedOutputs.Keys.First();
          }

          int additionalOutput = Mathf.Max(1, (int)(count * ratio));

          // Add or update output
          plannedOutputs[outputDef] =
            plannedOutputs.GetValueOrDefault(outputDef, 0) + additionalOutput;

          // Recalculate timing based on new total output
          int totalOutput = 0;
          foreach (var kvp in plannedOutputs)
          {
            totalOutput += kvp.Value;
          }

          int newTotalTicksPerCycle = (settings.ticksPerItemOut) * totalOutput;

          // Preserve progress ratio
          int totalTicksPassed = totalTicksPerCycle * currentCycle + progressTicks;
          totalTicksPerCycle = newTotalTicksPerCycle;

          // Recalculate cycles/progress
          currentCycle = 0;
          while (totalTicksPassed > totalTicksPerCycle && currentCycle < cycles - 1)
          {
            totalTicksPassed -= totalTicksPerCycle;
            currentCycle++;
          }
          progressTicks = totalTicksPassed;
        }

        // Update capacity
        float capacityFactor = settings.capacityFactor;
        capacityRemaining -= (int)(count * capacityFactor);
        if (capacityRemaining < 0)
          capacityRemaining = 0;

        // Update tracker
        UpdateTrackerState(needsFill: capacityRemaining > 0 && getIsReady());
        isInspectStringDirty = true;
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
      plannedOutputs?.Clear();
      isInspectStringDirty = true;
      cycles = 1;

      UpdateTrackerState(needsFill: false, needsEmpty: true, needsCycleStart: false);
      UpdateGlower();
    }

    public void EmptyBuilding()
    {
      if (!isFinished)
        return;

      // Spawn all outputs
      if (plannedOutputs != null && plannedOutputs.Count > 0)
      {
        foreach (var kvp in plannedOutputs)
        {
          if (kvp.Key != null && kvp.Value > 0)
          {
            Thing item = ThingMaker.MakeThing(kvp.Key);
            item.stackCount = kvp.Value;
            GenSpawn.Spawn(item, parent.InteractionCell, parent.Map);
          }
        }

        // Play extract sound
        Props.soundExtract?.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

        // Notify bill completion
        if (activeBill != null)
        {
          activeBill.Notify_IterationCompleted(null, new List<Thing>());
        }
        else if (isProcessing)
        {
          Log.Warning("[Production Expanded] Completed processing but bill was removed");
        }
      }

      // Clear ingredient container (ingredients consumed during processing)
      ingredientContainer?.ClearAndDestroyContents();

      // Reset all state
      parent.DirtyMapMesh(parent.Map);
      isFinished = false;
      isProcessing = false;
      activeBill = null;
      isWaitingForCycleInteraction = false;
      isInspectStringDirty = true;
      capacityRemaining = Props.maxCapacity;
      ruinTicks = 0;
      isRuinReason = RuinReason.None;
      previousRuinReason = RuinReason.None;

      // Clear recipe data
      plannedOutputs?.Clear();
      ingredientsNeeded?.Clear();
      ingredientsReceived?.Clear();

      if (heatPusher != null)
        heatPusher.enabled = false;

      // Forgive processor so it's immediately available for refilling (instead of waiting for punishment to expire)
      ForgiveProcessor();

      UpdateTrackerState(needsFill: getIsReady(), needsEmpty: false);
      UpdateGlower();
    }

    public void EjectIngredients()
    {
      if (ingredientContainer == null || ingredientContainer.Count == 0)
        return;

      // Drop all held ingredients at interaction cell
      ingredientContainer.TryDropAll(parent.InteractionCell, parent.Map, ThingPlaceMode.Near);

      // Reset state
      ingredientsNeeded?.Clear();
      ingredientsReceived?.Clear();
      plannedOutputs?.Clear();
      activeBill = null;
      capacityRemaining = Props.maxCapacity;
      isInspectStringDirty = true;

      UpdateTrackerState(needsFill: false, needsEmpty: false, needsCycleStart: false);
    }

    private void cleanInspectString()
    {
      if (!parent.Spawned)
      {
        inspectMessageCahce =
          $"Well this is awkward... \nso how is your day going? personally im decent but tbh could be better. \nI assume yours isnt that fun if you are seeing this string in game... \nWell im truly sorry about that! but think about the bright side, \natleast its more interesting than me writing \"ERROR COMP HAS NO PARENT\" right? \nwell anyway ive gotta get back to coding so cya XD";
      }
      else if (!isProcessing && (ingredientContainer == null || ingredientContainer.Count == 0))
      {
        inspectMessageCahce = "";
      }
      else if (!isProcessing && ingredientContainer != null && ingredientContainer.Count > 0)
      {
        // STATIC recipe collecting ingredients
        inspectMessageCahce = "Collecting ingredients...";
      }
      else if (isFinished && Props.ticksToRuin > ruinTicks)
      {
        // Show all planned outputs
        if (plannedOutputs != null && plannedOutputs.Count > 0)
        {
          inspectMessageCahce = "Finished. Waiting for colonist to extract:\n";
          foreach (var kvp in plannedOutputs)
          {
            inspectMessageCahce += $"{kvp.Key.label} x{kvp.Value}\n";
          }
          inspectMessageCahce = inspectMessageCahce.TrimEnd('\n');
        }
        else
        {
          inspectMessageCahce = "Finished. Waiting for colonist.";
        }
      }
      else if (isWaitingForCycleInteraction)
      {
        inspectMessageCahce =
          $"Waiting for colonist interaction to resume processing.\ncycles remaining: {cycles - currentCycle}";
      }
      else if (isProcessing)
      {
        inspectMessageCahce = $"Processing: {(float)progressTicks / totalTicksPerCycle:P0}";
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

    // ============ Save Building Data ============
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

      // Recipe type and parameters
      Scribe_Values.Look(ref isStaticRecipe, "isStaticRecipe", false);
      Scribe_Values.Look(ref ratio, "ratio", 1.0f);

      // STATIC recipe ingredient tracking
      Scribe_Collections.Look(
        ref ingredientsNeeded,
        "ingredientsNeeded",
        LookMode.Value,
        LookMode.Value
      );
      Scribe_Collections.Look(
        ref ingredientsReceived,
        "ingredientsReceived",
        LookMode.Value,
        LookMode.Value
      );

      // Planned outputs
      Scribe_Collections.Look(ref plannedOutputs, "plannedOutputs", LookMode.Def, LookMode.Value);

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

    // ============ IThingHolder ============
    public ThingOwner GetDirectlyHeldThings()
    {
      return ingredientContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
      ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    // ============ Accessors for WorkGiver ============
    public ThingDef getInputItem()
    {
      // For RATIO recipes only - return the input type from container
      if (isStaticRecipe)
        return null;

      if (ingredientContainer != null && ingredientContainer.Count > 0)
      {
        return ingredientContainer[0].def;
      }
      return null;
    }

    public int GetIngredientNeeded(int index)
    {
      return ingredientsNeeded?.GetValueOrDefault(index, 0) ?? 0;
    }

    public int GetIngredientReceived(int index)
    {
      return ingredientsReceived?.GetValueOrDefault(index, 0) ?? 0;
    }

    public int GetIngredientsNeededCount()
    {
      return ingredientsNeeded?.Count ?? 0;
    }

    public int GetIngredientsCollectedCount()
    {
      // Count how many ingredient slots are fully satisfied
      int count = 0;
      if (ingredientsNeeded != null && ingredientsReceived != null)
      {
        foreach (var kvp in ingredientsNeeded)
        {
          if (ingredientsReceived.GetValueOrDefault(kvp.Key, 0) >= kvp.Value)
            count++;
        }
      }
      return count;
    }

    // ============ Gizmo ============
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

        // Manual eject button for STATIC recipes with collected ingredients
        if (ingredientContainer != null && ingredientContainer.Count > 0 && !isProcessing)
        {
          yield return new Command_Action
          {
            defaultLabel = "Eject ingredients",
            defaultDesc = "Eject all collected ingredients from this processor",
            icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true),
            action = delegate
            {
              EjectIngredients();
            },
          };
        }

        // Debug buttons (god mode only)
        if (Prefs.DevMode)
        {
          yield return new Command_Action
          {
            defaultLabel = "DEV: Complete processing",
            defaultDesc = "Instantly complete current processing cycle",
            action = delegate
            {
              if (isProcessing)
              {
                progressTicks = totalTicksPerCycle;
                CompleteProcessingCycle();
              }
            },
          };

          yield return new Command_Action
          {
            defaultLabel = "DEV: Add test ingredients",
            defaultDesc = "Add test ingredients to processor",
            action = delegate
            {
              if (
                activeBill == null
                && parent is Building_WorkTable workTable
                && workTable.BillStack.Count > 0
              )
              {
                var bill = workTable.BillStack.Bills[0] as Bill_Production;
                if (bill != null)
                {
                  var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
                  if (
                    settings != null
                    && settings.ingredients != null
                    && settings.ingredients.Count > 0
                  )
                  {
                    var firstIng = settings.ingredients[0];
                    if (firstIng.thingDefs != null && firstIng.thingDefs.Count > 0)
                    {
                      Thing testThing = ThingMaker.MakeThing(firstIng.thingDefs[0]);
                      testThing.stackCount = firstIng.count;
                      AddMaterials(bill, testThing, testThing.stackCount);
                    }
                  }
                }
              }
            },
          };

          yield return new Command_Action
          {
            defaultLabel = "DEV: Force eject",
            defaultDesc = "Force eject all contents",
            action = delegate
            {
              if (ingredientContainer != null)
              {
                EjectIngredients();
              }
            },
          };
          yield return new Command_Action
          {
            defaultLabel = "DEV: info",
            defaultDesc = $"{punishRareTicksLeft}",
            action = delegate { },
          };
        }
      }
    }
  }
}
