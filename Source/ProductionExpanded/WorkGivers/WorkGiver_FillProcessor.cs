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

      foreach (Building_WorkTable processor in tracker.processorsNeedingFill)
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
      Building_WorkTable workTable = t as Building_WorkTable;

      CompResourceProcessor comp = workTable.TryGetComp<CompResourceProcessor>();
      if (comp == null)
      {
        Log.Error(
          "[Production Expanded] Building {t.def.defName} does not have CompResourceProcessor but was still in the Cache of work tables in need of filling"
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

      //checks if building has bills
      if (workTable.billStack.Count <= 0)
      {
        JobFailReason.IsSilent();
        return false;
      }
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
      //checks if we can access materials for one of the bills
      for (int i = 0; i < workTable.billStack.Count; i++)
      {
        if (workTable.billStack.Bills[i].suspended)
        {
          return false;
        }
        ProcessorRecipeDef curr_bill_recipe =
          workTable.billStack.Bills[i].recipe as ProcessorRecipeDef;
        if (curr_bill_recipe == null)
        {
          Log.Warning(
            $"[Production Expanded] Bill of id {i} in building {t.def.defName} has a recipe of a different type other than ProcessorRecipeDef ({workTable.billStack.Bills[i].recipe.GetType()})"
          ); //fix this please im fine with you editing the code here yourself
          continue;
        }
        if (FindMaterials(pawn, curr_bill_recipe) != null)
        {
          return true;
        }
      }
      JobFailReason.Is(NoMaterialsTrans);
      return false;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      Building_WorkTable workTable = t as Building_WorkTable;
      for (int i = 0; i < workTable.billStack.Count; i++)
      {
        ProcessorRecipeDef curr_bill_recipe =
          workTable.billStack.Bills[i].recipe as ProcessorRecipeDef;
        if (curr_bill_recipe == null)
        {
          Log.Warning(
            $"[Production Expanded] Bill of id {i} in building {t.def.defName} has a recipe of a different type other than ProcessorRecipeDef ({workTable.billStack.Bills[i].recipe.GetType()})"
          ); //fix this please im fine with you editing the code here yourself
          continue;
        }
        Thing thing = FindMaterials(pawn, curr_bill_recipe);
        if (thing != null)
        {
          Job job = JobMaker.MakeJob(JobDefOf_ProductionExpanded.PE_FillProcessor, t, thing);
          job.bill = workTable.billStack[i];
          return job;
        }
      }
      return null;
    }

    private Thing FindMaterials(Pawn pawn, ProcessorRecipeDef recipe)
    {
      Predicate<Thing> validator = (Thing x) =>
        (!x.IsForbidden(pawn) && pawn.CanReserve(x)) ? true : false;

      // For generic recipes (inputType = null), search for items in PE_RawLeathers category
      if (recipe.inputType == null)
      {
        var rawLeathersCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("PE_RawLeathers");
        if (rawLeathersCategory != null)
        {
          // Find any raw leather
          return GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            9999f,
            (Thing x) =>
              validator(x)
              && x.def.thingCategories != null
              && x.def.thingCategories.Contains(rawLeathersCategory)
          );
        }
        return null;
      }

      // For specific input recipes, use the inputType
      ThingDef input = recipe.inputType;
      return GenClosest.ClosestThingReachable(
        pawn.Position,
        pawn.Map,
        ThingRequest.ForDef(input),
        PathEndMode.ClosestTouch,
        TraverseParms.For(pawn),
        9999f,
        validator
      );
    }
  }
}
