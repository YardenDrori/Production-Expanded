using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProcessorFramework;

[StaticConstructorOnStartup]
public static class ProcessorFramework_Utility
{
	public static List<ProcessDef> allProcessDefs;

	public static Dictionary<ProcessDef, Texture2D> productIcons;

	public static Dictionary<ThingDef, Texture2D> ingredientIcons;

	public static Dictionary<QualityCategory, Command_Action> qualityGizmos;

	public static Dictionary<ProcessDef, Material> processMaterials;

	public static Dictionary<QualityCategory, Material> qualityMaterials;

	public static Command_Action emptyNowGizmo;

	public static Texture2D emptyNowIcon;

	public static Texture2D emptyNowDesignation;

	public static Command_Action dontEmptyGizmo;

	public static Texture2D dontEmptyIcon;

	private static int gooseAngle;

	static ProcessorFramework_Utility()
	{
		allProcessDefs = new List<ProcessDef>();
		productIcons = new Dictionary<ProcessDef, Texture2D>();
		ingredientIcons = new Dictionary<ThingDef, Texture2D>();
		qualityGizmos = new Dictionary<QualityCategory, Command_Action>();
		processMaterials = new Dictionary<ProcessDef, Material>();
		qualityMaterials = new Dictionary<QualityCategory, Material>();
		emptyNowIcon = ContentFinder<Texture2D>.Get("UI/EmptyNow", true);
		emptyNowDesignation = ContentFinder<Texture2D>.Get("UI/EmptyNowDesignation", true);
		dontEmptyIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
		gooseAngle = Rand.Range(0, 360);
		CheckForErrors();
		CacheAllProcesses();
		RecacheAll();
	}

