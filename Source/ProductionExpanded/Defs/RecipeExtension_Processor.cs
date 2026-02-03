using System.Collections.Generic;
using Verse;

namespace ProductionExpanded
{
  public class RecipeExtension_Processor : DefModExtension
  {
    public bool useDynamicOutput = false;

    /// <summary>
    /// all manually configured dynamic ingredients
    /// dynamic ingredients get mapped to a product that will be added to the products list
    ///</summary>
    public List<ThingDef> ingredientsDynamic;

    /// <summary>
    /// all manually configured categories needed for product any item of specified category is a valid input
    /// dynamic ingredients get mapped to a product that will be added to the products list
    ///</summary>
    public List<ThingCategoryDef> ingredientsCategoryDynamic;

    /// <summary>
    /// all manually configured categories needed for product any item of specified category is a valid input
    /// static ingredients DON'T get mapped to a product. They are just another requirement for the recipe
    ///</summary>
    public List<ThingCategoryDef> ingredientsCategoryStatic;

    /// <summary>
    /// all manually configured static ingredients
    /// static ingredients DON'T get mapped to a product. They are just another requirement for the recipe
    ///</summary>
    public List<ThingDef> ingredientsStatic;

    /// <summary>
    /// any additional products that will ALLWAYS be produced regardless of the input e.g making mozzarella can alsp produce whey as a byproduct
    /// NOTE: if this is set to null and we do not have any dynamic ingredients we should throw an error
    ///</summary>
    public List<ThingDef> staticProducts;

    /// <summary>
    /// amount of dynamic ingredients needed from the list e.g if set to 1 only 1 item of the list is needed if set to 2 only 2 are needed if set to 0 ALL items are needed
    ///</summary>
    public int ingredientsDynamicRequiredCount = 0;

    /// <summary>
    /// amount of dynamic categories needed from the list e.g if set to 1 only 1 item of the list is needed if set to 2 only 2 are needed if set to 0 ALL items are needed
    /// if you want to make a player pick one item from each category e.g [anyOil, anyButter] you set this to 0
    /// if you want to make a player need 2 items from one category you set this to 0 and write the category twice in the dynamicIngredientsCategory [anyOil, anyOil]
    /// the code will automatically ensure the same ingredient WON'T be picked twice from the two oil categories
    ///
    /// NOTE:this does not allow mixing of the two uses cleanly e.g if i want the player to use 2 anyOil OR 2 anyButter there i CANNOT do that as if i set the count to two this will allow the player to use either 2 oils, 2 butters or 1 oil and 1 butter
    /// this is a design limitation with not much to be done to solve this issue unfortunately
    /// NOTE: another design limitation is the inablity to mix Categories and manually specified ingredients e.g if i want either 1 item of AnyOil OR 1 tallow there is currently no way to do so.
    ///</summary>
    public int ingredientCategoryDynamicRequiredCount = 0;

    /// <summary>
    /// amount of static ingredients needed from the list e.g if set to 1 only 1 item of the list is needed if set to 2 only 2 are needed if set to 0 ALL items are needed
    ///</summary>
    public int ingredientsStaticRequiredCount = 0;

    /// <summary>
    /// amount of static categories needed from the list e.g if set to 1 only 1 item of the list is needed if set to 2 only 2 are needed if set to 0 ALL items are needed
    /// if you want to make a player pick one item from each category e.g [anyOil, anyButter] you set this to 0
    /// if you want to make a player need 2 items from one category you set this to 0 and write the category twice in the dynamicIngredientsCategory [anyOil, anyOil]
    /// the code will automatically ensure the same ingredient WON'T be picked twice from the two oil categories
    ///
    /// NOTE:this does not allow mixing of the two uses cleanly e.g if i want the player to use 2 anyOil OR 2 anyButter there i CANNOT do that as if i set the count to two this will allow the player to use either 2 oils, 2 butters or 1 oil and 1 butter
    /// this is a design limitation with not much to be done to solve this issue unfortunately
    /// NOTE: another design limitation is the inablity to mix Categories and manually specified ingredients e.g if i want either 1 item of AnyOil OR 1 tallow there is currently no way to do so.
    ///</summary>
    public int ingredientCategoryStaticRequiredCount = 0;

    /// <summary>
    /// amount of ticks per cycle it takes to process each item inserted
    /// NOTE: if the process has two cycles for instance the total time per item will be 2*ticksPerItemIn
    ///</summary>
    public int ticksPerItemIn = 100;

    /// <summary>
    /// amount of cycles the process takes when a cycle ends a pawn has to go the the building and interact with it for a few seconds and then the next process stars 1 cycle means a pawn NEVER has to interact with it 2 cycles means at the 50% mark a pawn needs to interact 3 means at 33% and 66% etc
    ///</summary>
    public int cycles = 1;

    /// <summary>
    /// ratio of the products added to output from dynamic ingredients e.g if rawElephantHide gets converted to elephantLeather if we set this value to 0.
    /// NOTE: this affects ONLY dynamic products determined at runtime with the registry
    ///</summary>
    public float ratioDynamicIngredients = 1.0f;

    /// <summary>
    /// ratio of the products added to output from static ingredients e.g if rice gets converted to vinegar and we set this value to 0.5 we will get a 2 rice tp 1 vinegar ratio
    /// NOTE: this affects ONLY static products written down manually in the static products list
    ///</summary>
    public float ratioStaticIngredients = 1.0f;

    /// <summary>
    /// items in this recipe take this amount of capacity in the processor so if a furnace has 50 capacity and we set this recipe's value to 2 every item in (dynamic AND static) will take 2 space totalling up to 25 items
    /// NOTE: this has a limitation if we wanna affect dynamic items and static ones differently we cannot, this is niche so i think its fine.
    ///</summary>
    public float capacityFactor = 1f;
  }
}
