using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  public class Building_Processor : Building_WorkTable, IBillGiver
  {
    // ============ VANILLA COMPATIBILITY ============

    // Re-implement interface method to prevent manual labor (WorkGiver_DoBill checks this)
    // This allows the Bills Tab to work, but prevents colonists from reserving the table for manual work.
    bool IBillGiver.CurrentlyUsableForBills()
    {
      return false;
    }

    // ============ GRAPHIC LOGIC ============
    // (Preserved from previous version)

    public override Graphic Graphic
    {
      get
      {
        CompResourceProcessor comp = this.GetComp<CompResourceProcessor>();
        if (
          comp != null
          && comp.getIsProcessing()
          && comp.CanContinueProcessing()
          && !comp.getIsWaitingForNextCycle()
          && !comp.getIsFinished()
          && comp.getProps().usesOnTexture
        )
        {
          string texPath = def.graphicData.texPath + "_on";
          Color color = (Stuff != null) ? Stuff.stuffProps.color : def.graphicData.color;
          Color colorTwo = def.graphicData.colorTwo;

          return GraphicDatabase.Get(
            def.graphicData.graphicClass,
            texPath,
            def.graphicData.shaderType.Shader,
            def.graphicData.drawSize,
            color,
            colorTwo
          );
        }
        return base.Graphic;
      }
    }
  }
}
