using System.Collections.Generic;
using Verse;

namespace ProductionExpanded
{
  public class ProcessFilter : IExposable
  {
    public HashSet<ThingDef> allowedIngredients = new HashSet<ThingDef>();
    public ProcessDef processDef = null;

    public ProcessFilter() { }

    public ProcessFilter(IEnumerable<ThingDef> ingredients)
    {
      allowedIngredients = new HashSet<ThingDef>(ingredients);
    }

    public ProcessFilter(ProcessDef def)
    {
      this.processDef = def;
      if (def != null && def.ingredientFilter != null)
      {
        // Initialize with all allowed things from the definition
        foreach (var thing in def.ingredientFilter.AllowedThingDefs)
        {
          allowedIngredients.Add(thing);
        }
      }
    }

    public bool Allows(ThingDef def)
    {
      return allowedIngredients.Contains(def);
    }

    public void Allow(ThingDef def)
    {
      if (processDef.AllowsInput(def))
      {
        allowedIngredients.Add(def);
      }
      else
      {
        Log.Error("[Production Expanded] tried to allow use of invalid ingredient in recipe");
      }
    }

    public void Disallow(ThingDef def)
    {
      allowedIngredients.Remove(def);
    }

    public void Toggle(ThingDef def, bool allow)
    {
      if (allow)
        Allow(def);
      else
        Disallow(def);
    }

    public void ExposeData()
    {
      Scribe_Collections.Look(ref allowedIngredients, "allowedIngredients", LookMode.Def);

      // HashSet can be null after loading if it was empty
      if (allowedIngredients == null)
      {
        allowedIngredients = new HashSet<ThingDef>();
      }
    }
  }
}
