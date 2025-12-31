using System.Collections.Generic;
using Verse;

namespace ProductionExpanded
{
  public class ProcessDef : Def
  {
    public ThingFilter ingredientFilter = new ThingFilter();
    public List<ThingDef> recipeUsers = new List<ThingDef>();
    public ThingDef outputDef = null;
    public int ticksPerItem = 100;
    public int cycles = 1;
    public float ratio = 1f;
    public float capacityFactor = 1f;

    public override void ResolveReferences()
    {
      base.ResolveReferences();
      ingredientFilter?.ResolveReferences();
    }

    public ThingDef GetOutputFor(ThingDef inputDef)
    {
      // If we have a fixed output, use it
      if (outputDef != null)
      {
        return outputDef;
      }

      // Dynamic output - look up in centralized registry
      var finished = RawToFinishedRegistry.GetFinished(inputDef);
      if (finished != null)
      {
        return finished;
      }

      // No mapping found
      Log.Warning(
        $"[Production Expanded] ProcessDef {defName}: No output mapping found for input {inputDef.defName}"
      );
      return null;
    }

    public bool AllowsInput(ThingDef thingDef)
    {
      return ingredientFilter.Allows(thingDef);
    }
  }
}
