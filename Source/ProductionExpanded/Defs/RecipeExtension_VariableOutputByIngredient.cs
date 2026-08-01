using System.Collections.Generic;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Scales a recipe's output stack by which ingredient was actually used, relative to a
  /// baseline ingredient. Attach to a RecipeDef via modExtensions; the recipe's declared
  /// product count is treated as the yield you get when crafting with <see cref="baseline"/>.
  /// Applied by <see cref="GenRecipe_PostProcessProduct_Patch"/>.
  /// </summary>
  public class VariableOutputByIngredient : DefModExtension
  {
    // The ingredient the recipe's declared product count is balanced against.
    public ThingDef baseline;

    // Ingredients that never count as "the" ingredient — e.g. the vinegar in a pickling
    // recipe, which is present in every variant and so says nothing about the result.
    public List<ThingDef> exclusions = new List<ThingDef>();

    // Optional ceiling on how far a pricier ingredient can inflate the yield, expressed
    // as a multiple of the baseline's market value.
    public bool useCap = false;
    public float capPriceInfluenceMultiplier;
  }
}
