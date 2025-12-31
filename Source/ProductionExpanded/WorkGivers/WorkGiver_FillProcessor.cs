using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class WorkGiver_FillProcessor : WorkGiver_Scanner
  {
    private static string NoMaterialsTrans;
    private static string AlreadyFinishedTrans;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForUndefined();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
      MapComponent_ProcessorTracker tracker =
        pawn.Map.GetComponent<MapComponent_ProcessorTracker>();
      if (tracker == null)
      {
        yield break;
      }

      foreach (Building_Processor processor in tracker.processorsNeedingFill)
      {
        yield return processor;
      }
    }

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public static void ResetStaticData()
    {
      NoMaterialsTrans = "NoMaterials".Translate();
      AlreadyFinishedTrans = "AlreadyFinished".Translate();
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      Building_Processor processor = t as Building_Processor;
      if (processor == null)
        return false;

      CompResourceProcessor comp = processor.GetComp<CompResourceProcessor>();
      if (comp == null)
      {
        Log.Error(
          $"[Production Expanded] Building {t.def.defName} is tracked but has no CompResourceProcessor"
        );
        return false;
      }

      if (comp.getCapacityRemaining() <= 0)
      {
        JobFailReason.IsSilent();
        return false;
      }

      if (comp.getIsFinished())
      {
        JobFailReason.IsSilent();
        return false;
      }

      // Check active bills
      if (processor.activeBills.Count == 0)
      {
        JobFailReason.IsSilent();
        return false;
      }

      if (!pawn.CanReserve(t, 1, -1, null, forced))
      {
        return false;
      }

      if (t.def.hasInteractionCell && !pawn.CanReserveSittableOrSpot(t.InteractionCell, t, forced))
      {
        return false;
      }

      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
      {
        return false;
      }

      if (t.IsBurning())
      {
        return false;
      }

      // Check if we can find materials for any active bill
      // Optimization: Get generic list of valid ingredients first?
      // Or iterate bills? Iterating bills is safer for logic.

      foreach (ProcessBill bill in processor.activeBills)
      {
        // Check if bill is suspended (if we add that later) or filled
        if (bill.IsFulfilled())
          continue;

        //checks to make sure correct employee is working
        if (bill.allowedWorker == AllowedWorker.Slave && !pawn.IsSlave)
          return false;
        if (bill.allowedWorker == AllowedWorker.Mech && !pawn.IsColonyMechPlayerControlled)
          return false;
        if (bill.allowedWorker == AllowedWorker.SpecificPawn && pawn != bill.worker)
          return false;

        Thing foundThing = FindMaterials(pawn, bill);
        if (foundThing == null)
          continue;

        if (comp.GetActiveBill() == null)
        {
          return true;
        }
        else if (comp.getInputItem() == foundThing.def)
        {
          return true;
        }
      }

      JobFailReason.Is(NoMaterialsTrans);
      return false;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      Building_Processor processor = t as Building_Processor;
      if (processor == null)
        return null;

      foreach (ProcessBill bill in processor.activeBills)
      {
        if (bill.IsFulfilled())
          continue;

        Thing thing = FindMaterials(pawn, bill);
        if (thing != null)
        {
          Job job = JobMaker.MakeJob(JobDefOf_ProductionExpanded.PE_FillProcessor, t, thing);
          // We can't attach our custom bill to job.bill (which expects vanilla Bill)
          // But we can process it in the JobDriver using the ingredient to find the bill again
          // Or we could cast/interface but Job.bill is strict.
          // For now, the driver will re-resolve or we pass info via target indices.
          return job;
        }
      }
      return null;
    }

    private Thing FindMaterials(Pawn pawn, ProcessBill bill)
    {
      if (bill.processFilter == null)
        return null;

      Predicate<Thing> validator = (Thing x) =>
        (!x.IsForbidden(pawn) && pawn.CanReserve(x) && bill.processFilter.Allows(x.def))
          ? true
          : false;

      foreach (ThingDef def in bill.processFilter.allowedIngredients)
      {
        Thing found = GenClosest.ClosestThingReachable(
          pawn.Position,
          pawn.Map,
          ThingRequest.ForDef(def),
          PathEndMode.ClosestTouch,
          TraverseParms.For(pawn),
          bill.ingredientSearchRadius,
          validator
        );

        if (found != null)
          return found;
      }
      return null;
    }
  }
}
