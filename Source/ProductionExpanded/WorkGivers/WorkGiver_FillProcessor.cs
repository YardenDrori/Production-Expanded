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
        var settings = comp.GetActiveBill()?.recipe?.GetModExtension<RecipeExtension_Processor>();

        // static recipes lock processor
        if (settings?.isStaticRecipe == true)
          return false;
        //invalid state
        if (settings?.ingredients == null || settings?.ingredients[0] == null)
          return false;
        //no room
        if (comp.getCapacityRemaining() <= 0)
          return false;

        var ing = settings.ingredients[0];
        bool found = false;

        foreach (var proIng in ing.thingDefs)
        {
          Thing foundIng = FindIngredient(
            pawn,
            processor,
            comp.GetActiveBill().ingredientSearchRadius,
            null,
            proIng
          );
          if (foundIng != null)
          {
            found = true;
            break;
          }
        }
        if (!found)
        {
          foreach (var proIng in ing.categoryDefs)
          {
            Thing foundIng = FindIngredient(
              pawn,
              processor,
              comp.GetActiveBill().ingredientSearchRadius,
              proIng,
              null
            );
            if (foundIng != null)
            {
              found = true;
              break;
            }
          }
        }
        if (!found)
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

        bool allIngredientsAvailable = true;
        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          if (
            FindIngredient(pawn, processor, settings.ingredients[i], bill.ingredientSearchRadius)
            == null
          )
          {
            allIngredientsAvailable = false;
            break;
          }
        }
        if (allIngredientsAvailable)
        {
          comp.forgivePunishment();
          return true;
        }
      }
      comp.PunishProcessor();
      return false;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      if (!(t is Building_Processor processor))
        return null;

      CompResourceProcessor comp = processor.GetComp<CompResourceProcessor>();
      if (comp == null || !comp.getIsReady() || comp.getCapacityRemaining() <= 0)
        return null;

      if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        return null;

      if (comp.getIsProcessing())
      {
        // Refuel running processor
        var settings = comp.GetActiveBill()?.recipe?.GetModExtension<RecipeExtension_Processor>();
        if (settings?.ingredients == null)
          return null;

        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          var ing = settings.ingredients[i];
          if (!ing.IsScaling)
            continue;

          int needed = comp.GetAmountNeeded(ing);
          if (needed <= 0)
            continue;

          Thing ingredient = FindIngredient(
            pawn,
            processor,
            ing,
            comp.GetActiveBill().ingredientSearchRadius
          );
          if (ingredient != null)
          {
            Job job = JobMaker.MakeJob(JobDefOf_ProductionExpanded.PE_FillProcessor, t, ingredient);
            job.count = Mathf.Min(ingredient.stackCount, needed);
            job.bill = comp.GetActiveBill();
            comp.forgivePunishment();
            return job;
          }
        }
        comp.PunishProcessor();
        return null;
      }

      // Start new bill — only if ALL ingredients are available on the map
      foreach (Bill_Production bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings?.ingredients == null)
          continue;

        // First pass: verify every ingredient type exists on the map
        bool allAvailable = true;
        List<List<Thing>> allPossibleItemsPerIngredient = new();
        List<int> availableTotalCountOfThingsPerIngredient = new();
        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          var possibleItemsForIngredient = FindIngredient(
            pawn,
            processor,
            settings.ingredients[i],
            bill.ingredientSearchRadius,
            true
          );
          if (possibleItemsForIngredient.NullOrEmpty())
          {
            allAvailable = false;
            break;
          }
          else
          {
            availableTotalCountOfThingsPerIngredient.Add(
              possibleItemsForIngredient.Sum(item => item.stackCount)
            );
          }
        }
        if (!allAvailable)
          continue;

        //second pass figure out how many of each item we need
        int craftCountBottleneck = -1;
        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          int amountNeeded = comp.GetAmountNeeded(settings.ingredients[i]);
        }

        // Second pass: create job for the first ingredient we can haul
        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          var ing = settings.ingredients[i];
          Thing ingredient = FindIngredient(pawn, processor, ing, bill.ingredientSearchRadius);
          if (ingredient != null)
          {
            int needed = ing.IsFixed ? ing.count : comp.MaxCountOfIngredientInRecipe(ing);

            Job job = JobMaker.MakeJob(JobDefOf_ProductionExpanded.PE_FillProcessor, t, ingredient);
            job.count = Mathf.Min(ingredient.stackCount, needed);
            job.bill = bill;
            comp.forgivePunishment();
            return job;
          }
        }
      }
      comp.PunishProcessor();
      return null;
    }

    //finds Thing of either specific thingdef or in a specific category
    private Thing FindIngredient(
      Pawn pawn,
      Building_Processor processor,
      float searchRadius,
      ThingCategoryDef ingredientCat = null,
      ThingDef ingredientDef = null
    )
    {
      if (ingredientDef != null)
      {
        return GenClosest.ClosestThingReachable(
          pawn.Position,
          pawn.Map,
          ThingRequest.ForDef(ingredientDef),
          PathEndMode.ClosestTouch,
          TraverseParms.For(pawn),
          searchRadius,
          (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x)
        );
      }
      if (ingredientCat != null)
      {
        foreach (ThingDef def in ingredientCat.DescendantThingDefs)
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
            return found;
          }
        }
      }
      return null;
    }
  }
}
