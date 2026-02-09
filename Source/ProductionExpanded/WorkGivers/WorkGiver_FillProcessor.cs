using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class WorkGiver_FillProcessor : WorkGiver_Scanner
  {
    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
      var tracker = pawn.Map?.GetComponent<MapComponent_ProcessorTracker>();
      if (tracker != null)
      {
        return tracker.processorsNeedingFill;
      }
      return null;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      if (!(t is Building_Processor processor))
        return false;

      CompResourceProcessor comp = processor.GetComp<CompResourceProcessor>();
      if (comp == null || !comp.getIsReady())
        return false;

      if (t.IsForbidden(pawn))
        return false;
      if (!pawn.CanReserve(t, 1, -1, null, forced))
        return false;

      if (comp.getIsProcessing())
      {
        // Refuel running processor — only scaling ingredients that already match
        Bill_Production bill = comp.GetActiveBill();
        if (bill == null)
        {
          Log.Error("[Production Expanded] Processor is processing but has no active bill");
          return false;
        }

        var settings = bill.recipe?.GetModExtension<RecipeExtension_Processor>();
        if (settings == null)
        {
          Log.Error(
            $"[Production Expanded] Recipe {bill.recipe.defName} has no RecipeExtension_Processor"
          );
          return false;
        }

        // Static recipes lock processor once started
        if (settings.isStaticRecipe)
          return false;

        // Invalid state check
        if (settings.ingredients.NullOrEmpty() || settings.ingredients[0] == null)
          return false;

        // No room
        if (comp.getCapacityRemaining() <= 0)
          return false;

        var ing = settings.ingredients[0];
        int minNeeded = (int)(1 / settings.ratio);
        int found = FindHowManyItemsExistForIngredient(pawn, processor, bill, ing);

        if (found < minNeeded)
        {
          comp.PunishProcessor();
          return false;
        }
        comp.ForgiveProcessor();
        return true;
      }

      // Start new bill — only if ALL ingredients are available on the map
      foreach (Bill bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings?.ingredients == null)
        {
          Log.Error(
            $"[Production Expanded] {bill.recipe.defName} has no RecipeExtension_Processor"
          );
          continue;
        }

        // Static recipes require all ingredients before starting (ignore capacity for static recipes)
        if (settings.isStaticRecipe)
        {
          bool allSlotsFulfilled = true;
          foreach (var ing in settings.ingredients)
          {
            int countNeeded = ing.count;
            int countAvailableStatic = FindHowManyItemsExistForIngredient(
              pawn,
              processor,
              bill,
              ing
            );
            if (countNeeded > countAvailableStatic)
            {
              allSlotsFulfilled = false;
              break;
            }
          }
          if (allSlotsFulfilled)
          {
            comp.ForgiveProcessor();
            return true;
          }
          comp.PunishProcessor();
          return false;
        }

        // Dynamic recipes: ratio-based scaling (e.g., ratio 0.5 means 10 iron → 5 steel)
        // Minimum needed = 1/ratio (e.g., 1/0.5 = 2 minimum iron for 1 craft)
        // Check capacity for dynamic recipes
        if (comp.getCapacityRemaining() <= 0)
          return false;

        int minCountNeeded = (int)(1 / settings.ratio);
        int countAvailableDynamic = FindHowManyItemsExistForIngredient(
          pawn,
          processor,
          bill,
          settings.ingredients[0]
        );
        if (minCountNeeded > countAvailableDynamic)
        {
          comp.PunishProcessor();
          return false;
        }
        comp.ForgiveProcessor();
        return true;
      }
      //no bills
      return false;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      // Basic validation
      if (!(t is Building_Processor processor))
        return null;

      CompResourceProcessor comp = processor.GetComp<CompResourceProcessor>();
      if (comp == null || !comp.getIsReady())
        return null;

      if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        return null;

      // Already processing, dynamic recipes only
      if (comp.getIsProcessing())
      {
        Bill_Production bill = comp.GetActiveBill();
        if (bill == null)
        {
          Log.Error("[Production Expanded] Processor is processing but has no active bill");
          return null;
        }

        var settings = bill.recipe?.GetModExtension<RecipeExtension_Processor>();
        if (settings == null)
        {
          Log.Error(
            $"[Production Expanded] Recipe {bill.recipe.defName} missing RecipeExtension_Processor"
          );
          return null;
        }

        // Static recipes can't be refilled while processing
        if (settings.isStaticRecipe)
          return null;

        // Validate ingredients exist
        if (settings.ingredients.NullOrEmpty() || settings.ingredients[0] == null)
          return null;

        // Find ingredient to haul for dynamic recipe
        ProcessorIngredient ing = settings.ingredients[0];
        List<Thing> availableIngredients = FindIngredient(
          pawn,
          processor,
          bill,
          ing,
          stopAtFirst: true
        );

        if (availableIngredients.NullOrEmpty())
          return null;

        Thing ingredient = availableIngredients[0];

        // Create job to haul ingredient to processor
        Job job = JobMaker.MakeJob(
          JobDefOf_ProductionExpanded.PE_FillProcessor,
          processor,
          ingredient
        );
        job.bill = bill;
        job.count = Mathf.Max(1, (int)(comp.getCapacityRemaining() / settings.capacityFactor));
        return job;
      }

      // Start new bill - find first valid bill with available ingredients
      foreach (Bill_Production bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        var settings = bill.recipe?.GetModExtension<RecipeExtension_Processor>();
        if (settings?.ingredients.NullOrEmpty() ?? true)
          continue;

        if (settings.isStaticRecipe)
        {
          // STATIC RECIPE: Find first unfulfilled ingredient slot
          for (int i = 0; i < settings.ingredients.Count; i++)
          {
            if (settings.ingredients[i] == null)
              continue;

            int needed = settings.ingredients[i].count;
            int received = comp.GetIngredientReceived(i);
            int stillNeeded = needed - received;

            if (stillNeeded <= 0)
              continue; // This slot is already satisfied

            // Find ingredient for this slot
            List<Thing> availableIngredients = FindIngredient(
              pawn,
              processor,
              bill,
              settings.ingredients[i],
              stopAtFirst: true
            );

            if (availableIngredients.NullOrEmpty())
              break; // Can't fulfill this slot, try next bill

            Thing ingredient = availableIngredients[0];

            // Create job to haul ingredient to processor
            Job job = JobMaker.MakeJob(
              JobDefOf_ProductionExpanded.PE_FillProcessor,
              processor,
              ingredient
            );
            job.bill = bill;
            job.count = Mathf.Min(stillNeeded, ingredient.stackCount);
            return job;
          }
        }
        else
        {
          // DYNAMIC RECIPE: Find ingredient from first (and only) ingredient slot
          // Check capacity for dynamic recipes
          if (comp.getCapacityRemaining() <= 0)
            continue;

          if (settings.ingredients[0] == null)
            continue;

          List<Thing> availableIngredients = FindIngredient(
            pawn,
            processor,
            bill,
            settings.ingredients[0],
            stopAtFirst: true
          );

          if (availableIngredients.NullOrEmpty())
            continue; // Try next bill

          Thing ingredient = availableIngredients[0];

          // Calculate minimum needed for this ratio recipe
          int minNeeded = Mathf.Max(1, (int)(1 / settings.ratio));
          if (ingredient.stackCount < minNeeded)
            continue; // Not enough for a single craft

          // Create job to haul ingredient to processor
          Job job = JobMaker.MakeJob(
            JobDefOf_ProductionExpanded.PE_FillProcessor,
            processor,
            ingredient
          );
          job.bill = bill;
          job.count = Mathf.Max(1, (int)(comp.getCapacityRemaining() / settings.capacityFactor));
          return job;
        }
      }

      // No valid bills found
      return null;
    }

    private int CountThingStackCountTotalFromList(List<Thing> things)
    {
      if (things.NullOrEmpty())
      {
        return 0;
      }
      int total = 0;
      foreach (var thing in things)
      {
        total += thing.stackCount;
      }
      return total;
    }

    private int FindHowManyItemsExistForIngredient(
      Pawn pawn,
      Building_Processor processor,
      Bill bill,
      ProcessorIngredient ingredient
    )
    {
      List<Thing> ingredientsFound = FindIngredient(
        pawn,
        processor,
        bill,
        ingredient,
        stopAtFirst: false
      );
      return CountThingStackCountTotalFromList(ingredientsFound);
    }

    private int GetCountOfDefInMap(Pawn pawn, ThingDef def, Bill_Production bill)
    {
      int count = 0;
      foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(def))
      {
        // Check if thing passes all bill filters
        if (!IsThingCountedForBill(thing, bill))
          continue;

        count += thing.stackCount;
      }
      return count;
    }

    // Mimics RimWorld's bill filter logic for counting products
    private bool IsThingCountedForBill(Thing thing, Bill_Production bill)
    {
      // Check if in valid zone (if bill has zone restriction)
      if (bill.GetIncludeSlotGroup() != null)
      {
        ISlotGroup slotGroup = thing.GetSlotGroup();
        if (slotGroup == null || slotGroup != bill.GetIncludeSlotGroup())
        {
          return false;
        }
      }

      // Check quality range
      if (thing.TryGetQuality(out QualityCategory quality))
      {
        if (!bill.qualityRange.Includes(quality))
        {
          return false;
        }
      }

      // Check HP range (for items with hit points)
      if (thing.def.useHitPoints)
      {
        float hpPercent = (float)thing.HitPoints / (float)thing.MaxHitPoints;
        if (!bill.hpRange.Includes(hpPercent))
        {
          return false;
        }
      }

      // Check tainted
      if (thing.TryGetComp<CompQuality>() != null)
      {
        CompQuality compQuality = thing.TryGetComp<CompQuality>();
        if (compQuality != null)
        {
          Apparel apparel = thing as Apparel;
          if (apparel != null && apparel.WornByCorpse && !bill.includeTainted)
          {
            return false;
          }
        }
      }

      // Check equipped (skip equipped items unless bill allows it)
      if (!bill.includeEquipped)
      {
        if (thing.ParentHolder is Pawn_EquipmentTracker || thing.ParentHolder is Pawn_ApparelTracker)
        {
          return false;
        }
      }

      return true;
    }

    //finds Thing of either specific thingdef or in a specific category
    private List<Thing> FindIngredient(
      Pawn pawn,
      Building_Processor processor,
      Bill bill,
      ProcessorIngredient ingredient,
      bool stopAtFirst
    )
    {
      if (bill == null)
        return null;

      var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
      if (settings == null)
      {
        return null;
      }

      List<Thing> foundThings = new();
      float searchRadius = bill.ingredientSearchRadius;

      if (!ingredient.thingDefs.NullOrEmpty())
      {
        foreach (var ingredientDef in ingredient.thingDefs)
        {
          // Iterate through ALL matching things to find valid ones
          foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(ingredientDef))
          {
            // Basic checks: forbidden, reachable, ingredient filter
            if (!thing.IsForbidden(pawn) &&
                pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly) &&
                bill.ingredientFilter.Allows(thing))
            {
              // Check if within search radius
              if (searchRadius > 0f && (thing.Position - processor.Position).LengthHorizontal > searchRadius)
                continue;

              // For dynamic recipes, check "Do until X" target count
              if (settings.useDynamicOutput)
              {
                ThingDef product = RawToFinishedRegistry.GetFinished(thing.def);
                if (product != null)
                {
                  Bill_Production billProduction = bill as Bill_Production;
                  // Check if bill is in "Do until you have X" mode (TargetCount)
                  if (billProduction != null && billProduction.repeatMode?.defName == "TargetCount")
                  {
                    int totalInMap = GetCountOfDefInMap(pawn, product, billProduction);
                    if (totalInMap >= billProduction.targetCount)
                    {
                      continue; // Already have enough of this output, skip this ingredient
                    }
                  }
                }
              }

              // All checks passed - this is a valid ingredient
              foundThings.Add(thing);

              // If we only need one valid thing, return immediately
              if (stopAtFirst)
                return foundThings;
            }
          }
        }
      }

      if (!ingredient.categoryDefs.NullOrEmpty())
      {
        foreach (var cat in ingredient.categoryDefs)
        {
          foreach (ThingDef def in cat.DescendantThingDefs)
          {
            // Iterate through ALL matching things to find valid ones
            foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(def))
            {
              // Basic checks: forbidden, reachable, ingredient filter
              if (!thing.IsForbidden(pawn) &&
                  pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly) &&
                  bill.ingredientFilter.Allows(thing))
              {
                // Check if within search radius
                if (searchRadius > 0f && (thing.Position - processor.Position).LengthHorizontal > searchRadius)
                  continue;

                // For dynamic recipes, check "Do until X" target count
                if (settings.useDynamicOutput)
                {
                  ThingDef product = RawToFinishedRegistry.GetFinished(thing.def);
                  if (product != null)
                  {
                    Bill_Production billProduction = bill as Bill_Production;
                    // Check if bill is in "Do until you have X" mode (TargetCount)
                    if (billProduction != null && billProduction.repeatMode?.defName == "TargetCount")
                    {
                      int totalInMap = GetCountOfDefInMap(pawn, product, billProduction);
                      if (totalInMap >= billProduction.targetCount)
                      {
                        continue; // Already have enough of this output, skip this ingredient
                      }
                    }
                  }
                }

                // All checks passed - this is a valid ingredient
                foundThings.Add(thing);

                // If we only need one valid thing, return immediately
                if (stopAtFirst)
                  return foundThings;
              }
            }
          }
        }
      }

      if (foundThings.NullOrEmpty())
        return null;

      return foundThings;
    }
  }
}
