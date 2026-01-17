using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class WorkGiver_StartNextProcessorCycle : WorkGiver_Scanner
  {
    // private static string NoMaterialsTrans;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForUndefined();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
      return pawn.Map?.GetComponent<MapComponent_ProcessorTracker>()?.processorsNeedingCycleStart;
    }

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public static void ResetStaticData()
    {
      // NoMaterialsTrans = "NoMaterials".Translate();
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      CompResourceProcessor comp = t.TryGetComp<CompResourceProcessor>();
      //idk tbh i just coppied it from the barrel one
      if (!pawn.CanReserve(t, 1, -1, null, forced))
      {
        return false;
      }
      // Check if building has interaction cell and pawn can reserve it
      if (t.def.hasInteractionCell && !pawn.CanReserveSittableOrSpot(t.InteractionCell, t, forced))
      {
        return false;
      }
      //checks if building marked for deconstruction
      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
      {
        return false;
      }
      //checks if building is on fire
      if (t.IsBurning())
      {
        return false;
      }
      return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      return JobMaker.MakeJob(JobDefOf_ProductionExpanded.PE_StartNextProcessorCycle, t);
    }
  }
}
