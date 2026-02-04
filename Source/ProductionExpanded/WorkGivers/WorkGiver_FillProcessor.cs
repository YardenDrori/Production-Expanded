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
        if (settings?.ingredients == null)
          return false;

        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          var ing = settings.ingredients[i];
          if (!ing.IsScaling)
            continue;

          int needed = comp.GetAmountNeeded(ing);
          if (needed <= 0)
            continue;

          if (FindIngredient(pawn, processor, ing, comp.GetActiveBill().ingredientSearchRadius) != null)
          {
            comp.forgivePunishment();
            return true;
          }
        }
        comp.PunishProcessor();
        return false;
      }

      // Start new bill — find any bill with available ingredients
      foreach (Bill bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings?.ingredients == null)
        {
          Log.Error($"[Production Expanded] {bill.recipe.defName} has no RecipeExtension_Processor");
          continue;
        }

        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          if (FindIngredient(pawn, processor, settings.ingredients[i], bill.ingredientSearchRadius) != null)
          {
            comp.forgivePunishment();
            return true;
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

          Thing ingredient = FindIngredient(pawn, processor, ing, comp.GetActiveBill().ingredientSearchRadius);
          if (ingredient != null)
          {
            Job job = JobMaker.MakeJob(
              JobDefOf_ProductionExpanded.PE_FillProcessor,
              t,
              ingredient
            );
            job.count = Mathf.Min(ingredient.stackCount, needed);
            job.bill = comp.GetActiveBill();
            comp.forgivePunishment();
            return job;
          }
        }
        comp.PunishProcessor();
        return null;
      }

      // Start new bill
      foreach (Bill_Production bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings?.ingredients == null)
          continue;

        for (int i = 0; i < settings.ingredients.Count; i++)
        {
          var ing = settings.ingredients[i];
          Thing ingredient = FindIngredient(pawn, processor, ing, bill.ingredientSearchRadius);
          if (ingredient != null)
          {
            int needed = ing.IsFixed
              ? ing.count
              : comp.MaxCountOfIngredientInRecipe(ing);

            Job job = JobMaker.MakeJob(
              JobDefOf_ProductionExpanded.PE_FillProcessor,
              t,
              ingredient
            );
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

    private Thing FindIngredient(
      Pawn pawn,
      Building_Processor processor,
      ProcessorIngredient ingredient,
      float searchRadius
    )
    {
      if (ingredient.IsSpecific)
      {
        for (int i = 0; i < ingredient.thingDefs.Count; i++)
        {
          Thing foundThing = GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForDef(ingredient.thingDefs[i]),
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
      if (ingredient.IsCategory)
      {
        Thing closest = null;
        float closestDist = float.MaxValue;
        for (int c = 0; c < ingredient.categories.Count; c++)
        {
          foreach (ThingDef def in ingredient.categories[c].DescendantThingDefs)
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
        }
        if (closest != null)
        {
          return closest;
        }
      }
      return null;
    }
  }
}
