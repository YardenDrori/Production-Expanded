using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProcessorFramework;

[HotSwappable]
public class ProcessorFramework_Core : Mod
{
	public static PF_Settings settings;

	public static FieldInfo cachedGraphic = typeof(MinifiedThing).GetField("cachedGraphic", BindingFlags.Instance | BindingFlags.NonPublic);

	public ProcessorFramework_Core(ModContentPack content)
		: base(content)
	{
		settings = ((Mod)this).GetSettings<PF_Settings>();
	}

	public override string SettingsCategory()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return TaggedString.op_Implicit(Translator.Translate("PF_SettingsCategory"));
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_041c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Unknown result type (might be due to invalid IL or missing references)
		Listing_Standard val = new Listing_Standard();
		((Listing)val).Begin(inRect);
		val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("PF_ShowProcessBar")), ref PF_Settings.showProcessBar, TaggedString.op_Implicit(Translator.Translate("PF_ShowProcessBarTooltip")), 0f, 1f);
		val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("PF_ShowProcessIcon")), ref PF_Settings.showProcessIconGlobal, TaggedString.op_Implicit(Translator.Translate("PF_ShowProcessIconTooltip")), 0f, 1f);
		val.Label(Translator.Translate("PF_ProcessIconSize") + ": <color=#FFFF44>" + GenText.ToStringByStyle(PF_Settings.processIconSize, (ToStringStyle)8, (ToStringNumberSense)1) + "</color>", -1f, TaggedString.op_Implicit(Translator.Translate("PF_ProcessIconSizeTooltip")));
		PF_Settings.processIconSize = val.Slider(GenMath.RoundTo(PF_Settings.processIconSize, 0.05f), 0.2f, 1f);
		val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("PF_ProductIcon")), ref PF_Settings.productIcon, TaggedString.op_Implicit(Translator.Translate("PF_ProductIconTooltip")), 0f, 1f);
		val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("PF_IngredientIcon")), ref PF_Settings.ingredientIcon, TaggedString.op_Implicit(Translator.Translate("PF_IngredientIconTooltip")), 0f, 1f);
		val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("PF_SingleItemIcon")), ref PF_Settings.singleItemIcon, TaggedString.op_Implicit(Translator.Translate("PF_SingleItemIconTooltip")), 0f, 1f);
		((Listing)val).GapLine(24f);
		val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("PF_ShowCurrentQualityIcon")), ref PF_Settings.showCurrentQualityIcon, TaggedString.op_Implicit(Translator.Translate("PF_ShowCurrentQualityIconTooltip")), 0f, 1f);
		val.Label(Translator.Translate("PF_defaultQuality") + ": <color=#FFFF44>" + QualityUtility.GetLabel((QualityCategory)checked((byte)PF_Settings.defaultTargetQualityInt)) + "</color>", -1f, TaggedString.op_Implicit(Translator.Translate("PF_defaultQualityTooltip")));
		PF_Settings.defaultTargetQualityInt = Mathf.RoundToInt(val.Slider((float)PF_Settings.defaultTargetQualityInt, 0f, 6f));
		((Listing)val).GapLine(24f);
		val.Label(Translator.Translate("PF_initialProcessState") + ": ", -1f, TaggedString.op_Implicit(Translator.Translate("PF_initialProcessStateTooltip")));
		((Listing)val).Indent(12f);
		((Listing)val).ColumnWidth = ((Listing)val).ColumnWidth - 12f;
		if (val.RadioButton(TaggedString.op_Implicit(Translator.Translate("PF_firstOnly")), PF_Settings.initialProcessState == PF_Settings.InitialProcessState.firstonly, 0f, TaggedString.op_Implicit(Translator.Translate("PF_firstOnlyTooltip")), (float?)null))
		{
			PF_Settings.initialProcessState = PF_Settings.InitialProcessState.firstonly;
		}
		if (val.RadioButton(TaggedString.op_Implicit(Translator.Translate("PF_allEnabled")), PF_Settings.initialProcessState == PF_Settings.InitialProcessState.enabled, 0f, TaggedString.op_Implicit(Translator.Translate("PF_allEnabledTooltip")), (float?)null))
		{
			PF_Settings.initialProcessState = PF_Settings.InitialProcessState.enabled;
		}
		if (val.RadioButton(TaggedString.op_Implicit(Translator.Translate("PF_allDisabled")), PF_Settings.initialProcessState == PF_Settings.InitialProcessState.disabled, 0f, TaggedString.op_Implicit(Translator.Translate("PF_allDisabledTooltip")), (float?)null))
		{
			PF_Settings.initialProcessState = PF_Settings.InitialProcessState.disabled;
		}
		((Listing)val).Indent(-12f);
		((Listing)val).ColumnWidth = ((Listing)val).ColumnWidth + 12f;
		((Listing)val).Gap(12f);
		val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("PF_replaceDestroyedProcessors")), ref PF_Settings.replaceDestroyedProcessors, TaggedString.op_Implicit(Translator.Translate("PF_replaceDestroyedProcessorsTooltip")), 0f, 1f);
		((Listing)val).GapLine(48f);
		Rect rect = ((Listing)val).GetRect(30f, 1f);
		TooltipHandler.TipRegion(rect, TipSignal.op_Implicit(Translator.Translate("PF_ReplaceVanillaBarrelsTooltip")));
		if (Widgets.ButtonText(rect, TaggedString.op_Implicit(Translator.Translate("PF_ReplaceVanillaBarrels")), true, true, true, (TextAnchor?)null))
		{
			SoundStarter.PlayOneShotOnCamera(SoundDefOf.Click, (Map)null);
			ReplaceVanillaBarrels();
		}
		((Listing)val).Gap(12f);
		Rect rect2 = ((Listing)val).GetRect(30f, 1f);
		TooltipHandler.TipRegion(rect2, TipSignal.op_Implicit(Translator.Translate("PF_DefaultSettingsTooltip")));
		if (Widgets.ButtonText(rect2, TaggedString.op_Implicit(Translator.Translate("PF_DefaultSettings")), true, true, true, (TextAnchor?)null))
		{
			PF_Settings.showProcessIconGlobal = true;
			PF_Settings.processIconSize = 0.6f;
			PF_Settings.singleItemIcon = true;
			PF_Settings.showCurrentQualityIcon = true;
		}
		((Listing)val).End();
		((ModSettings)settings).Write();
	}

	public override void WriteSettings()
	{
		((Mod)this).WriteSettings();
		ProcessorFramework_Utility.RecacheAll();
	}

	public void ReplaceVanillaBarrels()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Current.ProgramState != 2)
		{
			return;
		}
		foreach (Map map in Find.Maps)
		{
			foreach (Thing item in map.listerThings.ThingsOfDef(ThingDefOf.FermentingBarrel).ToList())
			{
				bool flag = false;
				float num = 0f;
				int stackCount = 0;
				IntVec3 position = item.Position;
				ThingDef val = ((item.Stuff == null) ? ThingDefOf.WoodLog : item.Stuff);
				Building_FermentingBarrel val2 = (Building_FermentingBarrel)(object)((item is Building_FermentingBarrel) ? item : null);
				if (val2 != null)
				{
					flag = val2.SpaceLeftForWort < 25;
					if (flag)
					{
						num = val2.Progress;
						stackCount = 25 - val2.SpaceLeftForWort;
					}
				}
				Thing val3 = ThingMaker.MakeThing(DefOf.BarrelProcessor, val);
				GenSpawn.Spawn(val3, position, map, (WipeMode)0);
				if (flag)
				{
					CompProcessor compProcessor = ThingCompUtility.TryGetComp<CompProcessor>(val3);
					Thing wort = ThingMaker.MakeThing(ThingDefOf.Wort, (ThingDef)null);
					wort.stackCount = stackCount;
					compProcessor.AddIngredient(wort, DefOf.Beer);
					compProcessor.activeProcesses.Find((ActiveProcess x) => x.processDef.ingredientFilter.Allows(wort)).activeProcessTicks = Mathf.RoundToInt(360000f * num);
				}
			}
			foreach (Thing item2 in from t in map.listerThings.ThingsOfDef(ThingDefOf.MinifiedThing)
				where MinifyUtility.GetInnerIfMinified(t).def == ThingDefOf.FermentingBarrel
				select t)
			{
				MinifiedThing val4 = (MinifiedThing)(object)((item2 is MinifiedThing) ? item2 : null);
				ThingDef val5 = ((val4.InnerThing.Stuff == null) ? ThingDefOf.WoodLog : val4.InnerThing.Stuff);
				val4.InnerThing = null;
				Thing innerThing = ThingMaker.MakeThing(DefOf.BarrelProcessor, val5);
				val4.InnerThing = innerThing;
				cachedGraphic.SetValue(val4, null);
			}
		}
	}
}
