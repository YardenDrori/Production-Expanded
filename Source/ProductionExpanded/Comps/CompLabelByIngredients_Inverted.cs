using System.Linq;
using RimWorld;
using Verse;

namespace ProductionExpanded
{
  // Names a crafted thing after what went into it, with the prefix first — "pickled carrots"
  // rather than "carrot pickles". VEF ships the non-inverted form as
  // VEF.Things.CompProperties_LabelByIngredients; this is the word-order variant it lacks.
  public class CompProperties_LabelByIngredients_Inverted : CompProperties
  {
    public string prefix = "";

    // Skipped when picking the ingredient to name the result after — typically a
    // preservative that appears in every variant of the recipe.
    public ThingDef ignoredIngredient;

    public CompProperties_LabelByIngredients_Inverted()
    {
      compClass = typeof(CompLabelByIngredients_Inverted);
    }
  }

  public class CompLabelByIngredients_Inverted : ThingComp
  {
    private CompIngredients ingredients;
    private string cachedLabel = "";

    public CompProperties_LabelByIngredients_Inverted Props =>
      (CompProperties_LabelByIngredients_Inverted)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
      if (ingredients == null)
      {
        ingredients = parent.TryGetComp<CompIngredients>();
      }
    }

    public override string TransformLabel(string label)
    {
      if (
        cachedLabel.NullOrEmpty()
        && ingredients != null
        && !ingredients.ingredients.NullOrEmpty()
      )
      {
        // FirstOrDefault, not First: a stack whose only ingredient is the ignored one
        // would otherwise throw.
        ThingDef named = ingredients.ingredients.FirstOrDefault(x =>
          x != Props.ignoredIngredient
        );
        if (named != null)
        {
          cachedLabel = Props.prefix + " " + named.LabelCap;
        }
      }

      return cachedLabel.NullOrEmpty() ? label : cachedLabel;
    }
  }
}
