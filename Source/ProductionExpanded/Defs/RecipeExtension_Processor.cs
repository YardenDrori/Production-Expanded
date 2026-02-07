using System.Collections.Generic;
using Verse;

namespace ProductionExpanded
{
  public class ProcessorIngredient : Def
  {
    public List<ThingDef> thingDefs;
    public List<ThingCategoryDef> categoryDefs;
    public int count;
  }

  public class ProcessorProduct : Def
  {
    public ThingDef output;
    public int count;
  }

  public class RecipeExtension_Processor : DefModExtension
  {
    // Core recipe type flag
    public bool isStaticRecipe = false;  // true = exact ingredients, false = ratio-based

    // Ingredient and product definitions (used by STATIC recipes)
    public List<ProcessorIngredient> ingredients;
    public List<ProcessorProduct> products;

    // Processing parameters
    public int cycles = 1;
    public int ticksPerItemOut = 100;  // Processing time per output item produced

    // Ratio recipe parameters
    public float ratio = 1f;
    public float capacityFactor = 1f;
    public bool useDynamicOutput = false;  // Dynamic ratio: lookup output via RawToFinishedRegistry
  }
}
