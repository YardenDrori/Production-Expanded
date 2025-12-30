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

    private const int Duration = 200;

    protected Building_WorkTable Processor =>
      (Building_WorkTable)job.GetTarget(TargetIndex.A).Thing;

    protected Thing Materials => job.GetTarget(TargetIndex.B).Thing;

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
      this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
      this.FailOnBurningImmobile(TargetIndex.A);
      AddEndCondition(() =>
        (processorComp.getCapacityRemaining() > 0) ? JobCondition.Ongoing : JobCondition.Succeeded
      );
      yield return Toils_General.DoAtomic(
        delegate
        {
          job.count = processorComp.getCapacityRemaining();
        }
      );
      Toil reserveMaterials = Toils_Reserve.Reserve(TargetIndex.B);
      yield return reserveMaterials;
      yield return Toils_Goto
        .GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
        .FailOnDespawnedNullOrForbidden(TargetIndex.B)
        .FailOnSomeonePhysicallyInteracting(TargetIndex.B);
      yield return Toils_Haul
        .StartCarryThing(
          TargetIndex.B,
          putRemainderInQueue: false,
          subtractNumTakenFromJobCount: true
        )
        .FailOnDestroyedNullOrForbidden(TargetIndex.B);
      yield return Toils_Haul.CheckForGetOpportunityDuplicate(
        reserveMaterials,
        TargetIndex.B,
        TargetIndex.None,
        takeFromValidStorage: true
      );
      yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
      yield return Toils_General
        .Wait(200)
        .FailOnDestroyedNullOrForbidden(TargetIndex.B)
        .FailOnDestroyedNullOrForbidden(TargetIndex.A)
        .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
        .WithProgressBarToilDelay(TargetIndex.A);
      Toil toil = ToilMaker.MakeToil("MakeNewToils");
      toil.initAction = delegate
      {
        processorComp.AddMaterials((Bill_Production)job.bill, Materials.stackCount, Materials);
        Materials.Destroy();
      };
      toil.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return toil;
    }
  }
}
