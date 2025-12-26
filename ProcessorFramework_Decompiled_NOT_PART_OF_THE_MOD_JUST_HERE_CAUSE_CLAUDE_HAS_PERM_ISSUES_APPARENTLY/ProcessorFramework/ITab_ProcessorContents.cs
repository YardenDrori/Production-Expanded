using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ProcessorFramework;

public class ITab_ProcessorContents : ITab
{
	private const float H = 28f;

	private const float Icon = 24f;

	private const float Pad = 4f;

	private const float PaddedIcon = 32f;

	private const float ThingWidth = 230f;

	public static readonly List<Pair<float, Color>> WhiteToYellowToRed = new List<Pair<float, Color>>
	{
		new Pair<float, Color>(0f, Color.red),
		new Pair<float, Color>(0.5f, Color.yellow),
		new Pair<float, Color>(1f, ITab_Pawn_Gear.ThingLabelColor)
	};

	public static readonly List<Pair<float, Color>> GreenToYellowToRed = new List<Pair<float, Color>>
	{
		new Pair<float, Color>(0f, Color.red),
		new Pair<float, Color>(0.5f, Color.yellow),
		new Pair<float, Color>(1f, Color.green)
	};

	private float lastDrawnHeight;

	private Vector2 scrollPosition;

	public ITab_ProcessorContents()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		((InspectTabBase)this).labelKey = "PF_TabContents";
		((InspectTabBase)this).size = new Vector2(600f, 450f);
	}

	protected override void FillTab()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		IEnumerable<CompProcessor> enumerable = Find.Selector.SelectedObjects.Select(delegate(object o)
		{
			object obj = ((o is ThingWithComps) ? o : null);
			return (obj == null) ? null : ThingCompUtility.TryGetComp<CompProcessor>((Thing)obj);
		});
		if (!GenCollection.EnumerableNullOrEmpty<CompProcessor>(enumerable))
		{
			Rect val = GenUI.ContractedBy(new Rect(default(Vector2), ((InspectTabBase)this).size), 10f);
			((Rect)(ref val)).yMin = ((Rect)(ref val)).yMin + 20f;
			Rect val2 = default(Rect);
			((Rect)(ref val2))._002Ector(0f, 0f, ((Rect)(ref val)).width, Mathf.Max(lastDrawnHeight, ((Rect)(ref val)).height));
			Text.Font = (GameFont)1;
			Widgets.BeginScrollView(val, ref scrollPosition, val2, true);
			float curY = 0f;
			DoItemsLists(val2, ref curY, enumerable);
			lastDrawnHeight = curY;
			Widgets.EndScrollView();
		}
	}

	protected void DoItemsLists(Rect inRect, ref float curY, IEnumerable<CompProcessor> processors)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		GUI.BeginGroup(inRect);
		GUI.color = Widgets.SeparatorLabelColor;
		Text.Anchor = (TextAnchor)0;
		Widgets.Label(new Rect(230f, curY + 3f, 230f, 30f), Translator.Translate("PF_Product"));
		Widgets.Label(new Rect(460f, curY + 3f, ((Rect)(ref inRect)).width - 230f - 230f, 30f), Translator.Translate("PF_TargetQuality"));
		Widgets.ListSeparator(ref curY, ((Rect)(ref inRect)).width, TaggedString.op_Implicit(Translator.Translate("PF_FermentingItems")));
		bool flag = false;
		foreach (CompProcessor processor in processors)
		{
			for (int i = 0; i < processor.innerContainer.Count; i++)
			{
				Thing val = processor.innerContainer[i];
				if (val != null)
				{
					flag = true;
					DoThingRow(val.def, val, ((Rect)(ref inRect)).width, ref curY, processor);
				}
			}
		}
		if (!flag)
		{
			Widgets.NoneLabel(ref curY, ((Rect)(ref inRect)).width, (string)null);
		}
		GUI.EndGroup();
	}

	protected void DoThingRow(ThingDef thingDef, Thing thing, float width, ref float y, CompProcessor processor)
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Expected O, but got Unknown
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		//IL_0587: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0612: Unknown result type (might be due to invalid IL or missing references)
		ActiveProcess progress = processor.activeProcesses.Find((ActiveProcess x) => x.ingredientThings.Contains(thing));
		if (progress == null)
		{
			return;
		}
		ProcessDef processDef = progress.processDef;
		Rect val = default(Rect);
		((Rect)(ref val))._002Ector(0f, y, 230f, 28f);
		Rect val2 = default(Rect);
		((Rect)(ref val2))._002Ector(((Rect)(ref val)).xMax, y, 230f, 28f);
		Rect val3 = default(Rect);
		((Rect)(ref val3))._002Ector(((Rect)(ref val2)).xMax, y, 50f, 28f);
		Rect val4 = default(Rect);
		((Rect)(ref val4))._002Ector(width - 70f, y, 75f, 28f);
		if (Mouse.IsOver(new Rect(0f, y, width, 28f)))
		{
			TargetHighlighter.Highlight(GlobalTargetInfo.op_Implicit(thing), true, true, false);
		}
		if (Mouse.IsOver(val4))
		{
			GUI.color = ITab_Pawn_Gear.HighlightColor;
			GUI.DrawTexture(val4, (Texture)(object)TexUI.HighlightTex);
		}
		Texture2D val5 = ((progress.SpeedFactor <= 0f) ? Widgets.CheckboxOffTex : ((progress.SpeedFactor <= 0.75f) ? Widgets.CheckboxPartialTex : Widgets.CheckboxOnTex));
		GUI.color = Color.white;
		GUI.DrawTexture(new Rect(((Rect)(ref val4)).xMax - 32f + 4f, y, 24f, 24f), (Texture)(object)val5);
		GUI.color = ITab_Pawn_Gear.ThingLabelColor;
		Text.Anchor = (TextAnchor)5;
		Widgets.Label(new Rect(((Rect)(ref val4)).x, y, ((Rect)(ref val4)).xMax - ((Rect)(ref val4)).x - 32f, 28f), $"{Mathf.Floor(progress.ActiveProcessPercent * 100f):0}%");
		TooltipHandler.TipRegion(val4, (Func<string>)(() => progress.ProgressTooltip), 23492376);
		if (processDef.usesQuality)
		{
			Widgets.Dropdown<ActiveProcess, QualityCategory>(val3, progress, (Func<ActiveProcess, QualityCategory>)((ActiveProcess p) => (QualityCategory)(((_003F?)p?.TargetQuality) ?? 2)), (Func<ActiveProcess, IEnumerable<DropdownMenuElement<QualityCategory>>>)GetProgressQualityDropdowns, GenText.CapitalizeFirst(QualityUtility.GetLabel(progress.TargetQuality)), (Texture2D)ProcessorFramework_Utility.qualityMaterials[progress.TargetQuality].mainTexture, (string)null, (Texture2D)null, (Action)null, false);
		}
		else
		{
			if (Mouse.IsOver(val3))
			{
				GUI.color = ITab_Pawn_Gear.HighlightColor;
				GUI.DrawTexture(val3, (Texture)(object)TexUI.HighlightTex);
			}
			GUI.color = ITab_Pawn_Gear.ThingLabelColor;
			Text.Anchor = (TextAnchor)4;
			Widgets.Label(val3, Translator.Translate("PF_NA"));
		}
		TooltipHandler.TipRegion(val3, (Func<string>)(() => progress.QualityTooltip), "PF_QualityTooltip1".GetHashCode());
		GUI.color = Color.white;
		Widgets.InfoCardButton(((Rect)(ref val)).xMax - 4f - 24f, y, thing);
		Widgets.InfoCardButton(((Rect)(ref val2)).xMax - 4f - 24f, y, (Def)(object)progress.processDef.thingDef);
		Rect val6 = default(Rect);
		((Rect)(ref val6))._002Ector(((Rect)(ref val)).x, y, ((Rect)(ref val)).width - 32f, 28f);
		if (Mouse.IsOver(val6))
		{
			GUI.color = ITab_Pawn_Gear.HighlightColor;
			GUI.DrawTexture(val6, (Texture)(object)TexUI.HighlightTex);
		}
		Rect val7 = default(Rect);
		((Rect)(ref val7))._002Ector(((Rect)(ref val2)).x, y, ((Rect)(ref val2)).width - 32f, 28f);
		if (Mouse.IsOver(val7))
		{
			GUI.color = ITab_Pawn_Gear.HighlightColor;
			GUI.DrawTexture(val7, (Texture)(object)TexUI.HighlightTex);
		}
		GUI.color = Color.white;
		Material drawMatSingle = ((BuildableDef)thingDef).DrawMatSingle;
		if ((Object)(object)((drawMatSingle != null) ? drawMatSingle.mainTexture : null) != (Object)null)
		{
			Widgets.ThingIcon(new Rect(((Rect)(ref val)).x, y, 32f, 28f), thing, 1f, (Rot4?)null, false, 1f, false);
		}
		ThingDef thingDef2 = processDef.thingDef;
		object obj;
		if (thingDef2 == null)
		{
			obj = null;
		}
		else
		{
			Material drawMatSingle2 = ((BuildableDef)thingDef2).DrawMatSingle;
			obj = ((drawMatSingle2 != null) ? drawMatSingle2.mainTexture : null);
		}
		if ((Object)obj != (Object)null)
		{
			Widgets.ThingIcon(new Rect(((Rect)(ref val2)).x, y, 32f, 28f), processDef.thingDef, (ThingDef)null, (ThingStyleDef)null, 1f, (Color?)null, (int?)null, 1f);
		}
		Text.Anchor = (TextAnchor)3;
		GUI.color = GenUI.LerpColor(WhiteToYellowToRed, 1f - progress.ruinedPercent);
		int num = IngredientCount(thing, processor, progress);
		int num2 = num;
		if (processDef.useStatForEfficiency)
		{
			float statValue = StatExtension.GetStatValue(thing, processDef.efficiencyStat, false, -1);
			float statBaselineValue = processDef.statBaselineValue;
			float num3 = statValue / statBaselineValue;
			num2 = Mathf.RoundToInt((float)num * num3);
		}
		Tuple.Create(GenText.CapitalizeFirst(GenLabel.ThingLabel((BuildableDef)(object)thing.def, (ThingDef)null, num)), GenText.CapitalizeFirst(GenLabel.ThingLabel((BuildableDef)(object)progress.processDef.thingDef, (ThingDef)null, Mathf.RoundToInt((float)num2 * processDef.efficiency)))).Deconstruct(out var item, out var item2);
		string ingredientLabel = item;
		string productLabel = item2;
		Text.WordWrap = false;
		Widgets.Label(new Rect(((Rect)(ref val)).x + 32f, y, 168f, 28f), GenText.Truncate(ingredientLabel, ((Rect)(ref val)).width, (Dictionary<string, string>)null));
		Widgets.Label(new Rect(((Rect)(ref val2)).x + 32f, y, 168f, 28f), GenText.Truncate(productLabel, ((Rect)(ref val2)).width, (Dictionary<string, string>)null));
		Text.WordWrap = true;
		Text.Anchor = (TextAnchor)0;
		GUI.color = Color.white;
		TooltipHandler.TipRegion(new Rect(((Rect)(ref val2)).x, y, ((Rect)(ref val2)).width - 32f, 28f), (Func<string>)(() => progress.ProcessTooltip(ingredientLabel, productLabel)), "PF_CreatingTooltip1".GetHashCode());
		TooltipHandler.TipRegion(new Rect(((Rect)(ref val)).x, y, ((Rect)(ref val2)).width - 32f, 28f), (Func<string>)(() => progress.ProcessTooltip(ingredientLabel, productLabel)), "PF_CreatingTooltip2".GetHashCode());
		y += 28f;
	}

	private IEnumerable<DropdownMenuElement<QualityCategory>> GetProgressQualityDropdowns(ActiveProcess activeProcess)
	{
		if (activeProcess == null)
		{
			yield break;
		}
		foreach (QualityCategory quality in QualityUtility.AllQualityCategories)
		{
			Material val = ProcessorFramework_Utility.qualityMaterials[quality];
			yield return new DropdownMenuElement<QualityCategory>
			{
				option = new FloatMenuOption(GenText.CapitalizeFirst(QualityUtility.GetLabel(quality)), (Action)delegate
				{
					//IL_000c: Unknown result type (might be due to invalid IL or missing references)
					activeProcess.TargetQuality = quality;
				}, (Texture2D)val.mainTexture, val.color, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0, (HorizontalJustification)0, false),
				payload = quality
			};
		}
	}

	private int IngredientCount(Thing thing, CompProcessor processor, ActiveProcess activeProcess)
	{
		int num = 0;
		if (processor.Props.independentProcesses)
		{
			return activeProcess.ingredientCount;
		}
		foreach (Thing item in (IEnumerable<Thing>)processor.innerContainer)
		{
			if (item != null && item.def == thing.def)
			{
				num += item.stackCount;
			}
		}
		return num;
	}
}
