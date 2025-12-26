using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProcessorFramework;

[HarmonyPatch(typeof(MainTabWindow_Inspect), "CurTabs", MethodType.Getter)]
public class CurTabsPatch
{
	private static List<object> _cachedSelectedObjects = new List<object>();

	private static IEnumerable<InspectTabBase> _cachedResult;

	[HarmonyPostfix]
	public static void CurTabs_Postfix(ref IEnumerable<InspectTabBase> __result)
	{
		List<object> selectedObjects = Find.Selector.SelectedObjects;
		if (selectedObjects == null || selectedObjects.Count == 0)
		{
			return;
		}
		int count = selectedObjects.Count;
		if (_cachedSelectedObjects.Count == count)
		{
			if (_cachedResult == null)
			{
				return;
			}
			bool flag = true;
			for (int i = 0; i < count; i++)
			{
				if (_cachedSelectedObjects[i] != selectedObjects[i])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				__result = _cachedResult;
				return;
			}
		}
		_cachedSelectedObjects.Clear();
		_cachedSelectedObjects.AddRange(selectedObjects);
		_cachedResult = null;
		object obj = selectedObjects[0];
		ThingWithComps val = (ThingWithComps)((obj is ThingWithComps) ? obj : null);
		if (val == null || ((Thing)val).Faction != Faction.OfPlayerSilentFail)
		{
			return;
		}
		for (int j = 1; j < count; j++)
		{
			object obj2 = selectedObjects[j];
			ThingWithComps val2 = (ThingWithComps)((obj2 is ThingWithComps) ? obj2 : null);
			if (val2 == null || ((Thing)val2).Faction != Faction.OfPlayerSilentFail || ((Thing)val2).def != ((Thing)val).def)
			{
				return;
			}
		}
		if (ThingCompUtility.TryGetComp<CompProcessor>((Thing)(object)val) != null)
		{
			_cachedResult = ((Thing)val).GetInspectTabs();
			__result = _cachedResult;
		}
	}
}
