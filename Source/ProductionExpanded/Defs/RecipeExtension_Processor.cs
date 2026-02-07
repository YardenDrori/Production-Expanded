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
    public List<ProcessorIngredient> ingredients;
    public List<ProcessorProduct> products;

    public int cycles = 1;
    public int ticksPerItemOut = 100;

    public bool isSingleItemRatioBasedRecipe = false;
    public bool shouldMapOutputFromInputViaRegistry = false;
    public float ratio = 1f;
    public float capacityFactor = 1f;
  }
}
