
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{

    public class WorkGiver_StartNextProcessorCycle : WorkGiver_Scanner
    {
        // private static string NoMaterialsTrans;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public static void ResetStaticData()
        {
            // NoMaterialsTrans = "NoMaterials".Translate();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //checks if it has our comp
            if (!t.HasComp<CompResourceProcessor>())
            {
                return false;
            }
            CompResourceProcessor comp = t.TryGetComp<CompResourceProcessor>();
            //checks if building waiting for items to be extracted
            if (!comp.getIsWaitingForNextCycle())
            {
                JobFailReason.IsSilent();
                return false;
            }
            //if building is finished dont interact with it
            if (comp.getIsFinished())
            {
                JobFailReason.IsSilent();
                return false;
            }
            //checks if building is a worktable
            Building_WorkTable workTable = t as Building_WorkTable;
            if (workTable == null)
            {
                JobFailReason.IsSilent();
                Log.Warning($"[Production Expanded] Building {t.def.defName} Has the comp \"CompResourceProcessor\" but isnt't a worktable.");
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
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_WorkTable workTable = t as Building_WorkTable;
            if (workTable == null)
            {
                Log.Warning($"[Production Expanded] Building {t.def.defName} Has the comp \"CompResourceProcessor\" but isnt't a worktable.");
                return null;
            }
            // return JobMaker.MakeJob(JobDef)
            for (int i = 0; i < workTable.billStack.Count; i++)
            {
                ProcessorRecipeDef curr_bill_recipe = workTable.billStack.Bills[i].recipe as ProcessorRecipeDef;
                if (curr_bill_recipe == null)
                {
                    Log.Warning($"[Production Expanded] Bill of id {i} in building {t.def.defName} has a recipe of a different type other than ProcessorRecipeDef ({workTable.billStack.Bills[i].recipe.GetType()})"); //fix this please im fine with you editing the code here yourself
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
    }
}
