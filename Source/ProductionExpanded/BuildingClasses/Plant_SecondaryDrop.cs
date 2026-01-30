using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  public class Plant_SecondaryDrop : Plant
  {
    public override void PlantCollected(Pawn pawn, PlantDestructionMode plantDestructionMode)
    {
      if (plantDestructionMode != PlantDestructionMode.Flame)
      {
        ModExtension_SecondaryPlantDrop secondaryPlantDrop =
          this.def.GetModExtension<ModExtension_SecondaryPlantDrop>();
        if (secondaryPlantDrop == null)
        {
          Log.Error(
            $"[Production Expanded] {this.def.defName} is of type Plant_SecondaryDrop but doesnt have a ModExtension_SecondaryPlantDrop"
          );
        }
        if (
          !(this.LeaflessNow && !secondaryPlantDrop.dropSecondWhenLeafless)
          && Rand.Chance(secondaryPlantDrop.secondItemChance)
        )
        {
          Thing secondItem = ThingMaker.MakeThing(secondaryPlantDrop.SecondDropItem);
          int min = secondaryPlantDrop.secondItemRange.min;
          int max = secondaryPlantDrop.secondItemRange.max;
          secondItem.stackCount = Rand.Range(min, max);
          GenSpawn.TrySpawn(secondItem.def, this.Position, this.Map, out secondItem);
        }
        if (
          secondaryPlantDrop.ThirdDropItem != null
          && Rand.Chance(secondaryPlantDrop.thirdItemChance)
          && !(this.LeaflessNow && !secondaryPlantDrop.dropThirdWhenLeafless)
        )
        {
          Thing ThirdItem = ThingMaker.MakeThing(secondaryPlantDrop.ThirdDropItem);
          int min = secondaryPlantDrop.thirdItemRange.min;
          int max = secondaryPlantDrop.thirdItemRange.max;
          ThirdItem.stackCount = Rand.Range(min, max);
          GenSpawn.TrySpawn(ThirdItem.def, this.Position, this.Map, out ThirdItem);
        }
      }
      base.PlantCollected(pawn, plantDestructionMode);
    }
  }
}
