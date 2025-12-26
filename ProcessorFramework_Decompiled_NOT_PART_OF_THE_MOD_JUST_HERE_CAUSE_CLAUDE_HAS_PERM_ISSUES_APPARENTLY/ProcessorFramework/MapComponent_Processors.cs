using System.Collections.Generic;
using Verse;

namespace ProcessorFramework;

public class MapComponent_Processors : MapComponent
{
	[Unsaved(false)]
	public List<ThingWithComps> thingsWithProcessorComp = new List<ThingWithComps>();

	public List<Thing> cachedMapIngredients = new List<Thing>();

	public int lastTick;

	public List<Thing> PotentialIngredients
	{
		get
		{
			if (Find.TickManager.TicksGame > lastTick + 300)
			{
				cachedMapIngredients.Clear();
				foreach (ThingDef key in ProcessorFramework_Utility.ingredientIcons.Keys)
				{
					cachedMapIngredients.AddRange(base.map.listerThings.ThingsOfDef(key));
				}
				lastTick = Find.TickManager.TicksGame;
			}
			return cachedMapIngredients;
		}
	}

	public MapComponent_Processors(Map map)
		: base(map)
	{
	}

	public void Register(ThingWithComps thing)
	{
		thingsWithProcessorComp.Add(thing);
	}

	public void Deregister(ThingWithComps thing)
	{
		thingsWithProcessorComp.Remove(thing);
	}
}
