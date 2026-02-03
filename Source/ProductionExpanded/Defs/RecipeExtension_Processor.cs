using System.Collections.Generic;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// A single ingredient slot for a processor recipe.
  /// Use either thingDef (specific item) or category (any item from category), not both.
  /// </summary>
  public class ProcessorIngredient
  {
    /// <summary>
    /// Specific item required. Mutually exclusive with category.
    /// </summary>
    public ThingDef thingDef;

    /// <summary>
    /// Any item from this category is accepted. Mutually exclusive with thingDef.
    /// When dynamic, whichever specific item the pawn delivers gets looked up in the registry.
    /// </summary>
    public ThingCategoryDef category;

    /// <summary>
    /// How many items are needed for this ingredient slot.
    /// -1 (default) = scaling mode: accepts any amount from 1 up to processor capacity.
    ///   This is the current furnace/tannery behavior where you dump in however many you have.
    /// >0 = fixed mode: exactly this many are required before processing can start.
    ///   Used for batch recipes like 15 vinegar + 50 grapes.
    /// </summary>
    public int count = -1;

    /// <summary>
    /// If true, the specific item inserted will be looked up in RawToFinishedRegistry
    /// and its finished counterpart will be added to the output.
    /// Output quantity for this slot = input quantity * ratioDynamic.
    /// </summary>
    public bool dynamic = false;

    /// <summary>
    /// Determines ratio between scaling ingredients.
    /// If one ingredient's ratio is set to 2 and another's is set to 1,
    /// for every 2 of ingredient A you need 1 of ingredient B.
    /// Ignored for fixed count ingredients.
    /// </summary>
    public float ratio = 1;

    /// <summary>
    /// How much processor capacity each unit of this ingredient takes.
    /// e.g. pig iron at 1.0 and coal at 1.34 in a capacity 50 processor:
    ///   one recipe unit (ratio 2:1) = 2*1.0 + 1*1.34 = 3.34 capacity
    ///   max units = floor(50/3.34) = 14 → 28 pig iron + 14 coal
    /// Default 1.0 means each item takes 1 capacity.
    /// </summary>
    public float capacityPerItem = 1f;

    public bool IsScaling => count <= 0;
    public bool IsFixed => count > 0;
    public bool IsCategory => category != null;
    public bool IsSpecific => thingDef != null;
    public bool IsNull => !IsCategory && !IsSpecific;
  }

  /// <summary>
  /// A fixed output product always produced when the recipe completes.
  /// Dynamic outputs (from registry lookups) are added on top of these.
  /// </summary>
  public class ProcessorProduct
  {
    public ThingDef thingDef;
    public int count = 1;
  }

  public class RecipeExtension_Processor : DefModExtension
  {
    /// <summary>
    /// All ingredient slots for this recipe. Each entry defines one input requirement
    /// with its own item/category, count, and dynamic flag.
    /// If null/empty, falls back to the vanilla recipe's ingredients and products tags.
    ///
    /// Examples:
    ///   Scaling hide tanning (current behavior):
    ///     ingredient with category=RawHides, dynamic=true (no count = accepts any amount)
    ///
    ///   Fixed batch balsamic vinegar:
    ///     ingredient with thingDef=Vinegar, count=15
    ///     ingredient with thingDef=Grapes, count=50
    ///
    ///   Mixed (fixed catalyst + scaling input):
    ///     ingredient with category=RawOres, dynamic=true (scaling)
    ///     ingredient with thingDef=Coal, count=5 (fixed per batch)
    /// </summary>
    public List<ProcessorIngredient> ingredients;

    /// <summary>
    /// Fixed output products always produced when processing completes.
    /// e.g. 12 balsamic vinegar from a vinegar + grapes recipe.
    /// Dynamic ingredients add additional outputs on top of these via registry lookup.
    /// If null and no dynamic ingredients exist, this is an error.
    /// </summary>
    public List<ProcessorProduct> products;

    /// <summary>
    /// Multiplier applied to dynamic ingredient output quantities.
    /// e.g. if 10 raw elephant hide is inserted and ratioDynamic = 0.8,
    /// output will be 8 elephant leather.
    /// Only affects outputs resolved via RawToFinishedRegistry, not fixed products.
    /// </summary>
    public float ratioDynamic = 1.0f;

    /// <summary>
    /// Ticks per input item per cycle.
    /// Total processing time = ticksPerItemIn * totalInputCount * cycles.
    /// For fixed recipes the total input count is constant (e.g. 15 + 50 = 65).
    /// For scaling recipes more items = proportionally more time.
    /// </summary>
    public int ticksPerItemOut = 100;

    /// <summary>
    /// Number of processing cycles. When a cycle ends a pawn must interact with the building
    /// before the next cycle starts.
    /// 1 = no pawn interaction needed, 2 = interaction at 50%, 3 = at 33% and 66%, etc.
    /// </summary>
    public int cycles = 1;

    /// <summary>
    /// True if any ingredient slot uses dynamic output (registry lookup).
    /// </summary>
    public bool UsesDynamicOutput
    {
      get
      {
        if (ingredients.NullOrEmpty())
          return false;
        for (int i = 0; i < ingredients.Count; i++)
        {
          if (ingredients[i].dynamic)
            return true;
        }
        return false;
      }
    }

    ///<summary>
    ///True if any ingredient slot is static (no reigstry lookup)
    ///</summary>
    public bool UsesStaticInput
    {
      get
      {
        if (ingredients.NullOrEmpty())
          return false;
        for (int i = 0; i < ingredients.Count; i++)
        {
          if (!ingredients[i].dynamic)
            return true;
        }
        return false;
      }
    }

    /// <summary>
    /// True if all ingredient slots have fixed counts (no scaling ingredients).
    /// When true, the processor won't start until every fixed requirement is met.
    /// </summary>
    public bool IsFixedBatch
    {
      get
      {
        if (ingredients.NullOrEmpty())
          return false;
        for (int i = 0; i < ingredients.Count; i++)
        {
          if (ingredients[i].IsScaling)
            return false;
        }
        return true;
      }
    }
  }
}
