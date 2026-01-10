using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Comprehensive evidence-based leather categorization system.
  /// Analyzes leather properties, source animal characteristics, and multiple
  /// indicators to determine whether a leather should be categorized as
  /// Hide, Fur, or Scale for the raw leather processing chain.
  /// </summary>
  public static class LeatherTypeHelper
  {
    public enum LeatherCategory
    {
      Hide, // Thick-skinned animals (elephants, rhinos, hippos, etc.)
      Fur, // Furred/furry animals (wolves, bears, chinchillas, seals, etc.)
      Scale, // Reptilian/scaled animals (lizards, snakes, etc.)
    }

    public enum SizeCategory
    {
      Small,
      Medium,
      Large,
    }

    // Evidence scores for categorization
    private class CategoryEvidence
    {
      public int HideScore = 0;
      public int FurScore = 0;
      public int ScaleScore = 0;

      public LeatherCategory GetWinner()
      {
        // Use > instead of >= so ties don't favor Scale
        // Default to Hide when no clear winner (most animals are hide)
        if (ScaleScore > HideScore && ScaleScore > FurScore)
          return LeatherCategory.Scale;
        if (FurScore > HideScore)
          return LeatherCategory.Fur;
        return LeatherCategory.Hide;
      }

      public override string ToString()
      {
        return $"Hide:{HideScore} Fur:{FurScore} Scale:{ScaleScore} -> {GetWinner()}";
      }
    }

    // Hardcoded overrides (guaranteed categorization with +50 points)
    private static readonly Dictionary<string, LeatherCategory> hardcodedOverrides = new Dictionary<
      string,
      LeatherCategory
    >
    {
      // Scales - reptilian creatures
      { "Leather_Lizard", LeatherCategory.Scale },
      { "Leather_Cobra", LeatherCategory.Scale },
      { "Leather_Iguana", LeatherCategory.Scale },
      { "Leather_Tortoise", LeatherCategory.Scale },
      // Explicit hides
      { "Leather_Human", LeatherCategory.Hide },
      { "Leather_Pig", LeatherCategory.Hide },
      { "Leather_Elephant", LeatherCategory.Hide },
      { "Leather_Rhinoceros", LeatherCategory.Hide },
      // Explicit furs
      { "Leather_Wolf", LeatherCategory.Fur },
      { "Leather_Bear", LeatherCategory.Fur },
      { "Leather_Panthera", LeatherCategory.Fur },
      { "Leather_Fox", LeatherCategory.Fur },
      { "Leather_Bluefur", LeatherCategory.Fur },
      { "Leather_Chinchilla", LeatherCategory.Fur },
      { "Leather_GuineaPig", LeatherCategory.Fur },
      { "Leather_Mink", LeatherCategory.Fur },
      { "Leather_Thrumbo", LeatherCategory.Fur },
      { "Leather_AlphaThrumbo", LeatherCategory.Fur },
      { "Leather_Heavy", LeatherCategory.Fur },
      { "Leather_Sealskin", LeatherCategory.Fur },
    };

    /// <summary>
    /// Main categorization method using comprehensive evidence-based scoring.
    /// </summary>
    public static LeatherCategory GetLeatherCategory(ThingDef leatherDef)
    {
      var evidence = new CategoryEvidence();

      // 1. Check hardcoded overrides first (+50 points = guaranteed win)
      if (hardcodedOverrides.TryGetValue(leatherDef.defName, out var overrideCategory))
      {
        if (overrideCategory == LeatherCategory.Hide)
          evidence.HideScore += 50;
        else if (overrideCategory == LeatherCategory.Fur)
          evidence.FurScore += 50;
        else if (overrideCategory == LeatherCategory.Scale)
          evidence.ScaleScore += 50;
      }

      // 2. Analyze leather label (defName and label)
      AnalyzeLabel(leatherDef, evidence);

      // 3. Analyze leather description
      AnalyzeDescription(leatherDef, evidence);

      // 4. Analyze leather stats (insulation, armor)
      AnalyzeStats(leatherDef, evidence);

      // 5. Find and analyze source animal
      AnalyzeSourceAnimal(leatherDef, evidence);

      // Log the evidence for debugging (always log for now to debug categorization)
      // Log.Message(
      //   $"[Production Expanded] Leather categorization for {leatherDef.defName}: {evidence}"
      // );

      return evidence.GetWinner();
    }

    private static void AnalyzeLabel(ThingDef leatherDef, CategoryEvidence evidence)
    {
      string label = leatherDef.label.ToLower();
      string defName = leatherDef.defName.ToLower();

      // FUR indicators (+3 points each)
      if (label.Contains("fur"))
        evidence.FurScore += 3;
      if (label.Contains("pelt"))
        evidence.FurScore += 3;
      if (label.Contains("wool"))
        evidence.FurScore += 3;

      // "skin" is ambiguous (wolfskin = fur, lizardskin = scale, pigskin = hide)
      // So only +1 point to hide
      if (label.Contains("skin"))
        evidence.HideScore += 1;

      // HIDE indicators (+3 points)
      if (label.Contains("hide"))
        evidence.HideScore += 3;

      // SCALE indicators (+3 points)
      if (label.Contains("scale"))
        evidence.ScaleScore += 3;
      if (label.Contains("scaled"))
        evidence.ScaleScore += 3;

      // Animal name indicators (+2 points)
      // Furry animals
      if (label.Contains("wolf") || defName.Contains("wolf"))
        evidence.FurScore += 2;
      if (label.Contains("bear") || defName.Contains("bear"))
        evidence.FurScore += 2;
      if (label.Contains("fox") || defName.Contains("fox"))
        evidence.FurScore += 2;
      if (label.Contains("chinchilla") || defName.Contains("chinchilla"))
        evidence.FurScore += 2;
      if (label.Contains("mink") || defName.Contains("mink"))
        evidence.FurScore += 2;
      if (label.Contains("seal") || defName.Contains("seal"))
        evidence.FurScore += 2;
      if (label.Contains("thrumbo") || defName.Contains("thrumbo"))
        evidence.FurScore += 2;

      // Scaled/reptilian animals
      if (label.Contains("lizard") || defName.Contains("lizard"))
        evidence.ScaleScore += 2;
      if (label.Contains("snake") || defName.Contains("snake"))
        evidence.ScaleScore += 2;
      if (label.Contains("cobra") || defName.Contains("cobra"))
        evidence.ScaleScore += 2;
      if (label.Contains("dragon") || defName.Contains("dragon"))
        evidence.ScaleScore += 2;
      if (label.Contains("reptile") || defName.Contains("reptile"))
        evidence.ScaleScore += 2;

      // Heavy hide animals
      if (label.Contains("elephant") || defName.Contains("elephant"))
        evidence.HideScore += 2;
      if (label.Contains("rhino") || defName.Contains("rhino"))
        evidence.HideScore += 2;
      if (label.Contains("hippo") || defName.Contains("hippo"))
        evidence.HideScore += 2;
    }

    private static void AnalyzeDescription(ThingDef leatherDef, CategoryEvidence evidence)
    {
      if (string.IsNullOrEmpty(leatherDef.description))
        return;

      string desc = leatherDef.description.ToLower();

      // FUR indicators (+3 for strong, +2 for moderate, +1 for weak)
      if (desc.Contains("furry"))
        evidence.FurScore += 3;
      if (desc.Contains("fur"))
        evidence.FurScore += 3;
      if (desc.Contains("pelt"))
        evidence.FurScore += 3;
      if (desc.Contains("fluffy"))
        evidence.FurScore += 2;
      if (desc.Contains("wool"))
        evidence.FurScore += 3;
      if (desc.Contains("soft"))
        evidence.FurScore += 1;

      // SCALE indicators (+3 for strong, +2 for moderate)
      if (desc.Contains("scale"))
        evidence.ScaleScore += 3;
      if (desc.Contains("scaled"))
        evidence.ScaleScore += 3;
      if (desc.Contains("reptile"))
        evidence.ScaleScore += 3;
      if (desc.Contains("reptilian"))
        evidence.ScaleScore += 3;
      if (desc.Contains("cold-blooded"))
        evidence.ScaleScore += 2;

      // HIDE indicators - now cumulative, each word counts separately
      if (desc.Contains("hide"))
        evidence.HideScore += 3;
      if (desc.Contains("skin"))
        evidence.HideScore += 3;
      if (desc.Contains("thick skin"))
        evidence.HideScore += 2; // Extra bonus if both
      if (desc.Contains("tough"))
        evidence.HideScore += 2;
      if (desc.Contains("durable"))
        evidence.HideScore += 2;

      // Leather processing terms - now cumulative (each gives points)
      if (desc.Contains("tanned"))
        evidence.HideScore += 2;
      if (desc.Contains("dried"))
        evidence.HideScore += 2;
      if (desc.Contains("scraped"))
        evidence.HideScore += 2;
    }

    private static void AnalyzeStats(ThingDef leatherDef, CategoryEvidence evidence)
    {
      var stuffProps = leatherDef.stuffProps;
      if (stuffProps == null)
        return;

      // Get insulation and armor values
      float coldInsulation = leatherDef.GetStatValueAbstract(StatDefOf.StuffPower_Insulation_Cold);
      float sharpArmor = leatherDef.GetStatValueAbstract(StatDefOf.StuffPower_Armor_Sharp);

      // FUR pattern: High cold insulation (≥20)
      if (coldInsulation >= 30)
        evidence.FurScore += 3; // Excellent insulation (chinchilla, thrumbo)
      else if (coldInsulation >= 24)
        evidence.FurScore += 2; // Very good (wolf, bear)
      else if (coldInsulation >= 20)
        evidence.FurScore += 1; // Good (fox, bluefur)

      // HIDE pattern: High armor, moderate insulation
      if (sharpArmor >= 1.24)
        evidence.HideScore += 3; // Excellent armor (rhino, heavy, armadillo)
      else if (sharpArmor >= 1.12)
        evidence.HideScore += 2; // Very good (elephant, hippo, bear)

      // Removed weak scale stats check - was too broad and caught all low-stat leathers
    }

    private static void AnalyzeSourceAnimal(ThingDef leatherDef, CategoryEvidence evidence)
    {
      // Find which animal(s) produce this leather
      var sourceAnimals = DefDatabase<ThingDef>
        .AllDefs.Where(def => def.race != null && def.race.leatherDef == leatherDef)
        .ToList();

      if (!sourceAnimals.Any())
        return;

      // Analyze the first source animal (most common case)
      var animal = sourceAnimals.First();
      var race = animal.race;

      // Check animalType (only Canine and Dryad exist in vanilla)
      if (race.animalType == AnimalType.Canine)
      {
        evidence.FurScore += 3; // Dogs, wolves = fur
      }
      // Dryad is special (tree creatures), default to hide

      // Check body type (comprehensive list)
      if (race.body != null)
      {
        string bodyDefName = race.body.defName;

        // FUR bodies (+3 points)
        if (bodyDefName == "QuadrupedAnimalWithPawsAndTail")
          evidence.FurScore += 3; // Wolves, bears, big cats, foxes

        // SCALE bodies (+3 points)
        if (bodyDefName == "QuadrupedAnimalWithClawsTailAndJowl")
          evidence.ScaleScore += 3; // Iguanas, monitor lizards
        if (bodyDefName == "Snake")
          evidence.ScaleScore += 3; // Cobras
        if (bodyDefName == "TurtleLike")
          evidence.ScaleScore += 3; // Tortoises

        // HIDE bodies (+2 points)
        if (bodyDefName == "QuadrupedAnimalWithHoovesTusksAndTrunk")
          evidence.HideScore += 2; // Elephants, mastodons
        if (bodyDefName.Contains("Hooves"))
          evidence.HideScore += 1; // Cows, sheep, deer (generic hooved = hide)

        // FUR special cases
        if (bodyDefName == "QuadrupedAnimalWithHoovesAndHorn")
          evidence.FurScore += 2; // Thrumbo (special furry hooved animal)
        if (bodyDefName == "Pinniped" || bodyDefName == "PinnipedWithTusks")
          evidence.FurScore += 2; // Seals, walruses

        // Light/small animals (weak fur indicator)
        if (bodyDefName == "QuadrupedAnimalWithPaws")
          evidence.FurScore += 1; // Rats, capybaras (small furred animals)
      }

      // Check for reptilian traits in animal label
      string animalLabel = animal.label.ToLower();
      if (
        animalLabel.Contains("lizard")
        || animalLabel.Contains("snake")
        || animalLabel.Contains("reptile")
        || animalLabel.Contains("iguana")
        || animalLabel.Contains("cobra")
        || animalLabel.Contains("tortoise")
      )
      {
        evidence.ScaleScore += 2;
      }
    }

    /// <summary>
    /// Determines the size category for texture selection.
    /// </summary>
    public static SizeCategory GetSizeCategory(ThingDef leatherDef)
    {
      // Hardcoded size mappings
      var largeSizes = new HashSet<string>
      {
        "Leather_Heavy",
        "Leather_Elephant",
        "Leather_Rhinoceros",
        "Leather_Thrumbo",
        "Leather_Bear",
        "Leather_Panthera",
        "Leather_Mastodon",
        "Leather_AlphaThrumbo",
        "Leather_Hippo",
      };

      var smallSizes = new HashSet<string>
      {
        "Leather_Light",
        "Leather_GuineaPig",
        "Leather_Chinchilla",
        "Leather_Bird",
        "Leather_Patch",
        "Leather_Hare",
        "Leather_Squirrel",
        "Leather_Rat",
      };

      if (largeSizes.Contains(leatherDef.defName))
        return SizeCategory.Large;
      if (smallSizes.Contains(leatherDef.defName))
        return SizeCategory.Small;

      // Heuristics based on market value and label
      float marketValue = leatherDef.BaseMarketValue;
      string label = leatherDef.label.ToLower();

      if (
        label.Contains("heavy")
        || label.Contains("large")
        || label.Contains("thick")
        || label.Contains("elephant")
        || label.Contains("rhino")
        || marketValue >= 4.0f
      )
        return SizeCategory.Large;

      if (
        label.Contains("light")
        || label.Contains("small")
        || label.Contains("thin")
        || label.Contains("bird")
        || marketValue <= 1.5f
      )
        return SizeCategory.Small;

      // Default to medium
      return SizeCategory.Medium;
    }

    /// <summary>
    /// Gets the texture path based on category and size.
    /// Returns just the folder path - RimWorld automatically uses the folder name as the texture filename prefix.
    /// Examples: Things/Item/Resource/HideSmall
    ///           Things/Item/Resource/PeltLarge
    /// </summary>
    public static string GetTexturePath(LeatherCategory category, SizeCategory size)
    {
      string sizeName = size.ToString(); // Small, Medium, Large
      string typeName;

      if (category == LeatherCategory.Hide)
      {
        typeName = "Hide";
      }
      else if (category == LeatherCategory.Fur)
      {
        typeName = "Pelt";
      }
      else // Scale
      {
        typeName = "Scale";
      }

      string baseName = typeName + sizeName; // e.g., "HideSmall", "PeltLarge"
      return $"Things/Item/Resource/PE_{baseName}";
    }

    /// <summary>
    /// Gets the label for raw leather.
    /// For fur types: "raw wolf pelt" instead of "raw wolfskin"
    /// For hides: "raw elephant hide"
    /// For scales: "raw lizard skin"
    /// </summary>
    public static string GetRawLeatherLabel(ThingDef finishedLeatherDef)
    {
      var category = GetLeatherCategory(finishedLeatherDef);

      // Extract the animal name from the leather label
      // Vanilla patterns: "bear leather", "wolfskin", "elephant leather", "lizardskin"
      string animalName = finishedLeatherDef
        .label.Replace(" leather", "")
        .Replace("leather", "")
        .Replace("skin", "")
        .Trim();

      if (category == LeatherCategory.Fur)
      {
        return $"{animalName} pelt";
      }
      else if (category == LeatherCategory.Scale)
      {
        return $"{animalName} skin";
      }
      else // Hide
      {
        return $"{animalName} hide";
      }
    }

    /// <summary>
    /// Gets the description for raw leather.
    /// </summary>
    public static string GetRawLeatherDescription(ThingDef finishedLeatherDef)
    {
      var category = GetLeatherCategory(finishedLeatherDef);

      string categoryDesc;
      if (category == LeatherCategory.Fur)
      {
        categoryDesc =
          $"An untanned pelt with fur still attached. Must be processed at a tanning station to produce.";
      }
      else if (category == LeatherCategory.Scale)
      {
        categoryDesc =
          $"Raw reptilian skin with scales intact. Must be processed at a tanning station to produce.";
      }
      else
      {
        categoryDesc =
          $"An untanned animal hide. Must be processed at a tanning station to produce.";
      }

      return categoryDesc;
    }
  }
}
