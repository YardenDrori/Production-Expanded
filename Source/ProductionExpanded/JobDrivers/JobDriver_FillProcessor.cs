using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class JobDriver_FillProcessor : JobDriver
  {
    private const TargetIndex ProcessorInd = TargetIndex.A;
    private const TargetIndex MaterialsInd = TargetIndex.B;

    protected Building_Processor Processor => (Building_Processor)job.GetTarget(TargetIndex.A).Thing;
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

      // Check if this is a static recipe
      Bill_Production bill = (Bill_Production)job.bill;
      bool isStaticRecipe = false;
      if (bill != null)
      {
        var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
        isStaticRecipe = settings?.isStaticRecipe ?? false;
      }

      // Only check capacity for dynamic recipes
      if (!isStaticRecipe)
      {
        AddEndCondition(() => (processorComp.getCapacityRemaining() > 0) ? JobCondition.Ongoing : JobCondition.Succeeded);
      }

      yield return Toils_General.DoAtomic(delegate
      {
        // Get capacityFactor to calculate actual item count
        Bill_Production billLocal = (Bill_Production)job.bill;
        if (billLocal == null && processorComp.getIsProcessing())
        {
          billLocal = processorComp.GetActiveBill();
        }

        if (billLocal != null)
        {
          var settings = billLocal.recipe.GetModExtension<RecipeExtension_Processor>();

          // Only recalculate count based on capacity for dynamic recipes
          if (settings?.isStaticRecipe != true)
          {
            float capacityFactor = settings?.capacityFactor ?? 1f;
            job.count = Mathf.Max(1, (int)(processorComp.getCapacityRemaining() / capacityFactor));
          }
          // For static recipes, keep the count set by WorkGiver
        }
      });

      Toil reserveMaterials = Toils_Reserve.Reserve(TargetIndex.B);
      yield return reserveMaterials;

      yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
        .FailOnDespawnedNullOrForbidden(TargetIndex.B)
        .FailOnSomeonePhysicallyInteracting(TargetIndex.B);

      yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true)
        .FailOnDestroyedNullOrForbidden(TargetIndex.B);

      yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveMaterials, TargetIndex.B, TargetIndex.None, true);

      yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

      yield return Toils_General.Wait(200)
        .FailOnDestroyedNullOrForbidden(TargetIndex.B)
        .FailOnDestroyedNullOrForbidden(TargetIndex.A)
        .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
        .WithProgressBarToilDelay(TargetIndex.A);

      Toil toil = ToilMaker.MakeToil("FillProcessor");
      toil.initAction = delegate
      {
        Bill_Production bill = (Bill_Production)job.bill;

        // If we are filling an active processor, use the stored active bill
        if (bill == null && processorComp.getIsProcessing())
        {
          bill = processorComp.GetActiveBill();
        }

        if (bill != null)
        {
          // AddMaterials now stores the ingredient in the ThingOwner, so don't destroy it
          processorComp.AddMaterials(bill, Materials, Materials.stackCount);
          // Materials will be transferred to the processor's container automatically
        }
        else
        {
          Log.Warning("[Production Expanded] Pawn arrived at processor but no valid bill found for ingredient. Dropping.");
          GenPlace.TryPlaceThing(Materials, pawn.Position, pawn.Map, ThingPlaceMode.Near);
        }
      };
      toil.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return toil;
    }
  }
}
