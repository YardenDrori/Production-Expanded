using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class JobDriver_StartNextProcessorCycle : JobDriver
  {
    private const TargetIndex ProcessorInd = TargetIndex.A;

    private const int Duration = 200;

    protected Building_WorkTable Processor =>
      (Building_WorkTable)job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
      return (pawn.Reserve(Processor, job, 1, -1, null, errorOnFailed));
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
      CompResourceProcessor processorComp = Processor.GetComp<CompResourceProcessor>();
      this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
      this.FailOnBurningImmobile(TargetIndex.A);

      // End the job if the processor no longer needs cycle start
      AddEndCondition(() =>
        processorComp.getIsWaitingForNextCycle() ? JobCondition.Ongoing : JobCondition.Succeeded
      );

      yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
      yield return Toils_General
        .Wait(300)
        .FailOnDestroyedNullOrForbidden(TargetIndex.A)
        .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
        .WithProgressBarToilDelay(TargetIndex.A);
      Toil toil = ToilMaker.MakeToil("MakeNewToils");
      toil.initAction = delegate
      {
        processorComp.StartNextCycle();
      };
      toil.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return toil;
    }
  }
}
