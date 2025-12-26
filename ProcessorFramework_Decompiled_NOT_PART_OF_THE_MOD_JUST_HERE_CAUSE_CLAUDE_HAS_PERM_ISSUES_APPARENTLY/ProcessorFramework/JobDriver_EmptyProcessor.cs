using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ProcessorFramework;

[HotSwappable]
public class JobDriver_EmptyProcessor : JobDriver
{
	private const TargetIndex ProcessorInd = (TargetIndex)1;

	private const TargetIndex ProductToHaulInd = (TargetIndex)2;

	private const TargetIndex StorageCellInd = (TargetIndex)3;

	private const int Duration = 200;

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

	protected Thing Product
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
		return ReservationUtility.Reserve(base.pawn, LocalTargetInfo.op_Implicit(Processor), base.job, 1, -1, DefOf.PF_Empty, errorOnFailed, false);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		CompProcessor comp = ThingCompUtility.TryGetComp<CompProcessor>(Processor);
		ToilFailConditions.FailOn<JobDriver_EmptyProcessor>(this, (Func<bool>)(() => (!comp.AnyComplete && !comp.AnyRuined) || comp.Empty));
		ToilFailConditions.FailOnDestroyedNullOrForbidden<JobDriver_EmptyProcessor>(this, (TargetIndex)1);
		((JobDriver)this).AddEndCondition((Func<JobCondition>)(() => (!comp.Empty) ? ((JobCondition)1) : ((JobCondition)2)));
		yield return Toils_Goto.GotoThing((TargetIndex)1, (PathEndMode)3, false);
		yield return ToilEffects.WithProgressBarToilDelay(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(Toils_General.Wait(200, (TargetIndex)0), (TargetIndex)1), (TargetIndex)1, false, -0.5f);
		yield return new Toil
		{
			initAction = delegate
			{
				//IL_016c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0192: Unknown result type (might be due to invalid IL or missing references)
				//IL_0197: Unknown result type (might be due to invalid IL or missing references)
				//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
				//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
				//IL_0205: Unknown result type (might be due to invalid IL or missing references)
				//IL_0207: Unknown result type (might be due to invalid IL or missing references)
				//IL_0093: Unknown result type (might be due to invalid IL or missing references)
				//IL_0117: Unknown result type (might be due to invalid IL or missing references)
				//IL_012c: Unknown result type (might be due to invalid IL or missing references)
				ActiveProcess activeProcess = GenCollection.FirstOrDefault<ActiveProcess>(comp.activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess x) => x.Complete || x.Ruined));
				if (activeProcess == null)
				{
					((JobDriver)this).EndJobWith((JobCondition)4);
				}
				else
				{
					Thing val = comp.TakeOutProduct(activeProcess);
					if (val == null || val.stackCount == 0)
					{
						((JobDriver)this).EndJobWith((JobCondition)2);
					}
					else if (val.def.race != null)
					{
						for (int num = 0; num < val.stackCount; num++)
						{
							GenSpawn.Spawn((Thing)(object)PawnGenerator.GeneratePawn(new PawnGenerationRequest(val.def.race.AnyPawnKind, Faction.OfPlayerSilentFail, (PawnGenerationContext)2, (PlanetTile?)PlanetTile.op_Implicit(-1), false, true, false, false, true, 0f, false, false, true, true, true, false, false, false, false, 0f, 0f, (Pawn)null, 1f, (Predicate<Pawn>)null, (Predicate<Pawn>)null, (IEnumerable<TraitDef>)null, (IEnumerable<TraitDef>)null, (float?)null, (float?)null, (float?)null, (Gender?)null, (string)null, (string)null, (RoyalTitleDef)null, (Ideo)null, false, false, false, false, (List<GeneDef>)null, (List<GeneDef>)null, (XenotypeDef)null, (CustomXenotype)null, (List<XenotypeDef>)null, 0f, (DevelopmentalStage)8, (Func<XenotypeDef, PawnKindDef>)null, (FloatRange?)null, (FloatRange?)null, false, false, false, -1, 0, false)), ((Thing)base.pawn).Position, ((JobDriver)this).Map, (WipeMode)0);
						}
						((JobDriver)this).EndJobWith((JobCondition)2);
					}
					else
					{
						GenPlace.TryPlaceThing(val, ((Thing)base.pawn).Position, ((JobDriver)this).Map, (ThingPlaceMode)1, (Action<Thing, int>)null, (Predicate<IntVec3>)null, (Rot4?)null, 1);
						StoragePriority val2 = StoreUtility.CurrentStoragePriorityOf(val, false);
						IntVec3 val3 = default(IntVec3);
						if (StoreUtility.TryFindBestBetterStoreCellFor(val, base.pawn, ((JobDriver)this).Map, val2, ((Thing)base.pawn).Faction, ref val3, true))
						{
							base.job.SetTarget((TargetIndex)2, LocalTargetInfo.op_Implicit(val));
							base.job.count = val.stackCount;
							base.job.SetTarget((TargetIndex)3, LocalTargetInfo.op_Implicit(val3));
						}
						else
						{
							((JobDriver)this).EndJobWith((JobCondition)2);
						}
					}
				}
			},
			defaultCompleteMode = (ToilCompleteMode)1
		};
		yield return Toils_Reserve.Reserve((TargetIndex)2, 1, -1, (ReservationLayerDef)null, false);
		yield return Toils_Reserve.Reserve((TargetIndex)3, 1, -1, (ReservationLayerDef)null, false);
		yield return Toils_Goto.GotoThing((TargetIndex)2, (PathEndMode)3, false);
		yield return Toils_Haul.StartCarryThing((TargetIndex)2, false, false, false, true, false);
		Toil carry = Toils_Haul.CarryHauledThingToCell((TargetIndex)3, (PathEndMode)3);
		yield return carry;
		yield return Toils_Haul.PlaceHauledThingInCell((TargetIndex)3, carry, true, false);
	}
}
