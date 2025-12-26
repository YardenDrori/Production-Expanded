using System;
using System.Collections.Generic;
using Verse;

namespace ProcessorFramework;

public class ProcessFilter : IExposable
{
	public List<ThingDef> allowedIngredients;

	public ProcessFilter()
	{
	}

	public ProcessFilter(List<ThingDef> ingredients)
	{
		allowedIngredients = ingredients;
	}

	public void ExposeData()
	{
		Scribe_Collections.Look<ThingDef>(ref allowedIngredients, "PF_allowedIngredients", (LookMode)4, Array.Empty<object>());
	}
}
