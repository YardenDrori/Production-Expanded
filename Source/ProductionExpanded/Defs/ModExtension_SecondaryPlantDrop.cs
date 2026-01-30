using Verse;

namespace ProductionExpanded
{
  public class ModExtension_SecondaryPlantDrop : DefModExtension
  {
    public ThingDef SecondDropItem = null;
    public ThingDef ThirdDropItem = null;
    public float secondItemChance = 0f;
    public float thirdItemChance = 0f;
    public IntRange secondItemRange = new IntRange(1, 1);
    public IntRange thirdItemRange = new IntRange(1, 1);
    public bool dropSecondWhenLeafless = false;
    public bool dropThirdWhenLeafless = false;
  }
}