	public static void CheckForErrors()
	{
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Invalid comparison between Unknown and I4
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp(typeof(CompProcessor))))
		{
			if (item.comps.Find((CompProperties c) => c.compClass == typeof(CompProcessor)) is CompProperties_Processor compProperties_Processor && GenCollection.Any<ProcessDef>(compProperties_Processor.processes, (Predicate<ProcessDef>)((ProcessDef p) => p.thingDef == null || GenCollection.EnumerableNullOrEmpty<ThingDef>(p.ingredientFilter.AllowedThingDefs))))
			{
				list.Add(((Def)item).modContentPack.Name + ": ThingDef '" + ((Def)item).defName + "' has processes with no product or no ingredient filter. These fields are required.");
				compProperties_Processor.processes.RemoveAll((ProcessDef p) => p.thingDef == null || GenCollection.EnumerableNullOrEmpty<ThingDef>(p.ingredientFilter.AllowedThingDefs));
			}
			if ((int)item.drawerType != 3)
			{
				list.Add(((Def)item).modContentPack.Name + ": ThingDef '" + ((Def)item).defName + "' has DrawerType '" + ((object)Unsafe.As<DrawerType, DrawerType>(ref item.drawerType)/*cast due to .constrained prefix*/).ToString() + "', but MapMeshAndRealTime is required to display product icons and a progress bar.");
			}
			if ((int)item.tickerType == 0)
			{
				list.Add(((Def)item).modContentPack.Name + ": ThingDef '" + ((Def)item).defName + "' has TickerType '" + ((object)Unsafe.As<TickerType, TickerType>(ref item.tickerType)/*cast due to .constrained prefix*/).ToString() + "', but processors need to tick to work.");
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		Log.Warning("<-- Processor Framework Warnings -->");
		foreach (string item2 in list)
		{
			Log.Warning(item2);
		}
	}

	public static void RecacheAll()
	{
		RecacheProcessIcons();
		RecacheProcessMaterials();
		RecacheQualityGizmos();
	}

	private static void CacheAllProcesses()
	{
		List<ProcessDef> list = new List<ProcessDef>();
		foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp(typeof(CompProcessor))))
		{
			if (item.comps.Find((CompProperties c) => c.compClass == typeof(CompProcessor)) is CompProperties_Processor compProperties_Processor)
			{
				list.AddRange(compProperties_Processor.processes);
			}
		}
		for (int num = 0; num < list.Count; num++)
		{
			list[num].uniqueID = num;
			allProcessDefs.Add(list[num]);
		}
	}

	public static void RecacheProcessIcons()
	{
		productIcons.Clear();
		ingredientIcons.Clear();
		foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp(typeof(CompProcessor))))
		{
			if (!(item.comps.Find((CompProperties c) => c.compClass == typeof(CompProcessor)) is CompProperties_Processor compProperties_Processor))
			{
				continue;
			}
			foreach (ProcessDef process in compProperties_Processor.processes)
			{
				if (!productIcons.ContainsKey(process))
				{
					productIcons.Add(process, GetIcon(process.thingDef, PF_Settings.singleItemIcon));
				}
				foreach (ThingDef allowedThingDef in process.ingredientFilter.AllowedThingDefs)
				{
					if (!ingredientIcons.ContainsKey(allowedThingDef))
					{
						ingredientIcons.Add(allowedThingDef, GetIcon(allowedThingDef, PF_Settings.singleItemIcon));
					}
				}
			}
		}
	}

	public static void RecacheProcessMaterials()
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		processMaterials.Clear();
		foreach (ProcessDef allProcessDef in allProcessDefs)
		{
			Material value = MaterialPool.MatFrom(GetIcon(allProcessDef.thingDef, PF_Settings.singleItemIcon));
			if (!processMaterials.ContainsKey(allProcessDef))
			{
				processMaterials.Add(allProcessDef, value);
			}
		}
		qualityMaterials.Clear();
		foreach (QualityCategory value3 in Enum.GetValues(typeof(QualityCategory)))
		{
			Material value2 = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get("UI/QualityIcons/" + ((object)value3/*cast due to .constrained prefix*/).ToString(), true));
			qualityMaterials.Add(value3, value2);
		}
	}

	public static void RecacheQualityGizmos()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		qualityGizmos.Clear();
		foreach (QualityCategory value in Enum.GetValues(typeof(QualityCategory)))
		{
			Command_Quality command_Quality = new Command_Quality
			{
				defaultLabel = GenText.CapitalizeFirst(QualityUtility.GetLabel(value)),
				defaultDesc = TaggedString.op_Implicit(Translator.Translate("PF_SetQualityDesc")),
				icon = (Texture)(Texture2D)qualityMaterials[value].mainTexture,
				qualityToTarget = value
			};
			((Command_Action)command_Quality).action = delegate
			{
				//IL_0010: Unknown result type (might be due to invalid IL or missing references)
				//IL_0015: Unknown result type (might be due to invalid IL or missing references)
				//IL_001d: Expected O, but got Unknown
				FloatMenu val2 = new FloatMenu(((Gizmo)command_Quality).RightClickFloatMenuOptions.ToList())
				{
					vanishIfMouseDistant = true
				};
				Find.WindowStack.Add((Window)(object)val2);
			};
			qualityGizmos.Add(value, (Command_Action)(object)command_Quality);
		}
		emptyNowGizmo = CacheEmptyNowGizmo(empty: true);
		dontEmptyGizmo = CacheEmptyNowGizmo(empty: false);
	}

	public static Command_Action CacheEmptyNowGizmo(bool empty)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		if (!empty)
		{
			return new Command_Action
			{
				defaultLabel = TaggedString.op_Implicit(Translator.Translate("PF_dontEmpty")),
				defaultDesc = TaggedString.op_Implicit(Translator.Translate("PF_dontEmptyDescription")),
				icon = (Texture)(object)dontEmptyIcon,
				action = delegate
				{
					SetEmptyNow(empty: false);
				},
				activateSound = SoundDefOf.TabClose
			};
		}
		return new Command_Action
		{
			defaultLabel = TaggedString.op_Implicit(Translator.Translate("PF_emptyNow")),
			defaultDesc = TaggedString.op_Implicit(Translator.Translate("PF_emptyNowDescription")),
			icon = (Texture)(object)emptyNowIcon,
			action = delegate
			{
				SetEmptyNow(empty: true);
			},
			activateSound = SoundDefOf.TabOpen
		};
	}

	internal static void SetEmptyNow(bool empty)
	{
		foreach (Thing item in Find.Selector.SelectedObjects.OfType<Thing>())
		{
			CompProcessor compProcessor = ThingCompUtility.TryGetComp<CompProcessor>(item);
			if (compProcessor != null && GenCollection.Any<ActiveProcess>(compProcessor.activeProcesses, (Predicate<ActiveProcess>)((ActiveProcess x) => x.processDef.usesQuality)))
			{
				compProcessor.emptyNow = empty;
			}
		}
	}

	public static Command_Action DebugGizmo()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		return new Command_Action
		{
			defaultLabel = "Debug: Options",
			defaultDesc = "Opens a float menu with debug options.",
			icon = (Texture)(object)ContentFinder<Texture2D>.Get("UI/DebugGoose", true),
			iconAngle = gooseAngle,
			iconDrawScale = 1.25f,
			action = delegate
			{
				//IL_0005: Unknown result type (might be due to invalid IL or missing references)
				//IL_000a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0012: Expected O, but got Unknown
				FloatMenu val = new FloatMenu(DebugOptions())
				{
					vanishIfMouseDistant = true
				};
				Find.WindowStack.Add((Window)(object)val);
			}
		};
	}

	public static List<FloatMenuOption> DebugOptions()
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Expected O, but got Unknown
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Expected O, but got Unknown
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Expected O, but got Unknown
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		IEnumerable<ThingWithComps> source = from t in Find.Selector.SelectedObjects.OfType<ThingWithComps>()
			where t.GetComp<CompProcessor>() != null
			select t;
		IEnumerable<CompProcessor> comps = source.Select((ThingWithComps t) => ThingCompUtility.TryGetComp<CompProcessor>((Thing)(object)t));
		if (comps.Any((CompProcessor c) => !c.Empty))
		{
			list.Add(new FloatMenuOption("Finish process", (Action)delegate
			{
				FinishProcess(comps);
			}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
			list.Add(new FloatMenuOption("Progress one day", (Action)delegate
			{
				ProgressOneDay(comps);
			}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
			list.Add(new FloatMenuOption("Progress half quadrum", (Action)delegate
			{
				ProgressHalfQuadrum(comps);
			}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
		}
		if (comps.Any((CompProcessor c) => c.AnyComplete))
		{
			list.Add(new FloatMenuOption("Empty object", (Action)delegate
			{
				EmptyObject(comps);
			}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
		}
		if (comps.Any((CompProcessor c) => c.Empty))
		{
			list.Add(new FloatMenuOption("Fill object", (Action)delegate
			{
				FillObject(comps);
			}, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
		}
		return list;
	}

	internal static void FinishProcess(IEnumerable<CompProcessor> comps)
	{
		foreach (CompProcessor comp in comps)
		{
			foreach (ActiveProcess activeProcess in comp.activeProcesses)
			{
				if (activeProcess.processDef.usesQuality)
				{
					activeProcess.activeProcessTicks = Mathf.RoundToInt(activeProcess.DaysToReachTargetQuality * 60000f);
				}
				else
				{
					activeProcess.activeProcessTicks = Mathf.RoundToInt(activeProcess.processDef.processDays * 60000f);
				}
			}
		}
		gooseAngle = Rand.Range(0, 360);
		SoundStarter.PlayOneShotOnCamera(DefOf.PF_Honk, (Map)null);
	}

	internal static void ProgressOneDay(IEnumerable<CompProcessor> comps)
	{
		foreach (CompProcessor comp in comps)
		{
			foreach (ActiveProcess activeProcess in comp.activeProcesses)
			{
				activeProcess.activeProcessTicks += 60000L;
			}
		}
		gooseAngle = Rand.Range(0, 360);
		SoundStarter.PlayOneShotOnCamera(DefOf.PF_Honk, (Map)null);
	}

	internal static void ProgressHalfQuadrum(IEnumerable<CompProcessor> comps)
	{
		foreach (CompProcessor comp in comps)
		{
			foreach (ActiveProcess activeProcess in comp.activeProcesses)
			{
				activeProcess.activeProcessTicks += 450000L;
			}
		}
		gooseAngle = Rand.Range(0, 360);
		SoundStarter.PlayOneShotOnCamera(DefOf.PF_Honk, (Map)null);
	}

	internal static void EmptyObject(IEnumerable<CompProcessor> comps)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		foreach (CompProcessor comp in comps)
		{
			List<ActiveProcess> list = new List<ActiveProcess>();
			foreach (ActiveProcess activeProcess in comp.activeProcesses)
			{
				if (activeProcess.Complete)
				{
					list.Add(activeProcess);
				}
			}
			foreach (ActiveProcess item in list)
			{
				GenPlace.TryPlaceThing(comp.TakeOutProduct(item), ((Thing)((ThingComp)comp).parent).Position, ((Thing)((ThingComp)comp).parent).Map, (ThingPlaceMode)1, (Action<Thing, int>)null, (Predicate<IntVec3>)null, (Rot4?)null, 1);
			}
		}
		gooseAngle = Rand.Range(0, 360);
		SoundStarter.PlayOneShotOnCamera(DefOf.PF_Honk, (Map)null);
	}

	internal static void FillObject(IEnumerable<CompProcessor> comps)
	{
		foreach (CompProcessor comp in comps)
		{
			if (comp.Empty && !GenCollection.EnumerableNullOrEmpty<KeyValuePair<ProcessDef, ProcessFilter>>((IEnumerable<KeyValuePair<ProcessDef, ProcessFilter>>)comp.enabledProcesses))
			{
				ProcessDef processDef = comp.enabledProcesses.Keys.First();
				Thing val = ThingMaker.MakeThing(processDef.ingredientFilter.AnyAllowedDef, (ThingDef)null);
				val.stackCount = comp.SpaceLeftFor(processDef);
				comp.AddIngredient(val, processDef);
			}
		}
		gooseAngle = Rand.Range(0, 360);
		SoundStarter.PlayOneShotOnCamera(DefOf.PF_Honk, (Map)null);
	}

	internal static void FixNREingredientThings(IEnumerable<CompProcessor> comps)
	{
		foreach (CompProcessor comp in comps)
		{
			foreach (ActiveProcess activeProcess in comp.activeProcesses)
			{
				activeProcess.ingredientThings.RemoveAll((Thing x) => x == null);
			}
		}
	}

	public static string IngredientFilterSummary(ThingFilter thingFilter)
	{
		return thingFilter.Summary;
	}

	public static string ToStringPercentColored(this float val, List<Pair<float, Color>> colors = null)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (colors == null)
		{
			colors = ITab_ProcessorContents.GreenToYellowToRed;
		}
		return ColoredText.Colorize(GenText.ToStringPercent(val), GenUI.LerpColor(colors, val));
	}

	public static Texture2D GetIcon(ThingDef thingDef, bool singleStack = true)
	{
		Texture2D val = null;
		if (thingDef == null || thingDef.graphicData?.texPath == null)
		{
			val = ContentFinder<Texture2D>.GetAllInFolder(thingDef.race.AnyPawnKind.lifeStages.First().bodyGraphicData.texPath).FirstOrDefault();
		}
		if ((Object)(object)val == (Object)null)
		{
			val = ContentFinder<Texture2D>.Get(thingDef.graphicData.texPath, false);
		}
		if ((Object)(object)val == (Object)null)
		{
			val = (singleStack ? ContentFinder<Texture2D>.GetAllInFolder(thingDef.graphicData.texPath).FirstOrDefault() : ContentFinder<Texture2D>.GetAllInFolder(thingDef.graphicData.texPath).LastOrDefault());
			if ((Object)(object)val == (Object)null)
			{
				val = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true);
				Log.Warning("Universal Fermenter:: No texture at " + thingDef.graphicData.texPath + ".");
			}
		}
		return val;
	}
}
