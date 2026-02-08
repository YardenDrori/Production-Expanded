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
      if (comp == null || !comp.getIsReady() || comp.getCapacityRemaining() <= 0)
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
        int found = FindHowManyItemsExistForIngredient(
          pawn,
          processor,
          bill.ingredientSearchRadius,
          ing
        );

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

        // Static recipes require all ingredients before starting
        if (settings.isStaticRecipe)
        {
          bool allSlotsFulfilled = true;
          foreach (var ing in settings.ingredients)
          {
            int countNeeded = ing.count;
            int countAvailableStatic = FindHowManyItemsExistForIngredient(
              pawn,
              processor,
              bill.ingredientSearchRadius,
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
        int minCountNeeded = (int)(1 / settings.ratio);
        int countAvailableDynamic = FindHowManyItemsExistForIngredient(
          pawn,
          processor,
          bill.ingredientSearchRadius,
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
      if (comp == null || !comp.getIsReady() || comp.getCapacityRemaining() <= 0)
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
          bill.ingredientSearchRadius,
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
              bill.ingredientSearchRadius,
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
          if (settings.ingredients[0] == null)
            continue;

          List<Thing> availableIngredients = FindIngredient(
            pawn,
            processor,
            bill.ingredientSearchRadius,
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
      float searchRadius,
      ProcessorIngredient ingredient
    )
    {
      List<Thing> ingredientsFound = FindIngredient(
        pawn,
        processor,
        searchRadius,
        ingredient,
        stopAtFirst: false
      );
      return CountThingStackCountTotalFromList(ingredientsFound);
    }

    //finds Thing of either specific thingdef or in a specific category
    private List<Thing> FindIngredient(
      Pawn pawn,
      Building_Processor processor,
      float searchRadius,
      ProcessorIngredient ingredient,
      bool stopAtFirst
    )
    {
      List<Thing> foundThings = new();
      if (!ingredient.thingDefs.NullOrEmpty())
      {
        foreach (var ingredientDef in ingredient.thingDefs)
        {
          Thing found = GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForDef(ingredientDef),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            searchRadius,
            (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x)
          );
          if (found != null)
          {
            foundThings.Add(found);
            if (stopAtFirst)
              return foundThings;
          }
        }
      }
      if (!ingredient.categoryDefs.NullOrEmpty())
      {
        foreach (var cat in ingredient.categoryDefs)
        {
          foreach (ThingDef def in cat.DescendantThingDefs)
          {
            Thing found = GenClosest.ClosestThingReachable(
              pawn.Position,
              pawn.Map,
              ThingRequest.ForDef(def),
              PathEndMode.ClosestTouch,
              TraverseParms.For(pawn),
              searchRadius,
              (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x)
            );
            if (found != null)
            {
              foundThings.Add(found);
              if (stopAtFirst)
                return foundThings;
            }
          }
        }
      }
      if (foundThings.NullOrEmpty())
      {
        return null;
      }
      return foundThings;
    }
  }
}
