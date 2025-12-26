using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ProcessorFramework;

public class Command_Quality : Command_Action
{
	public QualityCategory qualityToTarget;

	public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
	{
		get
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Expected O, but got Unknown
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Expected O, but got Unknown
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (QualityCategory quality in Enum.GetValues(typeof(QualityCategory)))
			{
				list.Add(new FloatMenuOption(QualityUtility.GetLabel(quality), (Action)delegate
				{
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_000c: Unknown result type (might be due to invalid IL or missing references)
					ChangeQuality(qualityToTarget, quality);
				}, (Texture2D)ProcessorFramework_Utility.qualityMaterials[quality].mainTexture, Color.white, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0, (HorizontalJustification)0, false));
			}
			return list;
		}
	}

	internal static void ChangeQuality(QualityCategory qualityToTarget, QualityCategory quality)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		foreach (Thing item in Find.Selector.SelectedObjects.OfType<Thing>())
		{
			CompProcessor compProcessor = ThingCompUtility.TryGetComp<CompProcessor>(item);
			if (compProcessor == null || !GenCollection.Any<ActiveProcess>(compProcessor.activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess x) => x.processDef.usesQuality)))
			{
				continue;
			}
			foreach (ActiveProcess activeProcess in compProcessor.activeProcesses)
			{
				activeProcess.TargetQuality = quality;
				compProcessor.cachedTargetQualities[activeProcess.processDef] = quality;
			}
		}
	}
}
