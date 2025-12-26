using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProcessorFramework;

public class ActiveProcess : IExposable
{
	private readonly CompProcessor processor;

	public ProcessDef processDef;

	public long activeProcessTicks;

	public int ingredientCount;

	public List<Thing> ingredientThings;

	public QualityCategory targetQuality;

	public float ruinedPercent;

	public float speedFactor;

	[Unsaved(false)]
	private Material activeProcessColorMaterial;

	public long ActiveProcessTicks
	{
		get
		{
			return activeProcessTicks;
		}
		set
		{
			if (value != activeProcessTicks)
			{
				activeProcessTicks = value;
				activeProcessColorMaterial = null;
			}
		}
	}

	public float ActiveProcessDays => (float)ActiveProcessTicks / 60000f;

	public float ActiveProcessPercent => Mathf.Clamp01(ActiveProcessDays / (processDef.usesQuality ? DaysToReachTargetQuality : processDef.processDays));

	public bool Complete
	{
		get
		{
			if (!(ActiveProcessPercent >= 1f))
			{
				return EmptyNow;
			}
			return true;
		}
	}

	public bool EmptyNow
	{
		get
		{
			if (processDef.usesQuality && ActiveProcessDays >= processDef.qualityDays.awful)
			{
				return processor.emptyNow;
			}
			return false;
		}
	}

	public bool Ruined => ruinedPercent >= 1f;

	public float SpeedFactor => speedFactor;

	public Map CurrentMap => ((Thing)((ThingComp)processor).parent).Map;

