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

      // Check finished state first
      if (cachedComp.getIsFinished())
      {
        // If keepOnTextureOnFinish is true, always show "on" when finished
        // If false, show "off"
        return props.keepOnTextureOnFinish;
      }

      // Check if paused (power/fuel issues) but not bad temp or waiting for cycle
      bool isPaused = !cachedComp.getIsReady() && !cachedComp.getIsBadTemp() && !cachedComp.getIsWaitingForNextCycle();
      if (isPaused)
      {
        // If showOnTextureWhenPaused is true, keep showing "on" even when paused
        // Otherwise show "off"
        return props.showOnTextureWhenPaused;
      }

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
