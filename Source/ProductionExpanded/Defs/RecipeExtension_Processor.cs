using Verse;

namespace ProductionExpanded
{
  public class RecipeExtension_Processor : DefModExtension
  {
    public bool useDynamicOutput = false;
    public int ticksPerItem = 100;
    public int cycles = 1;
    public float ratio = 1.0f;
    public float capacityFactor = 1f;

    // Optional: Fixed output override if different from RecipeDef's main product
    // (Not usually needed if using dynamic output)
    public ThingDef fixedOutputDef = null;
  }
}
