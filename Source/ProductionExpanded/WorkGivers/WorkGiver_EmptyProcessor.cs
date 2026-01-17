using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class WorkGiver_EmptyProcessor : WorkGiver_Scanner
  {
    private static string ProcessorNotFinishedTrans;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForUndefined();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
      return pawn.Map?.GetComponent<MapComponent_ProcessorTracker>()?.processorsNeedingEmpty;
    }

    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public static void ResetStaticData()
    {
      ProcessorNotFinishedTrans = "ProcessorNotFinished".Translate();
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      CompResourceProcessor comp = t.TryGetComp<CompResourceProcessor>();

      // Check if pawn can reserve the processor
      if (!pawn.CanReserve(t, 1, -1, null, forced))
      {
        return false;
      }

      // Check if pawn can reserve interaction cell
      if (t.def.hasInteractionCell && !pawn.CanReserveSittableOrSpot(t.InteractionCell, t, forced))
      {
        return false;
      }

      // Check if building is burning
      if (t.IsBurning())
      {
        return false;
      }

      // Check if building marked for deconstruction
      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
      {
        return false;
      }

      return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      return JobMaker.MakeJob(JobDefOf_ProductionExpanded.PE_EmptyProcessor, t);
    }
  }
}
