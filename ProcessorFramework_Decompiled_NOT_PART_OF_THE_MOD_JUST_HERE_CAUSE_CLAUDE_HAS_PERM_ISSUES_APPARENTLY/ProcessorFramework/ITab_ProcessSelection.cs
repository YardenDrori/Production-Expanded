using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProcessorFramework;

[HotSwappable]
public class ITab_ProcessSelection : ITab
{
	private Vector2 scrollPosition;

	private IEnumerable<CompProcessor> processorComps;

	private Dictionary<ProcessDef, bool> categoryOpen = new Dictionary<ProcessDef, bool>();

	private const int lineHeight = 22;

	public override bool IsVisible => Find.Selector.SelectedObjects.All(delegate(object x)
	{
		Thing val = (Thing)((x is Thing) ? x : null);
		return val != null && val.Faction == Faction.OfPlayerSilentFail && ThingCompUtility.TryGetComp<CompProcessor>(val) != null;
	});

	public ITab_ProcessSelection()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		((InspectTabBase)this).labelKey = "PF_ITab_ItemSelection";
		((InspectTabBase)this).size = new Vector2(300f, 400f);
	}

	protected override void FillTab()
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		List<object> selectedObjects = Find.Selector.SelectedObjects;
		processorComps = selectedObjects.Select(delegate(object o)
		{
			object obj = ((o is ThingWithComps) ? o : null);
			return (obj == null) ? null : ThingCompUtility.TryGetComp<CompProcessor>((Thing)obj);
		});
		List<ProcessDef> processes = processorComps.First().Props.processes;
		if (GenCollection.EnumerableNullOrEmpty<CompProcessor>(processorComps))
		{
			return;
		}
		Rect val = GenUI.ContractedBy(new Rect(default(Vector2), ((InspectTabBase)this).size), 12f);
		((Rect)(ref val)).yMin = ((Rect)(ref val)).yMin + 24f;
		int num = processes.Count * 22 + 80;
		foreach (KeyValuePair<ProcessDef, bool> item in categoryOpen)
		{
			num += ((processes.Contains(item.Key) && item.Value) ? (item.Key.ingredientFilter.AllowedDefCount * 22) : 0);
		}
		Rect val2 = default(Rect);
		((Rect)(ref val2))._002Ector(0f, 0f, ((Rect)(ref val)).width - GUI.skin.verticalScrollbar.fixedWidth - 1f, (float)num);
		Widgets.DrawMenuSection(val);
		Rect val3 = default(Rect);
		((Rect)(ref val3))._002Ector(((Rect)(ref val)).x + 1f, ((Rect)(ref val)).y + 1f, (((Rect)(ref val)).width - 2f) / 2f, 24f);
		Text.Font = (GameFont)1;
		if (Widgets.ButtonText(val3, TaggedString.op_Implicit(Translator.Translate("ClearAll")), true, true, true, (TextAnchor?)null))
		{
			foreach (CompProcessor processorComp in processorComps)
			{
				processorComp.enabledProcesses.Clear();
			}
			SoundStarter.PlayOneShotOnCamera(SoundDefOf.Checkbox_TurnedOff, (Map)null);
		}
		if (Widgets.ButtonText(new Rect(((Rect)(ref val3)).xMax + 1f, ((Rect)(ref val3)).y, ((Rect)(ref val)).xMax - 1f - (((Rect)(ref val3)).xMax + 1f), 24f), TaggedString.op_Implicit(Translator.Translate("AllowAll")), true, true, true, (TextAnchor?)null))
		{
			foreach (CompProcessor processorComp2 in processorComps)
			{
				processorComp2.EnableAllProcesses();
			}
			SoundStarter.PlayOneShotOnCamera(SoundDefOf.Checkbox_TurnedOn, (Map)null);
		}
		((Rect)(ref val)).yMin = ((Rect)(ref val)).yMin + (((Rect)(ref val3)).height + 6f);
		Rect listRect = default(Rect);
		((Rect)(ref listRect))._002Ector(0f, 2f, 280f, 9999f);
		Widgets.BeginScrollView(val, ref scrollPosition, val2, true);
		foreach (ProcessDef item2 in processes)
		{
			if (!categoryOpen.ContainsKey(item2))
			{
				categoryOpen.Add(item2, value: false);
			}
			DoItemsList(ref listRect, item2);
		}
		Widgets.EndScrollView();
	}

	public void DoItemsList(ref Rect listRect, ProcessDef processDef)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Invalid comparison between Unknown and I4
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Invalid comparison between Unknown and I4
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0400: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Invalid comparison between Unknown and I4
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Invalid comparison between Unknown and I4
		bool flag = categoryOpen[processDef];
		Rect val = GenUI.TopPartPixels(listRect, 24f);
		Rect val2 = new Rect(((Rect)(ref val)).x, ((Rect)(ref val)).y, 18f, 18f);
		((Rect)(ref val)).xMin = ((Rect)(ref val)).xMin + 18f;
		Rect val3 = default(Rect);
		((Rect)(ref val3))._002Ector(((Rect)(ref val)).x + ((Rect)(ref val)).width - 48f, ((Rect)(ref val)).y, 20f, 20f);
		Texture2D val4 = (flag ? TexButton.Collapse : TexButton.Reveal);
		if (Widgets.ButtonImage(val2, val4, true, (string)null))
		{
			if (flag)
			{
				SoundStarter.PlayOneShotOnCamera(SoundDefOf.TabClose, (Map)null);
			}
			else
			{
				SoundStarter.PlayOneShotOnCamera(SoundDefOf.TabOpen, (Map)null);
			}
			categoryOpen[processDef] = !flag;
		}
		if (PF_Settings.productIcon)
		{
			((Rect)(ref val)).x = ((Rect)(ref val)).x + 20f;
			Widgets.DrawTextureFitted(new Rect(((Rect)(ref val)).x - 24f, ((Rect)(ref val)).y, 24f, 24f), (Texture)(object)ProcessorFramework_Utility.productIcons[processDef], 1f, 1f);
		}
		Widgets.Label(val, ((Def)processDef.thingDef).LabelCap);
		if (processDef.destroyChance != 0f)
		{
			Rect val5 = default(Rect);
			((Rect)(ref val5))._002Ector(((Rect)(ref val)).width - 80f, ((Rect)(ref val)).y + 2f, 32f, 20f);
			Text.Anchor = (TextAnchor)2;
			if (Mouse.IsOver(val5))
			{
				GUI.color = ITab_Pawn_Gear.HighlightColor;
				GUI.DrawTexture(val5, (Texture)(object)TexUI.HighlightTex);
			}
			TooltipHandler.TipRegion(val5, (Func<string>)(() => TaggedString.op_Implicit(Translator.Translate("PF_DestroyChanceTooltip"))), 23492389);
			Text.Font = (GameFont)0;
			GUI.color = Color.red;
			Widgets.Label(val5, GenText.ToStringByStyle(processDef.destroyChance, (ToStringStyle)8, (ToStringNumberSense)1));
			GUI.color = Color.white;
			Text.Font = (GameFont)1;
			Text.Anchor = (TextAnchor)0;
		}
		MultiCheckboxState val6 = ProcessStateOf(processDef);
		MultiCheckboxState val7 = Widgets.CheckboxMulti(val3, val6, true);
		if (val6 != val7 && (int)val7 != 2)
		{
			foreach (CompProcessor processorComp in processorComps)
			{
				processorComp.ToggleProcess(processDef, (int)val7 == 0);
			}
		}
		if (flag)
		{
			((Rect)(ref val)).xMin = ((Rect)(ref val)).xMin + 12f;
			List<ThingDef> list = processDef.ingredientFilter.AllowedThingDefs.ToList();
			GenCollection.SortBy<ThingDef, string>(list, (Func<ThingDef, string>)((ThingDef x) => ((Def)x).label));
			Rect val8 = default(Rect);
			foreach (ThingDef item in list)
			{
				((Rect)(ref val3)).y = ((Rect)(ref val3)).y + 22f;
				((Rect)(ref val)).y = ((Rect)(ref val)).y + 22f;
				if (PF_Settings.ingredientIcon)
				{
					Widgets.DrawTextureFitted(new Rect(((Rect)(ref val)).x - 24f, ((Rect)(ref val)).y, 24f, 24f), (Texture)(object)ProcessorFramework_Utility.ingredientIcons[item], 1f, 1f);
				}
				Widgets.Label(val, ((Def)item).LabelCap);
				if (processDef.efficiency != 1f)
				{
					((Rect)(ref val8))._002Ector(((Rect)(ref val)).width - 70f, ((Rect)(ref val)).y + 2f, 32f, 20f);
					Text.Anchor = (TextAnchor)2;
					if (Mouse.IsOver(val8))
					{
						GUI.color = ITab_Pawn_Gear.HighlightColor;
						GUI.DrawTexture(val8, (Texture)(object)TexUI.HighlightTex);
					}
					TooltipHandler.TipRegion(val8, (Func<string>)(() => TaggedString.op_Implicit(Translator.Translate("PF_EfficiencyTooltip"))), 23492389);
					Text.Font = (GameFont)0;
					GUI.color = Color.gray;
					Widgets.Label(val8, "x" + GenText.ToStringByStyle(1f / processDef.efficiency, (ToStringStyle)5, (ToStringNumberSense)1));
					Text.Font = (GameFont)1;
					GUI.color = Color.white;
					Text.Anchor = (TextAnchor)0;
				}
				MultiCheckboxState val9 = IngredientStateOf(processDef, item);
				MultiCheckboxState val10 = Widgets.CheckboxMulti(val3, val9, true);
				if (val9 != val10 && (int)val10 != 2)
				{
					foreach (CompProcessor processorComp2 in processorComps)
					{
						processorComp2.ToggleIngredient(processDef, item, (int)val10 == 0);
					}
				}
				((Rect)(ref listRect)).y = ((Rect)(ref listRect)).y + 22f;
			}
		}
		((Rect)(ref listRect)).y = ((Rect)(ref listRect)).y + 22f;
	}

	public MultiCheckboxState ProcessStateOf(ProcessDef processDef)
	{
		int num = processorComps.Count((CompProcessor x) => x.enabledProcesses.ContainsKey(processDef));
		if (num > 0)
		{
			if (num != processorComps.Count())
			{
				return (MultiCheckboxState)2;
			}
			return (MultiCheckboxState)0;
		}
		return (MultiCheckboxState)1;
	}

	public MultiCheckboxState IngredientStateOf(ProcessDef processDef, ThingDef ingredient)
	{
		ProcessFilter value;
		int num = processorComps.Count((CompProcessor x) => x.enabledProcesses.TryGetValue(processDef, out value) && value.allowedIngredients.Contains(ingredient));
		if (num > 0)
		{
			if (num != processorComps.Count())
			{
				return (MultiCheckboxState)2;
			}
			return (MultiCheckboxState)0;
		}
		return (MultiCheckboxState)1;
	}
}