	public QualityCategory TargetQuality
	{
		get
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			if (!processDef.usesQuality)
			{
				return (QualityCategory)2;
			}
			return targetQuality;
		}
		set
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			if (value != targetQuality && processDef.usesQuality)
			{
				targetQuality = value;
				activeProcessColorMaterial = null;
			}
		}
	}

	public float DaysToReachTargetQuality => processDef.qualityDays.DaysForQuality(targetQuality);

	public QualityCategory CurrentQuality
	{
		get
		{
			if (!(ActiveProcessDays < processDef.qualityDays.poor))
			{
				if (!(ActiveProcessDays < processDef.qualityDays.normal))
				{
					if (!(ActiveProcessDays < processDef.qualityDays.good))
					{
						if (!(ActiveProcessDays < processDef.qualityDays.excellent))
						{
							if (!(ActiveProcessDays < processDef.qualityDays.masterwork))
							{
								if (!(ActiveProcessDays < processDef.qualityDays.legendary))
								{
									if (!(ActiveProcessDays >= processDef.qualityDays.legendary))
									{
										return (QualityCategory)2;
									}
									return (QualityCategory)6;
								}
								return (QualityCategory)5;
							}
							return (QualityCategory)4;
						}
						return (QualityCategory)3;
					}
					return (QualityCategory)2;
				}
				return (QualityCategory)1;
			}
			return (QualityCategory)0;
		}
	}

	public int EstimatedTicksLeft
	{
		get
		{
			if (!(SpeedFactor <= 0f))
			{
				return Mathf.Max(processDef.usesQuality ? Mathf.RoundToInt(DaysToReachTargetQuality * 60000f - (float)ActiveProcessTicks) : Mathf.RoundToInt(processDef.processDays * 60000f - (float)ActiveProcessTicks), 0);
			}
			return -1;
		}
	}

	public float CurrentPowerFactor
	{
		get
		{
			if (!processor.Powered)
			{
				return processDef.unpoweredFactor;
			}
			return 1f;
		}
	}

	public float CurrentFuelFactor
	{
		get
		{
			if (!processor.Fueled)
			{
				return processDef.unfueledFactor;
			}
			return 1f;
		}
	}

	public float CurrentSunFactor
	{
		get
		{
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			if (CurrentMap == null)
			{
				return 0f;
			}
			if (((FloatRange)(ref processDef.sunFactor)).Span == 0f)
			{
				return 1f;
			}
			float num = CurrentMap.skyManager.CurSkyGlow * (1f - processor.RoofCoverage);
			FloatRange sunGlowRange = Static_Weather.SunGlowRange;
			float trueMin = ((FloatRange)(ref sunGlowRange)).TrueMin;
			sunGlowRange = Static_Weather.SunGlowRange;
			return GenMath.LerpDouble(trueMin, ((FloatRange)(ref sunGlowRange)).TrueMax, processDef.sunFactor.min, processDef.sunFactor.max, num);
		}
	}

	public float CurrentTemperatureFactor
	{
		get
		{
			if (!processDef.usesTemperature)
			{
				return 1f;
			}
			float ambientTemperature = ((Thing)((ThingComp)processor).parent).AmbientTemperature;
			if (ambientTemperature < processDef.temperatureSafe.min)
			{
				return processDef.speedBelowSafe;
			}
			if (ambientTemperature > processDef.temperatureSafe.max)
			{
				return processDef.speedAboveSafe;
			}
			if (ambientTemperature < processDef.temperatureIdeal.min)
			{
				return GenMath.LerpDouble(processDef.temperatureSafe.min, processDef.temperatureIdeal.min, processDef.speedBelowSafe, 1f, ambientTemperature);
			}
			if (ambientTemperature > processDef.temperatureIdeal.max)
			{
				return GenMath.LerpDouble(processDef.temperatureIdeal.max, processDef.temperatureSafe.max, 1f, processDef.speedAboveSafe, ambientTemperature);
			}
			return 1f;
		}
	}

	public float CurrentRainFactor
	{
		get
		{
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			if (CurrentMap == null)
			{
				return 0f;
			}
			if (((FloatRange)(ref processDef.rainFactor)).Span == 0f)
			{
				return 1f;
			}
			if (CurrentMap.weatherManager.SnowRate != 0f)
			{
				return processDef.rainFactor.min;
			}
			float num = CurrentMap.weatherManager.RainRate * (1f - processor.RoofCoverage);
			FloatRange rainRateRange = Static_Weather.RainRateRange;
			float trueMin = ((FloatRange)(ref rainRateRange)).TrueMin;
			rainRateRange = Static_Weather.RainRateRange;
			return GenMath.LerpDoubleClamped(trueMin, ((FloatRange)(ref rainRateRange)).TrueMax, processDef.rainFactor.min, processDef.rainFactor.max, num);
		}
	}

	public float CurrentSnowFactor
	{
		get
		{
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			if (CurrentMap == null)
			{
				return 0f;
			}
			if (((FloatRange)(ref processDef.snowFactor)).Span == 0f)
			{
				return 1f;
			}
			float num = CurrentMap.weatherManager.SnowRate * (1f - processor.RoofCoverage);
			FloatRange snowRateRange = Static_Weather.SnowRateRange;
			float trueMin = ((FloatRange)(ref snowRateRange)).TrueMin;
			snowRateRange = Static_Weather.SnowRateRange;
			return GenMath.LerpDoubleClamped(trueMin, ((FloatRange)(ref snowRateRange)).TrueMax, processDef.snowFactor.min, processDef.snowFactor.max, num);
		}
	}

	public float CurrentWindFactor
	{
		get
		{
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			if (CurrentMap == null)
			{
				return 0f;
			}
			if (((FloatRange)(ref processDef.windFactor)).Span == 0f)
			{
				return 1f;
			}
			if (processor.RoofCoverage != 0f)
			{
				return processDef.windFactor.min;
			}
			FloatRange windSpeedRange = Static_Weather.WindSpeedRange;
			float trueMin = ((FloatRange)(ref windSpeedRange)).TrueMin;
			windSpeedRange = Static_Weather.WindSpeedRange;
			return GenMath.LerpDoubleClamped(trueMin, ((FloatRange)(ref windSpeedRange)).TrueMax, processDef.windFactor.min, processDef.windFactor.max, CurrentMap.windManager.WindSpeed);
		}
	}

	public string ProgressTooltip
	{
		get
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			StringBuilder stringBuilder = new StringBuilder();
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_SpeedTooltip1", NamedArgumentUtility.Named((object)GenText.ToStringPercent(ActiveProcessPercent), "COMPLETEPERCENT"), NamedArgumentUtility.Named((object)SpeedFactor.ToStringPercentColored(), "SPEED")));
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_SpeedTooltip2", NamedArgumentUtility.Named((object)CurrentTemperatureFactor.ToStringPercentColored(), "TEMPERATURE"), NamedArgumentUtility.Named((object)CurrentWindFactor.ToStringPercentColored(), "WIND"), NamedArgumentUtility.Named((object)CurrentRainFactor.ToStringPercentColored(), "RAIN"), NamedArgumentUtility.Named((object)CurrentSnowFactor.ToStringPercentColored(), "SNOW"), NamedArgumentUtility.Named((object)CurrentSunFactor.ToStringPercentColored(), "SUN")));
			if (!Complete)
			{
				ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_SpeedTooltip3", NamedArgumentUtility.Named((object)GenDate.ToStringTicksToPeriod(EstimatedTicksLeft, true, false, false, true, false), "ESTIMATED")));
			}
			return stringBuilder.ToString();
		}
	}

	public string QualityTooltip
	{
		get
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			if (!processDef.usesQuality)
			{
				TaggedString val = TranslatorFormattedStringExtensions.Translate("PF_QualityTooltipNA", NamedArgumentUtility.Named((object)processDef.thingDef, "PRODUCT"));
				return TaggedString.op_Implicit(((TaggedString)(ref val)).CapitalizeFirst());
			}
			StringBuilder stringBuilder = new StringBuilder();
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_QualityTooltip1", (ActiveProcessDays < processDef.qualityDays.awful) ? NamedArgumentUtility.Named((object)Translator.TranslateSimple("PF_None"), "CURRENT") : NamedArgumentUtility.Named((object)QualityUtility.GetLabel(CurrentQuality), "CURRENT"), NamedArgumentUtility.Named((object)QualityUtility.GetLabel(TargetQuality), "TARGET")));
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_QualityTooltip2", NamedArgumentUtility.Named((object)TimeForQualityLeft((QualityCategory)0), "AWFUL"), NamedArgumentUtility.Named((object)TimeForQualityLeft((QualityCategory)1), "POOR"), NamedArgumentUtility.Named((object)TimeForQualityLeft((QualityCategory)2), "NORMAL"), NamedArgumentUtility.Named((object)TimeForQualityLeft((QualityCategory)3), "GOOD"), NamedArgumentUtility.Named((object)TimeForQualityLeft((QualityCategory)4), "EXCELLENT"), NamedArgumentUtility.Named((object)TimeForQualityLeft((QualityCategory)5), "MASTERWORK"), NamedArgumentUtility.Named((object)TimeForQualityLeft((QualityCategory)6), "LEGENDARY")));
			return stringBuilder.ToString();
		}
	}

	public Material ProgressColorMaterial
	{
		get
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			if (activeProcessColorMaterial == null)
			{
				activeProcessColorMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(Static_Bar.ZeroProgressColor, Static_Bar.FermentedColor, ActiveProcessPercent), false);
			}
			return activeProcessColorMaterial;
		}
	}

	public ActiveProcess(CompProcessor parent)
	{
		processor = parent;
	}

	public void DoTicks(int ticks)
	{
		ActiveProcessTicks += Mathf.RoundToInt((float)ticks * SpeedFactor);
		if (!Ruined && processDef.usesTemperature)
		{
			float ambientTemperature = ((Thing)((ThingComp)processor).parent).AmbientTemperature;
			if (ambientTemperature > processDef.temperatureSafe.max)
			{
				ruinedPercent += (ambientTemperature - processDef.temperatureSafe.max) * (processDef.ruinedPerDegreePerHour / 2500f / 100f) * (float)ticks;
			}
			else if (ambientTemperature < processDef.temperatureSafe.min)
			{
				ruinedPercent -= (ambientTemperature - processDef.temperatureSafe.min) * (processDef.ruinedPerDegreePerHour / 2500f / 100f) * (float)ticks;
			}
			if (ruinedPercent >= 1f)
			{
				ruinedPercent = 1f;
				((ThingComp)processor).parent.BroadcastCompSignal("RuinedByTemperature");
			}
			else if (ruinedPercent < 0f)
			{
				ruinedPercent = 0f;
			}
		}
	}

	public void TickRare()
	{
		speedFactor = CalcSpeedFactor();
	}

	public void MergeProcess(Thing ingredient, float efficiency = 1f)
	{
		activeProcessTicks = Mathf.RoundToInt(GenMath.WeightedAverage(0f, (float)ingredient.stackCount, (float)activeProcessTicks, (float)ingredientCount));
		int num = Mathf.RoundToInt((float)ingredient.stackCount * efficiency);
		ingredientCount += num;
		Thing val = null;
		foreach (Thing ingredientThing in ingredientThings)
		{
			if (ingredientThing.CanStackWith(ingredient))
			{
				val = ingredientThing;
				break;
			}
		}
		if (val == null)
		{
			ingredientThings.Add(ingredient);
		}
		processor.innerContainer.TryAddOrTransfer(ingredient, ingredient.stackCount, true);
	}

	private float CalcSpeedFactor()
	{
		return Mathf.Max(CurrentPowerFactor * CurrentFuelFactor * CurrentTemperatureFactor * CurrentSunFactor * CurrentRainFactor * CurrentSnowFactor * CurrentWindFactor, 0f);
	}

	public string TimeForQualityLeft(QualityCategory qualityCategory)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(Mathf.RoundToInt(processDef.qualityDays.DaysForQuality(qualityCategory) * 60000f - (float)ActiveProcessTicks), 0);
		if (num != 0)
		{
			return GenDate.ToStringTicksToPeriod(num, true, false, false, true, false);
		}
		return TaggedString.op_Implicit(Translator.Translate("PF_None"));
	}

	public string ProcessTooltip(string ingredientLabel, string productLabel)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder stringBuilder = new StringBuilder();
		string text = (processDef.usesQuality ? (" (" + GenText.CapitalizeFirst(QualityUtility.GetLabel(TargetQuality)) + ")") : "");
		ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_CreatingTooltip1", NamedArgumentUtility.Named((object)productLabel, "PRODUCT"), NamedArgumentUtility.Named((object)ingredientLabel, "INGREDIENT"), NamedArgumentUtility.Named((object)text, "QUALITY")));
		ColoredText.AppendTagged(stringBuilder, processDef.usesQuality ? TranslatorFormattedStringExtensions.Translate("PF_CreatingTooltip2_Quality", NamedArgumentUtility.Named((object)GenDate.ToStringTicksToPeriod(Mathf.RoundToInt(processDef.qualityDays.awful * 60000f), true, false, true, true, false), "TOAWFUL")) : TranslatorFormattedStringExtensions.Translate("PF_CreatingTooltip2_NoQuality", NamedArgumentUtility.Named((object)GenDate.ToStringTicksToPeriod(Mathf.RoundToInt(processDef.processDays * 60000f), true, false, true, true, false), "TIME")));
		if (processDef.usesTemperature)
		{
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_CreatingTooltip3", NamedArgumentUtility.Named((object)GenText.ToStringTemperature(processDef.temperatureIdeal.min, "F1"), "MIN"), NamedArgumentUtility.Named((object)GenText.ToStringTemperature(processDef.temperatureIdeal.max, "F1"), "MAX")));
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_CreatingTooltip4", NamedArgumentUtility.Named((object)GenText.ToStringTemperature(processDef.temperatureSafe.min, "F1"), "MIN"), NamedArgumentUtility.Named((object)GenText.ToStringTemperature(processDef.temperatureSafe.max, "F1"), "MAX"), NamedArgumentUtility.Named((object)GenText.ToStringPercent(processDef.ruinedPerDegreePerHour / 100f), "PERHOUR")));
		}
		if (ruinedPercent > 0.05f)
		{
			ColoredText.AppendTagged(stringBuilder, TranslatorFormattedStringExtensions.Translate("PF_CreatingTooltip5", NamedArgument.op_Implicit(ColoredText.Colorize(GenText.ToStringPercent(ruinedPercent), Color.red))));
		}
		if (!((FloatRange)(ref processDef.temperatureSafe)).Includes(((Thing)((ThingComp)processor).parent).AmbientTemperature) && !Ruined)
		{
			TaggedString val = TranslatorFormattedStringExtensions.Translate("PF_CreatingTooltip6", NamedArgument.op_Implicit(GenText.ToStringTemperature(((Thing)((ThingComp)processor).parent).AmbientTemperature, "F1")));
			stringBuilder.Append(ColoredText.Colorize(((TaggedString)(ref val)).Resolve(), Color.red));
		}
		return stringBuilder.ToString();
	}

	public void ExposeData()
	{
		Scribe_Defs.Look<ProcessDef>(ref processDef, "PF_processDef");
		Scribe_Collections.Look<Thing>(ref ingredientThings, "ingredientThings", (LookMode)3, Array.Empty<object>());
		Scribe_Values.Look<float>(ref ruinedPercent, "PF_ruinedPercent", 0f, false);
		Scribe_Values.Look<int>(ref ingredientCount, "PF_ingredientCount", 0, false);
		Scribe_Values.Look<long>(ref activeProcessTicks, "PF_activeProcessTicks", 0L, false);
		Scribe_Values.Look<QualityCategory>(ref targetQuality, "targetQuality", (QualityCategory)2, false);
	}
}
