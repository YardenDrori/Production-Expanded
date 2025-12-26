using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProcessorFramework;

[HotSwappable]
public class JobDriver_FillProcessor : JobDriver
{
	private const TargetIndex ProcessorInd = (TargetIndex)1;

	private const TargetIndex IngredientInd = (TargetIndex)2;

	private const int Duration = 200;

	private CompProcessor comp;

	protected Thing Processor
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			LocalTargetInfo target = base.job.GetTarget((TargetIndex)1);
			return ((LocalTargetInfo)(ref target)).Thing;
		}
	}

	protected Thing Ingredient
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			LocalTargetInfo target = base.job.GetTarget((TargetIndex)2);
			return ((LocalTargetInfo)(ref target)).Thing;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (ReservationUtility.Reserve(base.pawn, LocalTargetInfo.op_Implicit(Processor), base.job, 1, -1, (ReservationLayerDef)null, true, false))
		{
			return ReservationUtility.Reserve(base.pawn, LocalTargetInfo.op_Implicit(Ingredient), base.job, 1, base.job.count, (ReservationLayerDef)null, errorOnFailed, false);
		}
		return false;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		comp = ThingCompUtility.TryGetComp<CompProcessor>(Processor);
		ProcessDef processDef = comp.enabledProcesses.FirstOrDefault((KeyValuePair<ProcessDef, ProcessFilter> y) => y.Value.allowedIngredients.Contains(Ingredient.def)).Key;
		if (processDef == null)
		{
			Log.Error("Processor Framework: Unable to find enabled process that allows " + ((Entity)Ingredient).Label + " for " + (object)Processor);
		}
		ToilFailConditions.FailOnDespawnedNullOrForbidden<JobDriver_FillProcessor>(this, (TargetIndex)1);
		ToilFailConditions.FailOnBurningImmobile<JobDriver_FillProcessor>(this, (TargetIndex)1);
		((JobDriver)this).AddEndCondition((Func<JobCondition>)(() => (comp.SpaceLeftFor(processDef) >= 1 && comp.enabledProcesses.TryGetValue(processDef, out var value) && value.allowedIngredients.Contains(Ingredient.def)) ? ((JobCondition)1) : ((JobCondition)2)));
		Toil reserveIngredient = Toils_Reserve.Reserve((TargetIndex)2, 1, base.job.count, (ReservationLayerDef)null, false);
		yield return reserveIngredient;
		yield return ToilFailConditions.FailOnSomeonePhysicallyInteracting<Toil>(ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(Toils_Goto.GotoThing((TargetIndex)2, (PathEndMode)3, false), (TargetIndex)2), (TargetIndex)2);
		yield return ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(Toils_Haul.StartCarryThing((TargetIndex)2, false, true, false, true, false), (TargetIndex)2);
		yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIngredient, (TargetIndex)2, (TargetIndex)0, false, (Predicate<Thing>)null);
		yield return Toils_Goto.GotoThing((TargetIndex)1, (PathEndMode)3, false);
		yield return ToilEffects.WithProgressBarToilDelay(ToilFailConditions.FailOnCannotTouch<Toil>(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(Toils_General.Wait(200, (TargetIndex)1), (TargetIndex)2), (TargetIndex)1), (TargetIndex)1, (PathEndMode)2), (TargetIndex)1, false, -0.5f);
		yield return new Toil
		{
			initAction = delegate
			{
				comp.AddIngredient(Ingredient, processDef);
			},
			defaultCompleteMode = (ToilCompleteMode)1
		};
	}
}
