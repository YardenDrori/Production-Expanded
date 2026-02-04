using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class JobDriver_FillProcessor : JobDriver
  {
    private const TargetIndex ProcessorInd = TargetIndex.A;
    private const TargetIndex MaterialsInd = TargetIndex.B;

    protected Building_Processor Processor => (Building_Processor)job.GetTarget(ProcessorInd).Thing;
    protected Thing Materials => job.GetTarget(MaterialsInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
      if (pawn.Reserve(Processor, job, 1, -1, null, errorOnFailed))
      {
        return pawn.Reserve(Materials, job, 1, -1, null, errorOnFailed);
      }
      return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
      CompResourceProcessor processorComp = Processor.GetComp<CompResourceProcessor>();
      this.FailOnDespawnedNullOrForbidden(ProcessorInd);
      this.FailOnBurningImmobile(ProcessorInd);

      AddEndCondition(() =>
        processorComp.getCapacityRemaining() > 0
          ? JobCondition.Ongoing
          : JobCondition.Succeeded
      );

      Toil reserveMaterials = Toils_Reserve.Reserve(MaterialsInd);
      yield return reserveMaterials;

      yield return Toils_Goto.GotoThing(MaterialsInd, PathEndMode.ClosestTouch)
        .FailOnDespawnedNullOrForbidden(MaterialsInd)
        .FailOnSomeonePhysicallyInteracting(MaterialsInd);

      yield return Toils_Haul.StartCarryThing(MaterialsInd, false, true)
        .FailOnDestroyedNullOrForbidden(MaterialsInd);

      yield return Toils_Haul.CheckForGetOpportunityDuplicate(
        reserveMaterials,
        MaterialsInd,
        TargetIndex.None,
        true
      );

      yield return Toils_Goto.GotoThing(ProcessorInd, PathEndMode.Touch);

      yield return Toils_General.Wait(200)
        .FailOnDestroyedNullOrForbidden(MaterialsInd)
        .FailOnDestroyedNullOrForbidden(ProcessorInd)
        .FailOnCannotTouch(ProcessorInd, PathEndMode.Touch)
        .WithProgressBarToilDelay(ProcessorInd);

      Toil depositToil = ToilMaker.MakeToil("FillProcessor");
      depositToil.initAction = delegate
      {
        Bill_Production bill = (Bill_Production)job.bill;

        // If refueling an active processor, use the stored active bill
        if (bill == null && processorComp.getIsProcessing())
        {
          bill = processorComp.GetActiveBill();
        }

        if (bill != null)
        {
          processorComp.AddMaterials(bill, Materials);
        }
        else
        {
          Log.Warning(
            "[Production Expanded] Pawn arrived at processor but no valid bill found. Dropping."
          );
          GenPlace.TryPlaceThing(Materials, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }
      };
      depositToil.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return depositToil;
    }
  }
}
