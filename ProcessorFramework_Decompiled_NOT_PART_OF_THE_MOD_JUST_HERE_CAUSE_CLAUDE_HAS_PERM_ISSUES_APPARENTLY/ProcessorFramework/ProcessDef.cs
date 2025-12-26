using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProcessorFramework;

public class ProcessDef : Def
{
	public int uniqueID;

	public ThingDef thingDef;

	public ThingFilter ingredientFilter = new ThingFilter();

	public float processDays = 6f;

	public float capacityFactor = 1f;

	public float efficiency = 1f;

	public bool usesTemperature = true;

	public FloatRange temperatureSafe = new FloatRange(-1f, 32f);

	public FloatRange temperatureIdeal = new FloatRange(7f, 32f);

	public float ruinedPerDegreePerHour = 2.5f;

	public float speedBelowSafe = 0.1f;

	public float speedAboveSafe = 1f;

	public FloatRange sunFactor = new FloatRange(1f, 1f);

	public FloatRange rainFactor = new FloatRange(1f, 1f);

	public FloatRange snowFactor = new FloatRange(1f, 1f);

	public FloatRange windFactor = new FloatRange(1f, 1f);

	public float unpoweredFactor;

	public float unfueledFactor;

	public float powerUseFactor = 1f;

	public float fuelUseFactor = 1f;

	public string filledGraphicSuffix;

	public bool usesQuality;

	public QualityDays qualityDays = new QualityDays(1f, 0f, 0f, 0f, 0f, 0f, 0f);

	public Color color = new Color(1f, 1f, 1f);

	public string customLabel = "";

	public float destroyChance;

	public List<BonusOutput> bonusOutputs = new List<BonusOutput>();

	public bool useStatForEfficiency;

	public StatDef efficiencyStat;

	public float statBaselineValue = 1f;

	public override void ResolveReferences()
	{
		ingredientFilter.ResolveReferences();
	}

	public override string ToString()
	{
		return ((object)thingDef)?.ToString() ?? "[invalid process]";
	}
}
