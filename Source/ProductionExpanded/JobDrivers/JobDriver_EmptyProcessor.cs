using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class JobDriver_EmptyProcessor : JobDriver
  {
    // C is used for Destination Cell

    protected Building_Processor Processor => (Building_Processor)job.GetTarget(TargetIndex.A).Thing;

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

      yield return Toils_General.Wait(90)
        .FailOnDestroyedNullOrForbidden(TargetIndex.A)
        .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
        .WithProgressBarToilDelay(TargetIndex.A);

      // Capture bill settings from vanilla system
      BillStoreModeDef storeMode = BillStoreModeDefOf.BestStockpile;
      Zone_Stockpile specificStockpile = null;

      Toil captureSettings = ToilMaker.MakeToil("CaptureSettings");
      captureSettings.initAction = delegate
      {
        CompResourceProcessor comp = Processor.GetComp<CompResourceProcessor>();
        Bill_Production active = comp?.GetActiveBill();
        if (active != null)
        {
          storeMode = active.GetStoreMode();
          ISlotGroup group = active.GetSlotGroup();
          if (group is SlotGroup slotGroup && slotGroup.parent is Zone_Stockpile zone)
          {
            specificStockpile = zone;
          }
        }
      };
      yield return captureSettings;

      // Empty the processor
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
          // If no item spawned or invalid
          if (storeMode != BillStoreModeDefOf.DropOnFloor)
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
          else
            pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
      };
      findItemsToil.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return findItemsToil;

      // Check Drop Mode
      Toil checkStoreMode = ToilMaker.MakeToil("CheckStoreMode");
      checkStoreMode.initAction = delegate
      {
        if (storeMode == BillStoreModeDefOf.DropOnFloor)
        {
          pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
      };
      yield return checkStoreMode;

      // Reserve items
      yield return Toils_Reserve.Reserve(TargetIndex.B);

      // Pick up
      yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
      yield return Toils_Haul.StartCarryThing(TargetIndex.B);

      // Find destination
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

        // Specific Stockpile
        if (storeMode == BillStoreModeDefOf.SpecificStockpile && specificStockpile != null)
        {
          // Vanilla helper might be better here, but StoreUtility is good
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

        // Best Stockpile (Fallback)
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
          // No storage found, drop
          actor.jobs.EndCurrentJob(JobCondition.Succeeded);
        }
      };
      findDest.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return findDest;

      // Haul
      yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
      yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, null, false);
    }
  }
}
