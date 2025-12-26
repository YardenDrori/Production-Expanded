using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProcessorFramework;

public class Building_ColorCoded : Building
{
	public override Color DrawColorTwo
	{
		get
		{
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			CompProcessor compProcessor = ThingCompUtility.TryGetComp<CompProcessor>((Thing)(object)this);
			if (compProcessor != null && !compProcessor.Props.parallelProcesses && compProcessor.Props.colorCoded && !GenList.NullOrEmpty<ActiveProcess>((IList<ActiveProcess>)compProcessor.activeProcesses) && compProcessor.activeProcesses.First().processDef.color != Color.white)
			{
				return compProcessor.activeProcesses.First().processDef.color;
			}
			return ((Thing)this).DrawColor;
		}
	}
}
