using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Defines a process that a passive processor building can perform.
  /// Unlike vanilla RecipeDef which is designed for active pawn work,
  /// ProcessDef is designed for buildings that process materials over time
  /// with minimal pawn interaction (just loading/unloading).
  /// </summary>
  public class ProcessDef : Def
  {
    // ============ INPUT ============
    /// <summary>
    /// Filter defining which items can be used as input for this process.
    /// Uses ThingFilter for flexibility - can specify individual ThingDefs
    /// or entire categories (like all raw leathers).
    /// </summary>
    public ThingFilter ingredientFilter = new ThingFilter();

    // ============ OUTPUT ============
    /// <summary>
    /// The item produced by this process. 
    /// If null, uses dynamic output resolution (looks up RawToFinishedMap based on input).
    /// If set, always produces this specific ThingDef.
    /// </summary>
    public ThingDef outputDef = null;

    // ============ TIMING ============
    /// <summary>
    /// How many ticks of processing time per unit of input material.
    /// Total time = ticksPerItem * inputCount * cycles
    /// </summary>
    public int ticksPerItem = 100;

    /// <summary>
    /// Number of processing cycles required. Each cycle may require
    /// pawn interaction to continue (useful for multi-stage processing).
    /// </summary>
    public int cycles = 1;

    /// <summary>
    /// Input to output ratio. 1.0 means 1 input = 1 output.
    /// 0.5 means 2 inputs = 1 output (50% efficiency).
    /// 2.0 means 1 input = 2 outputs (200% efficiency).
    /// </summary>
    public float ratio = 1f;

    // ============ METHODS ============
    /// <summary>
    /// Resolves references after def loading (required for ThingFilter).
    /// </summary>
    public override void ResolveReferences()
    {
      base.ResolveReferences();
      ingredientFilter?.ResolveReferences();
    }

    /// <summary>
    /// Gets the output ThingDef for a given input.
    /// If outputDef is set, returns that.
    /// If outputDef is null, looks up the finished version from RawLeatherDefGenerator.
    /// </summary>
    /// <param name="inputDef">The input ThingDef being processed</param>
    /// <returns>The ThingDef to produce, or null if no valid output found</returns>
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
      Log.Warning($"[Production Expanded] ProcessDef {defName}: No output mapping found for input {inputDef.defName}");
      return null;
    }

    /// <summary>
    /// Checks if a given ThingDef is allowed as input for this process.
    /// </summary>
    public bool AllowsInput(ThingDef thingDef)
    {
      return ingredientFilter.Allows(thingDef);
    }
  }
}
