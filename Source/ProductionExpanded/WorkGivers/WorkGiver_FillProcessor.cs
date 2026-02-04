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
        if (bill.ShouldDoNow())
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
              job.bill = comp.GetActiveBill();
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

    //this is prob bad and inefficent i genuinely dont know how to prevent this tho
    //buildings should not request any items unless the requirements for all ingredients are fulfilled
    private Dictionary<ProcessorIngredient, int> CalculateMaxOfEachItemBasedOnAvailableResources(
      List<ProcessorIngredient> ingredients,
      CompResourceProcessor comp,
      Pawn pawn,
      Building_Processor processor,
      float searchRadius
    )
    {
      int minCrafts = -1;
      Dictionary<Thing, int> result = new();
      Dictionary<Thing, int> processorIngredientsInStorage = processorIngredientsInStorage =
        comp.GetAllIngredientsAndTheirCounts();

      foreach (var ingredient in ingredients)
      {
        Thing foundIngredient = FindIngredient(pawn, processor, ingredient, searchRadius);
        if (foundIngredient == null || foundIngredient.stackCount == 0)
        {
          continue;
        }
        int alreadyInStorage = comp.GetIngredientCountInStorage(ingredient);
        //this call is very inefficient as we pretty much turn this from an O(n) opeartion
        //to an O(n^2) operation as we call this method which goes over all the ingredients once
        //for every ingredient.
        //is there a way to make this better? ABSOUTELY! do I know how to do it? HELL NO 😭
        int maxOfIngredient = comp.MaxCountOfIngredientInRecipe(ingredient);
        int ingredientCountNeeded = maxOfIngredient - alreadyInStorage;
        if (ingredientCountNeeded <= 0)
        {
          continue;
        }
        if (ingredient.IsFixed)
        {
          int maxCrafts = ingredientCountNeeded / ingredient.count;
          int availableCrafts = foundIngredient.stackCount / ingredient.count;
          int finalCrafts = Mathf.Min(availableCrafts, maxCrafts);
          if (finalCrafts < minCrafts)
            //HELP IM STUCK IM SO FUCKING FRUSTRATED
            result.Add(foundIngredient, Mathf.Min(availableCrafts, maxCrafts));
        }
        else
        {
          int maxCrafts = (int)(ingredientCountNeeded / ingredient.ratio);
          int availableCrafts = (int)(foundIngredient.stackCount / ingredient.ratio);

          result.Add(foundIngredient, Mathf.Min(availableCrafts, maxCrafts));
        }
      }
      return result;
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
        foreach (var ing in ingredient.thingDefs)
        {
          Thing foundThing = GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForDef(ing),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            searchRadius,
            (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x)
          );
          if (foundThing != null)
          {
            return foundThing;
          }
        }
      }
      //idk of a better way to search through all items of a category nor if a better way even exists
      if (ingredient.IsCategory)
      {
        foreach (var cat in ingredient.categories)
        {
          Thing closest = null;
          float closestDist = float.MaxValue;
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
              float dist = (found.Position - pawn.Position).LengthHorizontalSquared;
              if (dist < closestDist)
              {
                closest = found;
                closestDist = dist;
              }
            }
          }
          if (closest != null)
          {
            return closest;
          }
        }
      }
      return null;
    }
  }
}
