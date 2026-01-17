using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  public class Building_Processor : Building_WorkTable, IBillGiver
  {
    // Cached references for performance
    private CompResourceProcessor cachedComp;
    private Graphic cachedOnGraphic;
    private string cachedOnTexPath;

    // ============ VANILLA COMPATIBILITY ============

    /// Re-implement interface method to prevent manual labor (WorkGiver_DoBill checks this)
    /// This allows the Bills Tab to work, but prevents colonists from reserving the table for manual work.
    bool IBillGiver.CurrentlyUsableForBills()
    {
      return false;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
      base.SpawnSetup(map, respawningAfterLoad);
      cachedComp = this.GetComp<CompResourceProcessor>();

      // Pre-cache the "_on" texture path
      if (def.graphicData != null)
      {
        cachedOnTexPath = def.graphicData.texPath + "_on";
      }
    }

    // ============ GRAPHIC LOGIC ============

    private bool ShouldShowOnGraphic()
    {
      if (cachedComp == null)
        return false;

      var props = cachedComp.getProps();
      if (!props.usesOnTexture)
        return false;

      // Show "on" texture if processing (including waiting for cycle interaction)
      if (!cachedComp.getIsProcessing())
        return false;

      // Show "off" if paused due to power/fuel/temp issues (but not if just waiting for cycle)
      if (!cachedComp.getIsReady() && !cachedComp.getIsBadTemp() && !cachedComp.getIsWaitingForNextCycle())
        return false;

      if (cachedComp.getIsFinished() && !props.keepOnTextureOnFinish)
        return false;

      return true;
    }

    public override Graphic Graphic
    {
      get
      {
        if (!ShouldShowOnGraphic())
          return base.Graphic;

        // Return cached graphic if available
        if (cachedOnGraphic != null)
          return cachedOnGraphic;

        // Build and cache the "_on" graphic
        Color color = (Stuff != null) ? Stuff.stuffProps.color : def.graphicData.color;
        Color colorTwo = def.graphicData.colorTwo;

        cachedOnGraphic = GraphicDatabase.Get(
          def.graphicData.graphicClass,
          cachedOnTexPath,
          def.graphicData.shaderType.Shader,
          def.graphicData.drawSize,
          color,
          colorTwo
        );

        return cachedOnGraphic;
      }
    }
  }
}
