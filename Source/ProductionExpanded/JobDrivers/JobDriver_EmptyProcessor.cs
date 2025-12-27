using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
    public class JobDriver_EmptyProcessor : JobDriver
    {
        private const TargetIndex ProcessorInd = TargetIndex.A;
        private const TargetIndex ItemInd = TargetIndex.B;

        protected Building_WorkTable Processor => (Building_WorkTable)job.GetTarget(TargetIndex.A).Thing;

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


            yield return Toils_General.Wait(90).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
                .WithProgressBarToilDelay(TargetIndex.A);

            // Empty the processor (this spawns items at interaction cell)
            Toil emptyToil = ToilMaker.MakeToil("MakeNewToils");
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
            Toil findItemsToil = ToilMaker.MakeToil("MakeNewToils");
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
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };
            findItemsToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return findItemsToil;

            // Reserve the spawned items
            yield return Toils_Reserve.Reserve(TargetIndex.B);

            // Go pick up the items
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);

            // Haul to storage
            yield return Toils_Haul.CarryHauledThingToContainer();
        }
    }
}
