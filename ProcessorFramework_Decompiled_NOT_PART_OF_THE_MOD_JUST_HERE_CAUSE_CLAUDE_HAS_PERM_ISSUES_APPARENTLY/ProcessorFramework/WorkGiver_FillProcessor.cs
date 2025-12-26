using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProcessorFramework;

[HotSwappable]
public class WorkGiver_FillProcessor : WorkGiver_Scanner
{
	public override PathEndMode PathEndMode => (PathEndMode)2;

	public override bool Prioritized => true;

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return (IEnumerable<Thing>)((Thing)pawn).Map.GetComponent<MapComponent_Processors>().thingsWithProcessorComp;
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		return !GenCollection.Any<ThingWithComps>(((Thing)pawn).Map.GetComponent<MapComponent_Processors>().thingsWithProcessorComp);
	}

	public override float GetPriority(Pawn pawn, TargetInfo t)
	{
		CompProcessor compProcessor = ThingCompUtility.TryGetComp<CompProcessor>(((TargetInfo)(ref t)).Thing);
		if (compProcessor != null)
		{
			return 1f / (float)compProcessor.SpaceLeft;
		}
		return 0f;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		CompProcessor compProcessor = ThingCompUtility.TryGetComp<CompProcessor>(t);
		if (compProcessor == null || GenCollection.EnumerableNullOrEmpty<KeyValuePair<ProcessDef, ProcessFilter>>((IEnumerable<KeyValuePair<ProcessDef, ProcessFilter>>)compProcessor.enabledProcesses))
		{
			return false;
		}
		ProcessDef processDef = null;
		if (compProcessor.Props.parallelProcesses || compProcessor.activeProcesses == null || compProcessor.activeProcesses.Count == 0)
		{
			float num = float.MaxValue;
			foreach (ProcessDef key in compProcessor.enabledProcesses.Keys)
			{
				if (key.capacityFactor < num)
				{
					num = key.capacityFactor;
					processDef = key;
				}
			}
		}
		else
		{
			processDef = compProcessor.activeProcesses[0].processDef;
		}
		if (compProcessor.SpaceLeftFor(processDef) < 1)
		{
			return false;
		}
		if (!compProcessor.TemperatureOk)
		{
			TaggedString val = Translator.Translate("BadTemperature");
			JobFailReason.Is(TaggedString.op_Implicit(((TaggedString)(ref val)).ToLower()), (string)null);
			return false;
		}
		if (((Thing)pawn).Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null || ForbidUtility.IsForbidden(t, pawn) || !ReservationUtility.CanReserveAndReach(pawn, LocalTargetInfo.op_Implicit(t), (PathEndMode)2, DangerUtility.NormalMaxDanger(pawn), 10, 0, (ReservationLayerDef)null, forced) || FireUtility.IsBurning(t))
		{
			return false;
		}
		if (FindIngredient(pawn, compProcessor) == null)
		{
			JobFailReason.Is(TaggedString.op_Implicit(Translator.Translate("PF_NoIngredient")), (string)null);
			return false;
		}
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		CompProcessor compProcessor = ThingCompUtility.TryGetComp<CompProcessor>(t);
		Thing val = FindIngredient(pawn, compProcessor);
		ProcessDef processDef = null;
		foreach (KeyValuePair<ProcessDef, ProcessFilter> enabledProcess in compProcessor.enabledProcesses)
		{
			if (enabledProcess.Value.allowedIngredients.Contains(val.def))
			{
				processDef = enabledProcess.Key;
				break;
			}
		}
		int count = 0;
		if (processDef != null)
		{
			int num = compProcessor.SpaceLeftFor(processDef);
			int stackCount = val.stackCount;
			if (processDef.useStatForEfficiency)
			{
				float statValue = StatExtension.GetStatValue(val, processDef.efficiencyStat, false, -1);
				float statBaselineValue = processDef.statBaselineValue;
				float num2 = statValue / statBaselineValue;
				num = Mathf.RoundToInt((float)num / num2);
			}
			int num3 = pawn.carryTracker.AvailableStackSpace(val.def);
			count = Mathf.Min(new int[3] { num, stackCount, num3 });
		}
		return new Job(DefOf.FillProcessor, LocalTargetInfo.op_Implicit(t), LocalTargetInfo.op_Implicit(val))
		{
			count = count
		};
	}

	private Thing FindIngredient(Pawn pawn, CompProcessor comp)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		HashSet<ThingDef> validIngredients = comp.ValidIngredients;
		return GenClosest.ClosestThingReachable(((Thing)pawn).Position, ((Thing)pawn).Map, ThingRequest.ForGroup((ThingRequestGroup)3), (PathEndMode)3, TraverseParms.For(pawn, (Danger)3, (TraverseMode)0, false, false, false, true), 9999f, (Predicate<Thing>)validator, (IEnumerable<Thing>)((Thing)pawn).Map.GetComponent<MapComponent_Processors>().PotentialIngredients, 0, -1, false, (RegionType)14, false, false);
		bool validator(Thing x)
		{
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			if (ForbidUtility.IsForbidden(x, pawn))
			{
				return false;
			}
			if (!validIngredients.Contains(x.def))
			{
				return false;
			}
			ProcessDef processDef = null;
			foreach (KeyValuePair<ProcessDef, ProcessFilter> enabledProcess in comp.enabledProcesses)
			{
				if (enabledProcess.Value.allowedIngredients.Contains(x.def))
				{
					processDef = enabledProcess.Key;
					break;
				}
			}
			if (processDef == null)
			{
				return false;
			}
			if (!ReservationUtility.CanReserve(pawn, LocalTargetInfo.op_Implicit(x), 1, Mathf.Min(new int[3]
			{
				comp.SpaceLeftFor(processDef),
				x.stackCount,
				pawn.carryTracker.AvailableStackSpace(x.def)
			}), (ReservationLayerDef)null, false) || comp.SpaceLeftFor(processDef) < 1)
			{
				return false;
			}
			return true;
		}
	}
}
