using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  public class Gizmo_ProcessorStatus : Gizmo_Slider
  {
    private CompResourceProcessor processor;
    private static bool draggingBar;

    protected override float Target
    {
      get => 0f; // Not draggable
      set { }
    }

    protected override float ValuePercent =>
      1f - ((float)processor.getCapacityRemaining() / processor.getProps().maxCapacity);

    protected override string Title => "Capacity";

    protected override bool IsDraggable => false;

    protected override string BarLabel
    {
      get
      {
        int filled = processor.getProps().maxCapacity - processor.getCapacityRemaining();
        return $"{filled} / {processor.getProps().maxCapacity}";
      }
    }

    protected override bool DraggingBar
    {
      get => draggingBar;
      set => draggingBar = value;
    }

    public Gizmo_ProcessorStatus(CompResourceProcessor processor)
    {
      this.processor = processor;
    }

    protected override string GetTooltip()
    {
      int filled = processor.getProps().maxCapacity - processor.getCapacityRemaining();
      return $"Capacity: {filled} / {processor.getProps().maxCapacity}";
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
      // Override to add custom icon display
      GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);

      // Draw icon of currently processing item if applicable
      ThingDef inputItem = processor.getInputItem();
      if (inputItem != null && processor.getIsProcessing())
      {
        // Draw small icon in top-right corner of gizmo
        Rect iconRect = new Rect(topLeft.x + Width - 32f - 5f, topLeft.y + 5f, 32f, 32f);
        Widgets.ThingIcon(iconRect, inputItem);
      }

      return result;
    }
  }
}
