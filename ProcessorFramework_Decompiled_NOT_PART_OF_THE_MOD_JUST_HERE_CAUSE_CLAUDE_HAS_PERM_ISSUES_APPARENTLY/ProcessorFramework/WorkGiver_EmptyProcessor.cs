using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProcessorFramework;

[HotSwappable]
public class WorkGiver_EmptyProcessor : WorkGiver_Scanner
{
	public override PathEndMode PathEndMode => (PathEndMode)2;

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		return !GenCollection.Any<ThingWithComps>(((Thing)pawn).Map.GetComponent<MapComponent_Processors>().thingsWithProcessorComp);
	}

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return (IEnumerable<Thing>)((Thing)pawn).Map.GetComponent<MapComponent_Processors>().thingsWithProcessorComp;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		CompProcessor compProcessor = ThingCompUtility.TryGetComp<CompProcessor>(t);
		if (compProcessor != null && (compProcessor.AnyComplete || compProcessor.AnyRuined) && !FireUtility.IsBurning(t) && !ForbidUtility.IsForbidden(t, pawn))
		{
			return ReservationUtility.CanReserveAndReach(pawn, LocalTargetInfo.op_Implicit(t), (PathEndMode)2, DangerUtility.NormalMaxDanger(pawn), 1, -1, DefOf.PF_Empty, forced);
		}
		return false;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		return new Job(DefOf.EmptyProcessor, LocalTargetInfo.op_Implicit(t));
	}
}
