using System.Collections.Generic;
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
      {
        return false;
      }

      CompResourceProcessor comp = processor.GetComp<CompResourceProcessor>();
      if (comp == null || !comp.getIsReady() || comp.getCapacityRemaining() <= 0)
        return false;

      if (t.IsForbidden(pawn))
        return false;
      if (!pawn.CanReserve(t, 1, -1, null, forced))
        return false;

      //refuel running processor
      if (comp.getIsProcessing())
      {
        // Only accept matching input
        foreach (var ingredient in comp.GetAllIngredientsAndTheirCounts())
        {
          if (ingredient.Key.IsNull)
          {
            return false;
          }
          //not allowed to refuel static ingredients
          if (ingredient.Key.count != -1)
          {
            continue;
          }
          if (
            ingredient.Value < comp.MaxCountOfIngredientInRecipe(ingredient.Key)
            && FindIngredient(
              pawn,
              processor,
              ingredient.Key,
              comp.GetActiveBill().ingredientSearchRadius
            ) != null
          )
          {
            return true;
          }
        }
        comp.PunishProcessor();
        return false;
        // return FindIngredient(pawn, processor, comp.getInputItem()) != null;
      }

      //start new bill
      foreach (Bill bill in processor.BillStack)
      {
        if (bill.ShouldDoNow() && bill.recipe.fixedIngredientFilter != null)
        {
          RecipeExtension_Processor settings =
            bill.recipe.GetModExtension<RecipeExtension_Processor>();
          if (settings == null)
          {
            Log.Error(
              $"[Production Expanded] {bill.recipe.defName} does not have RecipeExtension_Processor"
            );
            return false;
          }
          foreach (ProcessorIngredient ingredient in settings.ingredients)
          {
            if (FindIngredient(pawn, processor, ingredient, bill.ingredientSearchRadius) != null)
            {
              comp.forgivePunishment();
              return true;
            }
          }
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
        foreach (var recipeIngredient in comp.GetAllIngredientsAndTheirCounts())
        {
          if (recipeIngredient.Key.IsNull)
            return null;
          //not allowed to refuel static ingredients
          if (recipeIngredient.Key.count != -1)
            continue;

          int ingredientNeeded =
            comp.MaxCountOfIngredientInRecipe(recipeIngredient.Key) - recipeIngredient.Value;
          if (ingredientNeeded > 0)
          {
            Thing ingredient = FindIngredient(
              pawn,
              processor,
              recipeIngredient.Key,
              comp.GetActiveBill().ingredientSearchRadius
            );
            if (ingredient != null)
            {
              Job job = JobMaker.MakeJob(
                JobDefOf_ProductionExpanded.PE_FillProcessor,
                t,
                ingredient
              );
              job.count = Mathf.Min(ingredient.stackCount, ingredientNeeded);
              comp.forgivePunishment();
              return job;
            }
          }
        }
        comp.PunishProcessor();
        return null;
      }

      foreach (Bill_Production bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        RecipeExtension_Processor settings =
          bill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings != null)
        {
          foreach (ProcessorIngredient recipeIngredient in settings.ingredients)
          {
            if (recipeIngredient.IsNull)
            {
              return null;
            }
            Thing ingredient = FindIngredient(
              pawn,
              processor,
              recipeIngredient,
              bill.ingredientSearchRadius
            );
            if (ingredient != null)
            {
              int ingredientsNeeded = comp.MaxCountOfIngredientInRecipe(recipeIngredient);

              Job job = JobMaker.MakeJob(
                JobDefOf_ProductionExpanded.PE_FillProcessor,
                t,
                ingredient
              );
              job.count = Mathf.Min(ingredient.stackCount, ingredientsNeeded);
              job.bill = bill;
              comp.forgivePunishment();
              return job;
            }
          }
        }
        //should prob log here
      }
      comp.PunishProcessor();
      return null;
    }

    private Thing FindIngredient(
      Pawn pawn,
      Building_Processor processor,
      ProcessorIngredient ingredient,
      float searchRadius
    )
    {
      if (ingredient.IsSpecific)
      {
        return GenClosest.ClosestThingReachable(
          pawn.Position,
          pawn.Map,
          ThingRequest.ForDef(ingredient.thingDef),
          PathEndMode.ClosestTouch,
          TraverseParms.For(pawn),
          searchRadius,
          (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x)
        );
      }
      if (ingredient.IsCategory)
      {
        Thing closest = null;
        float closestDist = float.MaxValue;
        foreach (ThingDef def in ingredient.category.childThingDefs)
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
            float dist = (found.Position - pawn.Position).LengthHorizontalSquared;
            if (dist < closestDist)
            {
              closest = found;
              closestDist = dist;
            }
          }
        }
        return closest;
      }
      return null;
    }
  }
}
