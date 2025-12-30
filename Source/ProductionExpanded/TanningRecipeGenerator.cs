using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace ProductionExpanded
{
  /// <summary>
  /// Generates a single generic tanning recipe that works on all raw leathers.
  /// Similar to butchering/cooking, one recipe handles all types.
  /// </summary>
  [StaticConstructorOnStartup]
  public static class TanningRecipeGenerator
  {
    static TanningRecipeGenerator()
    {
      GenerateTanningRecipe();
    }

    private static void GenerateTanningRecipe()
    {
      Log.Message("[Production Expanded] Generating tanning recipe...");

      // Get the tanning drum building
      var tanningDrum = DefDatabase<ThingDef>.GetNamedSilentFail("PE_TanningDrum");
      if (tanningDrum == null)
      {
        Log.Error(
          "[Production Expanded] Could not find PE_TanningDrum building! Tanning recipe not generated."
        );
        return;
      }

      // Get the raw leathers category
      var rawLeathersCategory = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("PE_RawLeathers");
      if (rawLeathersCategory == null)
      {
        Log.Error(
          "[Production Expanded] Could not find PE_RawLeathers category! Tanning recipe not generated."
        );
        return;
      }

      // Create ONE generic recipe that works on all raw leathers
      var recipe = new ProcessorRecipeDef
      {
        defName = "PE_TanLeather",
        label = "tan leather",
        description = "Tan raw hides, pelts, and skins into usable leather.",

        // Standard RecipeDef fields (required for bill config dialog)
        workAmount = 450f,
        workSpeedStat = StatDefOf.GeneralLaborSpeed,

        // ProcessorRecipeDef-specific fields
        ticksPerItem = 15000, // ~6 in-game hours
        cycles = 1, // Single-cycle processing
        ratio = 1.0f, // 1:1 conversion
        inputType = null, // Generic - determined dynamically
        outputType = null, // Generic - determined dynamically

        // Recipe list assignment
        recipeUsers = new List<ThingDef>(),

        // Required RecipeDef filters (must be initialized even if empty)
        fixedIngredientFilter = new ThingFilter(),
        defaultIngredientFilter = new ThingFilter(),

        // Ingredients list (required for bill config dialog to not crash)
        ingredients = new List<IngredientCount>
        {
          new IngredientCount()
        }
      };

      // Configure ingredient filter to include all raw leathers
      recipe.ingredients[0].SetBaseCount(1);
      recipe.ingredients[0].filter = new ThingFilter();

      // Add each raw leather individually to the filter (needed for UI to show them)
      foreach (var rawLeather in RawLeatherDefGenerator.RawToFinishedMap.Keys)
      {
        recipe.ingredients[0].filter.SetAllow(rawLeather, true);
        recipe.fixedIngredientFilter.SetAllow(rawLeather, true);
        recipe.defaultIngredientFilter.SetAllow(rawLeather, true);
      }

      // Add to DefDatabase
      DefGenerator.AddImpliedDef(recipe);

      // Add recipe to tanning drum
      if (tanningDrum.recipes == null)
        tanningDrum.recipes = new List<RecipeDef>();
      tanningDrum.recipes.Add(recipe);

      Log.Message("[Production Expanded] Generated generic tanning recipe.");
    }
  }
}
