using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ProcessorFramework;

public class CompProcessor : ThingComp, IThingHolder
{
	public List<ActiveProcess> activeProcesses = new List<ActiveProcess>();

	public Dictionary<ProcessDef, ProcessFilter> enabledProcesses = new Dictionary<ProcessDef, ProcessFilter>();

	public Dictionary<ProcessDef, QualityCategory> cachedTargetQualities = new Dictionary<ProcessDef, QualityCategory>();

	public bool emptyNow;

	public bool graphicChangeQueued;

	public CompRefuelable refuelComp;

	public CompPowerTrader powerTradeComp;

	public CompFlickable flickComp;

	public ThingOwner innerContainer;

	public CompProperties_Processor Props => (CompProperties_Processor)(object)base.props;

	public bool AnyRuined => GenCollection.Any<ActiveProcess>(activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess x) => x.Ruined));

	public bool Empty => TotalIngredientCount <= 0;

	public bool AnyComplete => GenCollection.Any<ActiveProcess>(activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess x) => x.Complete));

	public int SpaceLeft => Props.capacity - TotalIngredientCount;

	public int TotalIngredientCount
	{
		get
		{
			float num = 0f;
			for (int i = 0; i < activeProcesses.Count; i++)
			{
				num += (float)activeProcesses[i].ingredientCount * activeProcesses[i].processDef.capacityFactor;
			}
			return Mathf.CeilToInt(num);
		}
	}

	public HashSet<ThingDef> ValidIngredients
	{
		get
		{
			HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
			foreach (ProcessFilter value in enabledProcesses.Values)
			{
				GenCollection.AddRange<ThingDef>(hashSet, value.allowedIngredients);
			}
			return hashSet;
		}
	}

	public bool TemperatureOk
	{
		get
		{
			float ambientTemperature = ((Thing)base.parent).AmbientTemperature;
			foreach (ProcessDef key in enabledProcesses.Keys)
			{
				if (ambientTemperature >= key.temperatureSafe.min - 2f || ambientTemperature <= key.temperatureSafe.max + 2f)
				{
					return true;
				}
			}
			return false;
		}
	}

	public float PowerConsumptionRate
	{
		get
		{
			float num = 0f;
			foreach (ActiveProcess activeProcess in activeProcesses)
			{
				num += activeProcess.processDef.powerUseFactor * (float)activeProcess.ingredientCount * activeProcess.processDef.capacityFactor;
			}
			if (num != 0f)
			{
				return num / (float)TotalIngredientCount;
			}
			return 1f;
		}
	}

	public float FuelConsumptionRate
	{
		get
		{
			float num = 0f;
			foreach (ActiveProcess activeProcess in activeProcesses)
			{
				num += activeProcess.processDef.fuelUseFactor * (float)activeProcess.ingredientCount * activeProcess.processDef.capacityFactor;
			}
			if (num != 0f)
			{
				return num / (float)TotalIngredientCount;
			}
			return 1f;
		}
	}

	public unsafe float RoofCoverage
	{
		get
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			if (((Thing)base.parent).Map == null)
			{
				return 0f;
			}
			int num = 0;
			int num2 = 0;
			CellRect val = GenAdj.OccupiedRect((Thing)(object)base.parent);
			Enumerator enumerator = ((CellRect)(ref val)).GetEnumerator();
			try
			{
				while (((Enumerator)(ref enumerator)).MoveNext())
				{
					IntVec3 current = ((Enumerator)(ref enumerator)).Current;
					num++;
					if (((Thing)base.parent).Map.roofGrid.Roofed(current))
					{
						num2++;
					}
				}
			}
			finally
			{
				((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
			}
			return (float)num2 / (float)num;
		}
	}

	public bool Fueled
	{
		get
		{
			if (refuelComp != null)
			{
				return refuelComp.HasFuel;
			}
			return true;
		}
	}

	public bool Powered
	{
		get
		{
			if (powerTradeComp != null)
			{
				return powerTradeComp.PowerOn;
			}
			return true;
		}
	}

	public bool FlickedOn
	{
		get
		{
			if (flickComp != null)
			{
				return flickComp.SwitchIsOn;
			}
			return true;
		}
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>)GetDirectlyHeldThings());
	}

	public override void Initialize(CompProperties props)
	{
		((ThingComp)this).Initialize(props);
		innerContainer = (ThingOwner)(object)new ThingOwner<Thing>((IThingHolder)(object)this);
		ThingDef def = ((Thing)base.parent).def;
		if (def.inspectorTabsResolved == null)
		{
			def.inspectorTabsResolved = new List<InspectTabBase>();
		}
		if (!GenCollection.Any<InspectTabBase>(((Thing)base.parent).def.inspectorTabsResolved, (Predicate<InspectTabBase>)((InspectTabBase t) => t is ITab_ProcessSelection)))
		{
			((Thing)base.parent).def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_ProcessSelection)));
			((Thing)base.parent).def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_ProcessorContents)));
		}
		if (PF_Settings.initialProcessState == PF_Settings.InitialProcessState.firstonly)
		{
			if (Props.processes.Count > 0)
			{
				ToggleProcess(Props.processes[0], on: true);
			}
		}
		else if (PF_Settings.initialProcessState == PF_Settings.InitialProcessState.enabled)
		{
			EnableAllProcesses();
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		((ThingComp)this).PostSpawnSetup(respawningAfterLoad);
		refuelComp = base.parent.GetComp<CompRefuelable>();
		powerTradeComp = base.parent.GetComp<CompPowerTrader>();
		flickComp = base.parent.GetComp<CompFlickable>();
		((Thing)base.parent).Map.GetComponent<MapComponent_Processors>().Register(base.parent);
		if (!Empty)
		{
			graphicChangeQueued = true;
		}
		if (enabledProcesses == null)
		{
			enabledProcesses = new Dictionary<ProcessDef, ProcessFilter>();
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		((ThingComp)this).PostDestroy(mode, previousMap);
		if ((int)mode != 0 && Props.dropIngredients)
		{
			List<Thing> list = new List<Thing>((IEnumerable<Thing>)innerContainer);
			for (int i = 0; i < list.Count; i++)
			{
				GenSpawn.Spawn(list[i], ((Thing)base.parent).Position, previousMap, (WipeMode)0);
			}
		}
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = (DestroyMode)0)
	{
		((ThingComp)this).PostDeSpawn(map, (DestroyMode)0);
		map.GetComponent<MapComponent_Processors>().Deregister(base.parent);
	}

	public override void PostExposeData()
	{
		Scribe_Deep.Look<ThingOwner>(ref innerContainer, "PF_innerContainer", new object[1] { this });
		Scribe_Collections.Look<ActiveProcess>(ref activeProcesses, "PF_activeProcesses", (LookMode)2, new object[1] { this });
		Scribe_Collections.Look<ProcessDef, ProcessFilter>(ref enabledProcesses, "PF_enabledProcesses", (LookMode)4, (LookMode)2);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Prefs.DevMode)
		{
			yield return (Gizmo)(object)ProcessorFramework_Utility.DebugGizmo();
		}
		foreach (Gizmo item in _003C_003En__0())
		{
			yield return item;
		}
		if (!GenCollection.Any<ActiveProcess>(activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess x) => x.processDef.usesQuality)))
		{
			yield break;
		}
		if (emptyNow)
		{
			yield return (Gizmo)(object)ProcessorFramework_Utility.dontEmptyGizmo;
		}
		else
		{
			yield return (Gizmo)(object)ProcessorFramework_Utility.emptyNowGizmo;
		}
		foreach (ActiveProcess activeProcess in activeProcesses)
		{
			if (activeProcess.processDef.usesQuality)
			{
				yield return (Gizmo)(object)ProcessorFramework_Utility.qualityGizmos[activeProcess.TargetQuality];
			}
		}
	}

	public override void PostDraw()
	{
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		((ThingComp)this).PostDraw();
		if (!Empty)
		{
			if (graphicChangeQueued)
			{
				GraphicChange(toEmpty: false);
				graphicChangeQueued = false;
			}
			if (activeProcesses.Count <= 0)
			{
				return;
			}
			ActiveProcess activeProcess = activeProcesses[0];
			bool flag = !Props.parallelProcesses && activeProcess.processDef.usesQuality && PF_Settings.showCurrentQualityIcon;
			Vector3 drawPos = ((Thing)base.parent).DrawPos;
			Vector2 barScale = Props.barScale;
			Vector2 barOffset = Props.barOffset;
			float num = Static_Bar.Size.x * barScale.x;
			float num2 = Static_Bar.Size.y * barScale.y;
			drawPos.x += barOffset.x - (flag ? 0.1f : 0f);
			drawPos.y += 0.02f;
			drawPos.z += barOffset.y;
			if (PF_Settings.showProcessBar)
			{
				Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos, Quaternion.identity, new Vector3(num + 0.1f, 1f, num2 + 0.1f)), Static_Bar.UnfilledMat, 0);
				float num3 = 0f;
				float num4 = drawPos.x - num * 0.5f;
				for (int i = 0; i < activeProcesses.Count; i++)
				{
					ActiveProcess activeProcess2 = activeProcesses[i];
					float num5 = num * ((float)activeProcess2.ingredientCount * activeProcess2.processDef.capacityFactor / (float)Props.capacity);
					float num6 = num4 + num5 * 0.5f + num3;
					num3 += num5;
					Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(new Vector3(num6, drawPos.y + 0.01f, drawPos.z), Quaternion.identity, new Vector3(num5, 1f, num2)), activeProcess2.ProgressColorMaterial, 0);
				}
			}
			if (flag)
			{
				drawPos.y += 0.02f;
				drawPos.x += 0.45f * barScale.x;
				Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos, Quaternion.identity, new Vector3(0.2f * barScale.x, 1f, 0.2f * barScale.y)), ProcessorFramework_Utility.qualityMaterials[activeProcess.CurrentQuality], 0);
			}
		}
		if (Props.showProductIcon && PF_Settings.showProcessIconGlobal && ((Thing)base.parent).Map.designationManager.DesignationOn((Thing)(object)base.parent) == null && !emptyNow && activeProcesses.Count > 0)
		{
			Vector3 drawPos2 = ((Thing)base.parent).DrawPos;
			float num7 = PF_Settings.processIconSize * Props.productIconSize.x;
			float num8 = PF_Settings.processIconSize * Props.productIconSize.y;
			Dictionary<ProcessDef, byte> dictionary = new Dictionary<ProcessDef, byte>();
			for (int j = 0; j < activeProcesses.Count; j++)
			{
				ProcessDef processDef = activeProcesses[j].processDef;
				if (!dictionary.ContainsKey(processDef))
				{
					dictionary[processDef] = 1;
				}
			}
			int count = dictionary.Count;
			drawPos2.y += 0.2f;
			drawPos2.z += 0.05f;
			drawPos2.x -= (float)(count - 1) * num7 * 0.25f;
			foreach (KeyValuePair<ProcessDef, byte> item in dictionary)
			{
				Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos2, Quaternion.identity, new Vector3(num7, 1f, num8)), ProcessorFramework_Utility.processMaterials[item.Key], 0);
				drawPos2.x += num7 * 0.5f;
				drawPos2.y -= 0.01f;
			}
		}
		if (emptyNow)
		{
			Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(((Thing)base.parent).DrawPos + new Vector3(0f, 0.3f, 0f), Quaternion.identity, new Vector3(0.8f, 1f, 0.8f)), MaterialPool.MatFrom(ProcessorFramework_Utility.emptyNowDesignation), 0);
		}
	}

	public override void CompTick()
	{
		if (Gen.IsHashIntervalTick((Thing)(object)base.parent, 60))
		{
			DoTicks(60);
		}
		if (Gen.IsHashIntervalTick((Thing)(object)base.parent, 250))
		{
			DoActiveProcessesRareTicks();
			AdjustPowerConsumption();
		}
	}

	public override void CompTickRare()
	{
		DoTicks(250);
		DoActiveProcessesRareTicks();
		AdjustPowerConsumption();
	}

	public override void CompTickLong()
	{
		DoTicks(2000);
		DoActiveProcessesRareTicks();
	}

	public void EnableAllProcesses()
	{
		enabledProcesses.Clear();
		foreach (ProcessDef process in Props.processes)
		{
			ProcessFilter value = new ProcessFilter(process.ingredientFilter.AllowedThingDefs.ToList());
			enabledProcesses.Add(process, value);
		}
	}

	public void ToggleProcess(ProcessDef processDef, bool on)
	{
		if (on && !enabledProcesses.ContainsKey(processDef))
		{
			ProcessFilter value = new ProcessFilter(processDef.ingredientFilter.AllowedThingDefs.ToList());
			enabledProcesses.Add(processDef, value);
		}
		else if (!on && enabledProcesses.ContainsKey(processDef))
		{
			enabledProcesses.Remove(processDef);
		}
	}

	public void ToggleIngredient(ProcessDef processDef, ThingDef ingredient, bool on)
	{
		if (on)
		{
			if (enabledProcesses.ContainsKey(processDef))
			{
				enabledProcesses[processDef].allowedIngredients.Add(ingredient);
				return;
			}
			enabledProcesses[processDef] = new ProcessFilter(new List<ThingDef> { ingredient });
		}
		else if (enabledProcesses.ContainsKey(processDef) && enabledProcesses[processDef].allowedIngredients.Contains(ingredient))
		{
			if (enabledProcesses[processDef].allowedIngredients.Count == 1)
			{
				enabledProcesses.Remove(processDef);
			}
			else
			{
				enabledProcesses[processDef].allowedIngredients.Remove(ingredient);
			}
		}
	}

	public int SpaceLeftFor(ProcessDef processDef, float efficiency = 1f)
	{
		if (activeProcesses.Count > 0)
		{
			if (!Props.parallelProcesses && processDef != activeProcesses[0].processDef)
			{
				return 0;
			}
			float num = 0f;
			for (int i = 0; i < activeProcesses.Count; i++)
			{
				num += (float)activeProcesses[i].ingredientCount * activeProcesses[i].processDef.capacityFactor;
			}
			return Mathf.FloorToInt(((float)Props.capacity - num) / processDef.capacityFactor);
		}
		return Mathf.FloorToInt((float)Props.capacity / processDef.capacityFactor);
	}

	public void DoTicks(int ticks)
	{
		if (Empty || !FlickedOn)
		{
			return;
		}
		foreach (ActiveProcess activeProcess in activeProcesses)
		{
			activeProcess.DoTicks(ticks);
		}
		ConsumeFuel(ticks);
	}

	public void AdjustPowerConsumption()
	{
		if (powerTradeComp != null)
		{
			powerTradeComp.PowerOutput = (0f - ((CompPower)powerTradeComp).Props.PowerConsumption) * PowerConsumptionRate;
		}
	}

	public void ConsumeFuel(int ticks)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		if (refuelComp != null && ((int)((Thing)base.parent).def.tickerType != 1 || refuelComp.Props.consumeFuelOnlyWhenUsed) && Fueled && FlickedOn && (!refuelComp.Props.consumeFuelOnlyWhenUsed || !Empty) && (!refuelComp.Props.consumeFuelOnlyWhenPowered || Powered))
		{
			refuelComp.ConsumeFuel((float)ticks * FuelConsumptionRate * refuelComp.Props.fuelConsumptionRate / 60000f);
		}
	}

	public void DoActiveProcessesRareTicks()
	{
		foreach (ActiveProcess activeProcess in activeProcesses)
		{
			activeProcess.TickRare();
		}
	}

	public ActiveProcess FindActiveProcess(ProcessDef processDef)
	{
		foreach (ActiveProcess activeProcess in activeProcesses)
		{
			if (activeProcess.processDef == processDef)
			{
				return activeProcess;
			}
		}
		return null;
	}

	public void AddIngredient(Thing ingredient, ProcessDef processDef)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		float num = 1f;
		if (processDef.useStatForEfficiency)
		{
			float statValue = StatExtension.GetStatValue(ingredient, processDef.efficiencyStat, false, -1);
			float statBaselineValue = processDef.statBaselineValue;
			num = statValue / statBaselineValue;
		}
		int num2 = Mathf.Min(ingredient.stackCount, SpaceLeftFor(processDef) / (int)num);
		if (num2 < ingredient.stackCount)
		{
			Thing val = default(Thing);
			GenDrop.TryDropSpawn(ingredient.SplitOff(ingredient.stackCount - num2), ((Thing)base.parent).Position, ((Thing)base.parent).Map, (ThingPlaceMode)1, ref val, (Action<Thing, int>)null, (Predicate<IntVec3>)null, true);
		}
		bool empty = Empty;
		if (num2 <= 0)
		{
			return;
		}
		if (!Props.independentProcesses)
		{
			ActiveProcess activeProcess = FindActiveProcess(processDef);
			if (activeProcess != null)
			{
				TryMergeProcess(ingredient, activeProcess, num);
				goto IL_00b4;
			}
		}
		TryAddNewProcess(ingredient, processDef, num);
		goto IL_00b4;
		IL_00b4:
		if (empty && !Empty)
		{
			GraphicChange(toEmpty: false);
		}
	}

	private void TryAddNewProcess(Thing ingredient, ProcessDef processDef, float efficiency = 1f)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		int ingredientCount = Mathf.RoundToInt((float)ingredient.stackCount * efficiency);
		activeProcesses.Add(new ActiveProcess(this)
		{
			processDef = processDef,
			ingredientCount = ingredientCount,
			ingredientThings = new List<Thing>(1) { ingredient },
			targetQuality = (QualityCategory)((!cachedTargetQualities.TryGetValue(processDef, out var value)) ? ((byte)PF_Settings.defaultTargetQualityInt) : ((int)value))
		});
		innerContainer.TryAddOrTransfer(ingredient, false);
	}

	private void TryMergeProcess(Thing ingredient, ActiveProcess activeProcess, float efficiency)
	{
		activeProcess.MergeProcess(ingredient, efficiency);
	}

	public Thing TakeOutProduct(ActiveProcess activeProcess)
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		Thing val = null;
		if (!activeProcess.Ruined)
		{
			val = ThingMaker.MakeThing(activeProcess.processDef.thingDef, (ThingDef)null);
			val.stackCount = GenMath.RoundRandom((float)activeProcess.ingredientCount * activeProcess.processDef.efficiency);
			CompIngredients val2 = ThingCompUtility.TryGetComp<CompIngredients>(val);
			if (val2 != null)
			{
				List<ThingDef> list = new List<ThingDef>();
				foreach (Thing ingredientThing in activeProcess.ingredientThings)
				{
					List<ThingDef> list2 = ThingCompUtility.TryGetComp<CompIngredients>(ingredientThing)?.ingredients;
					if (!GenList.NullOrEmpty<ThingDef>((IList<ThingDef>)list2))
					{
						list.AddRange(list2);
					}
					else
					{
						val2.RegisterIngredient(ingredientThing.def);
					}
				}
				if (val2 != null && !GenList.NullOrEmpty<ThingDef>((IList<ThingDef>)list))
				{
					val2.ingredients.AddRange(list);
				}
			}
			if (activeProcess.processDef.usesQuality)
			{
				CompQuality val3 = ThingCompUtility.TryGetComp<CompQuality>(val);
				if (val3 != null)
				{
					val3.SetQuality(activeProcess.CurrentQuality, (ArtGenerationContext?)(ArtGenerationContext)1);
				}
			}
			foreach (BonusOutput bonusOutput in activeProcess.processDef.bonusOutputs)
			{
				if (!Rand.Chance(bonusOutput.chance))
				{
					continue;
				}
				int num = GenMath.RoundRandom((float)activeProcess.ingredientCount * activeProcess.processDef.capacityFactor / (float)Props.capacity * (float)bonusOutput.amount);
				if (num <= 0)
				{
					continue;
				}
				if (bonusOutput.thingDef.race != null)
				{
					for (int i = 0; i < num; i++)
					{
						GenSpawn.Spawn((Thing)(object)PawnGenerator.GeneratePawn(new PawnGenerationRequest(bonusOutput.thingDef.race.AnyPawnKind, (Faction)null, (PawnGenerationContext)2, (PlanetTile?)PlanetTile.op_Implicit(-1), false, true, false, false, true, 0f, false, false, true, true, true, false, false, false, false, 0f, 0f, (Pawn)null, 1f, (Predicate<Pawn>)null, (Predicate<Pawn>)null, (IEnumerable<TraitDef>)null, (IEnumerable<TraitDef>)null, (float?)null, (float?)null, (float?)null, (Gender?)null, (string)null, (string)null, (RoyalTitleDef)null, (Ideo)null, false, false, false, false, (List<GeneDef>)null, (List<GeneDef>)null, (XenotypeDef)null, (CustomXenotype)null, (List<XenotypeDef>)null, 0f, (DevelopmentalStage)8, (Func<XenotypeDef, PawnKindDef>)null, (FloatRange?)null, (FloatRange?)null, false, false, false, -1, 0, false)), ((Thing)base.parent).Position, ((Thing)base.parent).Map, (WipeMode)0);
					}
				}
				else
				{
					Thing obj = ThingMaker.MakeThing(bonusOutput.thingDef, (ThingDef)null);
					obj.stackCount = num;
					GenPlace.TryPlaceThing(obj, ((Thing)base.parent).Position, ((Thing)base.parent).Map, (ThingPlaceMode)1, (Action<Thing, int>)null, (Predicate<IntVec3>)null, (Rot4?)null, 1);
				}
			}
		}
		foreach (Thing ingredientThing2 in activeProcess.ingredientThings)
		{
			innerContainer.Remove(ingredientThing2);
			ingredientThing2.Destroy((DestroyMode)0);
		}
		activeProcesses.Remove(activeProcess);
		if (activeProcesses.Count == 0)
		{
			innerContainer.Clear();
		}
		if (Rand.Chance(activeProcess.processDef.destroyChance * (float)activeProcess.ingredientCount * activeProcess.processDef.capacityFactor / (float)Props.capacity))
		{
			if (PF_Settings.replaceDestroyedProcessors)
			{
				GenConstruct.PlaceBlueprintForBuild((BuildableDef)(object)((Thing)base.parent).def, ((Thing)base.parent).Position, ((Thing)base.parent).Map, ((Thing)base.parent).Rotation, Faction.OfPlayer, (ThingDef)null, (Precept_ThingStyle)null, (ThingStyleDef)null, true);
			}
			((Thing)base.parent).Destroy((DestroyMode)0);
			return val;
		}
		if (Empty)
		{
			GraphicChange(toEmpty: true);
		}
		if (!GenCollection.Any<ActiveProcess>(activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess x) => x.processDef.usesQuality)))
		{
			emptyNow = false;
		}
		return val;
	}

	public void GraphicChange(bool toEmpty)
	{
		if (base.parent is Pawn)
		{
			return;
		}
		string text = ((Thing)base.parent).def.graphicData.texPath;
		if (!toEmpty)
		{
			text += GenCollection.MaxByWithFallback<ActiveProcess, int>((IEnumerable<ActiveProcess>)activeProcesses, (Func<ActiveProcess, int>)((ActiveProcess x) => x.ingredientCount), (ActiveProcess)null)?.processDef?.filledGraphicSuffix;
		}
		Static_TexReloader.Reload((Thing)(object)base.parent, text);
	}

	public override string CompInspectStringExtra()
	{
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		if (activeProcesses.Count == 0)
		{
			return Translator.TranslateSimple("PF_NoIngredient");
		}
		StringBuilder stringBuilder = new StringBuilder();
		ProcessDef processDef = (Props.parallelProcesses ? null : activeProcesses[0].processDef);
		if (processDef != null)
		{
			if (activeProcesses.Count == 1 && processDef.usesQuality && activeProcesses[0].ActiveProcessDays >= processDef.qualityDays.awful)
			{
				ActiveProcess activeProcess = activeProcesses[0];
				ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_ContainsProduct", NamedArgument.op_Implicit(TotalIngredientCount), NamedArgument.op_Implicit(Props.capacity), NamedArgumentUtility.Named((object)processDef.thingDef, "PRODUCT"), NamedArgumentUtility.Named((object)QualityUtility.GetLabel(activeProcess.CurrentQuality).ToLower(), "QUALITY")));
			}
			else
			{
				ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_ContainsIngredientsGeneric", NamedArgument.op_Implicit(TotalIngredientCount), NamedArgument.op_Implicit(Props.capacity)));
			}
		}
		else
		{
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_ContainsIngredientsGeneric", NamedArgument.op_Implicit(TotalIngredientCount), NamedArgument.op_Implicit(Props.capacity)));
		}
		stringBuilder.AppendLine();
		if (processDef == null || (Props.independentProcesses && !Props.parallelProcesses))
		{
			int count = activeProcesses.Count;
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_NumProcessing", NamedArgument.op_Implicit(count), (count == 1) ? NamedArgumentUtility.Named((object)Translator.Translate("PF_RunningStacksNoun"), "STACKS") : NamedArgumentUtility.Named((object)Find.ActiveLanguageWorker.Pluralize(TaggedString.op_Implicit(Translator.Translate("PF_RunningStacksNoun")), count), "STACKS")));
			int num = GenCollection.Count<ActiveProcess>(activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess p) => p.SpeedFactor < 0.75f));
			if (num > 0)
			{
				ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_RunningCountSlow", NamedArgument.op_Implicit(num)));
			}
			int num2 = GenCollection.Count<ActiveProcess>(activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess p) => p.Complete));
			if (num2 > 0)
			{
				ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_RunningCountFinished", NamedArgument.op_Implicit(num2)));
			}
			int num3 = GenCollection.Count<ActiveProcess>(activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess p) => p.Ruined));
			if (num3 > 0)
			{
				ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_RunningCountRuined", NamedArgument.op_Implicit(num3)));
			}
		}
		else if (activeProcesses[0].Complete)
		{
			ColoredText.AppendTagged(stringBuilder, Translator.Translate("PF_Finished"));
		}
		else if (activeProcesses[0].Ruined)
		{
			ColoredText.AppendTagged(stringBuilder, Translator.Translate("PF_Ruined"));
		}
		else if (activeProcesses[0].SpeedFactor < 0.75f)
		{
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_RunningSlow", NamedArgumentUtility.Named((object)GenText.ToStringPercent(activeProcesses[0].SpeedFactor), "SPEED"), NamedArgumentUtility.Named((object)GenText.ToStringPercent(activeProcesses[0].ActiveProcessPercent), "COMPLETE")));
		}
		else
		{
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_RunningInfo", NamedArgument.op_Implicit(GenText.ToStringPercent(activeProcesses[0].ActiveProcessPercent))));
		}
		stringBuilder.AppendLine();
		if (GenCollection.Any<ActiveProcess>(activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess p) => p.processDef.usesTemperature)))
		{
			float ambientTemperature = ((Thing)base.parent).AmbientTemperature;
			stringBuilder.AppendFormat("{0}: {1}", Translator.TranslateSimple("Temperature"), GenText.ToStringTemperature(ambientTemperature, "F0"));
			if (processDef != null)
			{
				if (((FloatRange)(ref processDef.temperatureSafe)).Includes(ambientTemperature))
				{
					stringBuilder.AppendFormat(" ({0})", ((FloatRange)(ref processDef.temperatureIdeal)).Includes(ambientTemperature) ? Translator.TranslateSimple("PF_Ideal") : Translator.TranslateSimple("PF_Safe"));
				}
				else if (!Empty)
				{
					bool flag = ambientTemperature < ((FloatRange)(ref processDef.temperatureSafe)).TrueMin;
					stringBuilder.AppendFormat(ColoredText.Colorize(" ({0}{1})", flag ? Color.red : Color.blue), flag ? Translator.TranslateSimple("Freezing") : Translator.TranslateSimple("Overheating"), (activeProcesses.Count == 1 && !Props.independentProcesses) ? (" " + GenText.ToStringPercent(activeProcesses[0].ruinedPercent)) : "");
				}
			}
			else if (activeProcesses.Count > 0)
			{
				bool flag2 = false;
				foreach (ActiveProcess activeProcess2 in activeProcesses)
				{
					if (ambientTemperature > ((FloatRange)(ref activeProcess2.processDef.temperatureSafe)).TrueMax)
					{
						stringBuilder.AppendFormat(" ({0})", Translator.TranslateSimple("Freezing"));
						flag2 = true;
						break;
					}
					if (ambientTemperature < ((FloatRange)(ref activeProcess2.processDef.temperatureSafe)).TrueMin)
					{
						stringBuilder.AppendFormat(" ({0})", Translator.TranslateSimple("Overheating"));
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					foreach (ActiveProcess activeProcess3 in activeProcesses)
					{
						if (((FloatRange)(ref activeProcess3.processDef.temperatureIdeal)).Includes(ambientTemperature))
						{
							stringBuilder.AppendFormat(" ({0})", Translator.TranslateSimple("PF_Safe"));
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					stringBuilder.AppendFormat(" ({0})", Translator.TranslateSimple("PF_Ideal"));
				}
			}
			stringBuilder.AppendLine();
			if (processDef != null && processDef.usesTemperature)
			{
				stringBuilder.AppendFormat("{0}: {1}~{2} ({3}~{4})", Translator.TranslateSimple("PF_IdealSafeProductionTemperature"), GenText.ToStringTemperature(processDef.temperatureIdeal.min, "F0"), GenText.ToStringTemperature(processDef.temperatureIdeal.max, "F0"), GenText.ToStringTemperature(processDef.temperatureSafe.min, "F0"), GenText.ToStringTemperature(processDef.temperatureSafe.max, "F0"));
			}
		}
		return GenText.TrimEndNewlines(stringBuilder.ToString());
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private IEnumerable<Gizmo> _003C_003En__0()
	{
		return ((ThingComp)this).CompGetGizmosExtra();
	}
}
