using System.Reflection;
using HarmonyLib;
using Verse;

namespace ProcessorFramework;

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
	static HarmonyPatches()
	{
		new Harmony("Syrchalis.Rimworld.UniversalFermenter").PatchAll(Assembly.GetExecutingAssembly());
	}
}
