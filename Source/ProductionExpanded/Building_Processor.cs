using RimWorld;
using Verse;
using UnityEngine;

namespace ProductionExpanded
{
    public class Building_Processor : Building_WorkTable
    {
        public override Graphic Graphic
        {
            get
            {
                CompResourceProcessor comp = this.GetComp<CompResourceProcessor>();
                if (comp != null && comp.getIsProcessing() && comp.CanContinueProcessing() && !comp.getIsWaitingForNextCycle() && !comp.getIsFinished())
                {
                    // Get the base texture path and add "_on" suffix
                    string texPath = def.graphicData.texPath + "_on";

                    // For stuffed buildings, use stuff color; otherwise use graphic color
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
