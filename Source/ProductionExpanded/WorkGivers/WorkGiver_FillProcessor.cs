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
      // Use our tracker - return a copy to prevent concurrent modification
      var tracker = pawn.Map?.GetComponent<MapComponent_ProcessorTracker>();
      if (tracker != null)
      {
        // Create a new list to prevent concurrent modification while multiple pawns iterate
        return new List<Thing>(tracker.processorsNeedingFill);
      }
      Log.Error("[Production Expanded] no mak cache found, fuck you! nah im joking i <3 you!");
      return base.PotentialWorkThingsGlobal(pawn);
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
        // Only accept matching input
        return FindIngredient(pawn, processor, comp.getInputItem()) != null;
      }

      // Check bills
      foreach (Bill bill in processor.BillStack)
      {
        if (bill.ShouldDoNow() && bill.recipe.fixedIngredientFilter != null)
        {
          if (FindIngredientForBill(pawn, processor, (Bill_Production)bill) != null)
            return true;
        }
      }
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
        ThingDef requiredDef = comp.getInputItem();
        if (requiredDef == null)
          return null;

        Thing ingredient = FindIngredient(pawn, processor, requiredDef);
        if (ingredient != null)
        {
          Job job = JobMaker.MakeJob(
            DefDatabase<JobDef>.GetNamed("PE_FillProcessor"),
            t,
            ingredient
          );
          job.count = Mathf.Min(ingredient.stackCount, comp.getCapacityRemaining());
          return job;
        }
        return null;
      }

      foreach (Bill_Production bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        if (bill.recipe.fixedIngredientFilter != null)
        {
          Thing ingredient = FindIngredientForBill(pawn, processor, bill);
          if (ingredient != null)
          {
            Job job = JobMaker.MakeJob(
              DefDatabase<JobDef>.GetNamed("PE_FillProcessor"),
              t,
              ingredient
            );
            job.count = Mathf.Min(ingredient.stackCount, comp.getCapacityRemaining());
            job.bill = bill; // Vanilla field
            return job;
          }
        }
      }

      return null;
    }

    private Thing FindIngredient(Pawn pawn, Building_Processor processor, ThingDef def)
    {
      return GenClosest.ClosestThingReachable(
        pawn.Position,
        pawn.Map,
        ThingRequest.ForDef(def),
        PathEndMode.ClosestTouch,
        TraverseParms.For(pawn),
        9999f,
        (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x)
      );
    }

    private Thing FindIngredientForBill(
      Pawn pawn,
      Building_Processor processor,
      Bill_Production bill
    )
    {
      return GenClosest.ClosestThingReachable(
        pawn.Position,
        pawn.Map,
        ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
        PathEndMode.ClosestTouch,
        TraverseParms.For(pawn),
        bill.ingredientSearchRadius,
        (Thing x) =>
          !x.IsForbidden(pawn)
          && pawn.CanReserve(x)
          && bill.recipe.fixedIngredientFilter.Allows(x)
          && bill.ingredientFilter.Allows(x)
      );
    }
  }
}
