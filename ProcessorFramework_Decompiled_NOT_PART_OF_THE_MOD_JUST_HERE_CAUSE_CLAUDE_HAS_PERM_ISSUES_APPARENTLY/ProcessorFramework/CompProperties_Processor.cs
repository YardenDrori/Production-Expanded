using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProcessorFramework;

public class CompProperties_Processor : CompProperties
{
	public bool showProductIcon = true;

	public Vector2 barOffset = new Vector2(0f, 0.25f);

	public Vector2 barScale = new Vector2(1f, 1f);

	public Vector2 productIconSize = new Vector2(1f, 1f);

	public bool independentProcesses;

	public bool parallelProcesses;

	public bool dropIngredients;

	public bool colorCoded;

	public int capacity = 25;

	public List<ProcessDef> processes = new List<ProcessDef>();

	public CompProperties_Processor()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		base.compClass = typeof(CompProcessor);
	}

	public override void ResolveReferences(ThingDef parentDef)
	{
		((CompProperties)this).ResolveReferences(parentDef);
		foreach (ProcessDef process in processes)
		{
			((Editable)process).ResolveReferences();
		}
	}
}
