using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class JobDriver_EmptyProcessor : JobDriver
  {
    private const TargetIndex ProcessorInd = TargetIndex.A;
    private const TargetIndex ItemInd = TargetIndex.B;
    // C is used for Destination Cell

    protected Building_Processor Processor =>
      (Building_Processor)job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
      return pawn.Reserve(Processor, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
      this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
      this.FailOnBurningImmobile(TargetIndex.A);

      // Go to processor's interaction cell
      yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

      yield return Toils_General
        .Wait(90)
        .FailOnDestroyedNullOrForbidden(TargetIndex.A)
        .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
        .WithProgressBarToilDelay(TargetIndex.A);

      // Capture bill settings - ActionWithOutput
      ActionWithOutput storeMode = ActionWithOutput.HaulToBestStockpile;
      Zone_Stockpile specificStockpile = null;

      Toil captureSettings = ToilMaker.MakeToil("CaptureSettings");
      captureSettings.initAction = delegate
      {
        CompResourceProcessor comp = Processor.GetComp<CompResourceProcessor>();
        ProcessBill active = comp?.GetActiveBill();
        if (active != null)
        {
          storeMode = active.actionWithOutput;
          specificStockpile = active.destinationStockpile;
        }
      };
      yield return captureSettings;

      // Empty the processor (this spawns items at interaction cell)
      Toil emptyToil = ToilMaker.MakeToil("EmptyProcessor");
      emptyToil.initAction = delegate
      {
        CompResourceProcessor comp = Processor.GetComp<CompResourceProcessor>();
        if (comp != null)
        {
          comp.EmptyBuilding();
        }
      };
      emptyToil.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return emptyToil;

      // Find and target the spawned items
      Toil findItemsToil = ToilMaker.MakeToil("FindSpawnedItems");
      findItemsToil.initAction = delegate
      {
        Thing item = Processor.InteractionCell.GetFirstItem(pawn.Map);
        if (item != null)
        {
          job.SetTarget(TargetIndex.B, item);
          job.count = item.stackCount;
        }
        else
        {
          if (storeMode != ActionWithOutput.DropOnFloor)
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
          else
            pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
      };
      findItemsToil.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return findItemsToil;

      // Check Store Mode logic
      Toil checkStoreMode = ToilMaker.MakeToil("CheckStoreMode");
      checkStoreMode.initAction = delegate
      {
        if (storeMode == ActionWithOutput.DropOnFloor)
        {
          pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
      };
      yield return checkStoreMode;

      // Reserve the spawned items
      yield return Toils_Reserve.Reserve(TargetIndex.B);

      // Go pick up the items
      yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
      yield return Toils_Haul.StartCarryThing(TargetIndex.B);

      // Find destination for hauling
      Toil findDest = ToilMaker.MakeToil("FindDestination");
      findDest.initAction = delegate
      {
        Pawn actor = pawn;
        Thing item = actor.carryTracker.CarriedThing;
        if (item == null)
        {
          actor.jobs.EndCurrentJob(JobCondition.Incompletable);
          return;
        }

        IntVec3 dest = IntVec3.Invalid;
        bool found = false;

        // Try specific stockpile if requested
        if (storeMode == ActionWithOutput.HaulToSpecificStockpile && specificStockpile != null)
        {
          foreach (IntVec3 cell in specificStockpile.Cells)
          {
            if (StoreUtility.IsGoodStoreCell(cell, actor.Map, item, actor, actor.Faction))
            {
              dest = cell;
              found = true;
              break;
            }
          }
        }

        // Fallback to best stockpile if specific failed or not requested
        if (!found)
        {
          if (StoreUtility.TryFindBestBetterStoreCellFor(item, actor, actor.Map, StoragePriority.Unstored, actor.Faction, out IntVec3 c))
          {
            dest = c;
            found = true;
          }
        }

        if (found)
        {
          actor.jobs.curJob.SetTarget(TargetIndex.C, dest);
        }
        else
        {
          // No valid storage found, drop it here and end job
          actor.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
      };
      findDest.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return findDest;

      // Haul to calculated destination
      yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
      yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, null, false);
    }
  }
}
