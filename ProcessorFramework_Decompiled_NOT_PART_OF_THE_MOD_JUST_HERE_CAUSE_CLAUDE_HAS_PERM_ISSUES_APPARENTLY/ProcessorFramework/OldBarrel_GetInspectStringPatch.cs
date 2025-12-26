using HarmonyLib;
using RimWorld;
using Verse;

namespace ProcessorFramework;

[HarmonyPatch(typeof(Building_FermentingBarrel), "GetInspectString")]
public class OldBarrel_GetInspectStringPatch
{
	[HarmonyPrefix]
	public static bool OldBarrel_GetInspectString_Postfix(ref string __result)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		__result = TaggedString.op_Implicit(Translator.Translate("PF_OldBarrelInspectString"));
		return false;
	}
}
