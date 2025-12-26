using Verse;

namespace ProcessorFramework;

public class PF_Settings : ModSettings
{
	public enum InitialProcessState
	{
		disabled,
		enabled,
		firstonly
	}

	public static bool showProcessIconGlobal = true;

	public static float processIconSize = 0.6f;

	public static bool singleItemIcon = true;

	public static bool productIcon = true;

	public static bool ingredientIcon = true;

	public static bool showCurrentQualityIcon = true;

	public static int defaultTargetQualityInt = 2;

	public static bool showProcessBar = true;

	public static bool replaceDestroyedProcessors = true;

	public static InitialProcessState initialProcessState = InitialProcessState.firstonly;

	public override void ExposeData()
	{
		((ModSettings)this).ExposeData();
		Scribe_Values.Look<bool>(ref showProcessIconGlobal, "PF_showProcessIconGlobal", true, true);
		Scribe_Values.Look<float>(ref processIconSize, "PF_processIconSize", 0.6f, true);
		Scribe_Values.Look<bool>(ref showCurrentQualityIcon, "PF_showCurrentQualityIcon", true, true);
		Scribe_Values.Look<bool>(ref singleItemIcon, "PF_singleItemIcon", true, true);
		Scribe_Values.Look<bool>(ref productIcon, "PF_productIcon", true, true);
		Scribe_Values.Look<bool>(ref ingredientIcon, "PF_ingredientIcon", true, true);
		Scribe_Values.Look<int>(ref defaultTargetQualityInt, "PF_defaultTargetQualityInt", 2, true);
		Scribe_Values.Look<bool>(ref replaceDestroyedProcessors, "PF_replaceDestroyedProcessors", true, true);
		Scribe_Values.Look<InitialProcessState>(ref initialProcessState, "PF_initialProcessState", InitialProcessState.firstonly, true);
		Scribe_Values.Look<bool>(ref showProcessBar, "PF_showProcessBar", true, true);
	}
}
