using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProductionExpanded
{
  public enum ProcessRepeatMode
  {
    Forever,
    DoUntillX,
    DoXTimes,
  }

  public enum ActionWithOutput
  {
    DropOnFloor,
    HaulToBestStockpile,
    HaulToSpecificStockpile,
  }

  public enum AllowedWorker
  {
    Any,
    Slave,
    Mech,
    SpecificPawn,
  }

  public class ProcessBill : IExposable
  {
    public Thing Parent = null;
    public ProcessDef processDef = null;
    public ProcessFilter processFilter = null;
    public ProcessRepeatMode repeatMode = ProcessRepeatMode.Forever;
    public ActionWithOutput actionWithOutput = ActionWithOutput.HaulToBestStockpile;
    public AllowedWorker allowedWorker = AllowedWorker.Any;
    public Zone_Stockpile destinationStockpile = null;
    public Pawn worker = null;
    public string label = null;
    public bool isSuspended = false;
    public int ingredientSearchRadius = 9999;
    public int x = 10;

    // UI Constants
    private const float RowHeight = 53f;
    private const float ButtonSize = 24f;
    private const float StatusLineHeight = 17f;

    /// <summary>
    /// Display label for the bill.
    /// </summary>
    public string LabelCap => label ?? processDef?.LabelCap ?? "Unknown Process";

    /// <summary>
    /// Status text for display in the bill row.
    /// </summary>
    public string StatusString
    {
      get
      {
        if (isSuspended)
          return null; // Suspended overlay handles this
        return ProcessBillUtility.GetRepeatInfoText(this);
      }
    }

    /// <summary>
    /// Base color for UI elements based on bill state.
    /// </summary>
    private Color BaseColor
    {
      get
      {
        if (!IsFulfilled() && !isSuspended)
          return Color.white;
        return new Color(1f, 0.7f, 0.7f, 0.7f);
      }
    }

    public bool IsFulfilled()
    {
      if (isSuspended)
        return true;
      if (repeatMode == ProcessRepeatMode.Forever)
      {
        return false;
      }
      if (repeatMode == ProcessRepeatMode.DoXTimes)
      {
        return x <= 0;
      }
      if (repeatMode == ProcessRepeatMode.DoUntillX)
      {
        if (Parent == null || Parent.Map == null)
          return false;

        ThingDef thingToCount = processDef.outputDef;
        if (thingToCount == null)
        {
          Log.Error("tried doing a do untill x bill for generic recipe");
          return false;
        }

        int currentCount = Parent.Map.resourceCounter.GetCount(thingToCount);
        return currentCount >= x;
      }
      return false;
    }

    /// <summary>
    /// Draws the bill row in the ITab.
    /// Returns the rect used for mouseover detection.
    /// </summary>
    public Rect DoInterface(float x, float y, float width, int index, Building_Processor processor)
    {
      Rect rect = new Rect(x, y, width, RowHeight);

      Color color = GUI.color = BaseColor;
      Text.Font = GameFont.Small;

      // Alternating row background
      if (index % 2 == 0)
      {
        Widgets.DrawAltRect(rect);
      }

      Widgets.BeginGroup(rect);

      // ===== Reorder Buttons (Left Side) =====
      int billIndex = processor.activeBills.IndexOf(this);
      Rect upRect = new Rect(0f, 0f, ButtonSize, ButtonSize);
      if (billIndex > 0)
      {
        if (Widgets.ButtonImage(upRect, TexButton.ReorderUp, color))
        {
          ReorderBill(processor, -1);
          SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
        TooltipHandler.TipRegion(upRect, "Move bill up");
      }

      Rect downRect = new Rect(0f, ButtonSize, ButtonSize, ButtonSize);
      if (billIndex < processor.activeBills.Count - 1)
      {
        if (Widgets.ButtonImage(downRect, TexButton.ReorderDown, color))
        {
          ReorderBill(processor, 1);
          SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
        TooltipHandler.TipRegion(downRect, "Move bill down");
      }

      // ===== Label =====
      GUI.color = color;
      Rect labelRect = new Rect(28f, 0f, rect.width - 48f - 20f, rect.height + 5f);
      Widgets.Label(labelRect, LabelCap);

      // ===== Delete Button (Top Right) =====
      Rect deleteRect = new Rect(rect.width - ButtonSize, 0f, ButtonSize, ButtonSize);
      if (
        Widgets.ButtonImage(deleteRect, TexButton.Delete, color, color * GenUI.SubtleMouseoverColor)
      )
      {
        processor.DeleteBill(this);
        SoundDefOf.Click.PlayOneShotOnCamera();
      }
      TooltipHandler.TipRegion(deleteRect, "Delete this bill");

      // ===== Copy Button =====
      Rect copyRect = new Rect(deleteRect.x - ButtonSize - 4f, 0f, ButtonSize, ButtonSize);
      if (Widgets.ButtonImageFitted(copyRect, TexButton.Copy, color))
      {
        ProcessBillUtility.Clipboard = Clone();
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
      }
      TooltipHandler.TipRegion(copyRect, "Copy this bill");

      // ===== Suspend Button =====
      Rect suspendRect = new Rect(copyRect.x - ButtonSize, 0f, ButtonSize, ButtonSize);
      if (Widgets.ButtonImage(suspendRect, TexButton.Suspend, color))
      {
        isSuspended = !isSuspended;
        SoundDefOf.Click.PlayOneShotOnCamera();
      }
      TooltipHandler.TipRegion(suspendRect, isSuspended ? "Resume this bill" : "Suspend this bill");

      // ===== Config Row (Second Line) =====
      DoConfigInterface(rect.AtZero(), color, processor);

      Widgets.EndGroup();

      // ===== Suspended Overlay =====
      if (isSuspended)
      {
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect suspendedRect = new Rect(
          rect.x + rect.width / 2f - 70f,
          rect.y + rect.height / 2f - 20f,
          140f,
          40f
        );
        GUI.DrawTexture(suspendedRect, TexUI.GrayTextBG);
        Widgets.Label(suspendedRect, "SUSPENDED");
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
      }

      Text.Font = GameFont.Small;
      GUI.color = Color.white;
      return rect;
    }

    /// <summary>
    /// Draws the config buttons on the bill row.
    /// </summary>
    private void DoConfigInterface(Rect baseRect, Color baseColor, Building_Processor processor)
    {
      // Repeat Info Text (far left)
      string infoText = ProcessBillUtility.GetRepeatInfoText(this);
      if (!string.IsNullOrEmpty(infoText))
      {
        Rect infoRect = new Rect(ButtonSize, 29f, 100f, 24f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(infoRect, infoText);
        Text.Anchor = TextAnchor.UpperLeft;
      }

      // Using WidgetRow for right-aligned buttons
      WidgetRow widgetRow = new WidgetRow(
        baseRect.xMax - 4f,
        baseRect.y + 29f,
        UIDirection.LeftThenUp
      );

      // Details Button
      if (widgetRow.ButtonText("Details..."))
      {
        Find.WindowStack.Add(new Dialog_ProcessConfig(this, processor));
      }

      // Repeat Mode Button
      string modeLabel = ProcessBillUtility.GetRepeatModeLabel(repeatMode).PadRight(15);
      if (widgetRow.ButtonText(modeLabel))
      {
        ProcessBillUtility.MakeRepeatModeConfigFloatMenu(this);
      }

      // Plus Button
      if (widgetRow.ButtonIcon(TexButton.Plus))
      {
        if (repeatMode == ProcessRepeatMode.Forever)
        {
          repeatMode = ProcessRepeatMode.DoXTimes;
          this.x = 1;
        }
        else
        {
          this.x += GenUI.CurrentAdjustmentMultiplier();
        }
        SoundDefOf.DragSlider.PlayOneShotOnCamera();
      }

      // Minus Button
      if (widgetRow.ButtonIcon(TexButton.Minus))
      {
        if (repeatMode == ProcessRepeatMode.Forever)
        {
          repeatMode = ProcessRepeatMode.DoXTimes;
          this.x = 1;
        }
        else
        {
          this.x = Mathf.Max(0, this.x - GenUI.CurrentAdjustmentMultiplier());
        }
        SoundDefOf.DragSlider.PlayOneShotOnCamera();
      }
    }

    /// <summary>
    /// Reorders this bill within the processor's bill list.
    /// </summary>
    private void ReorderBill(Building_Processor processor, int offset)
    {
      int index = processor.activeBills.IndexOf(this);
      int newIndex = index + offset;
      if (newIndex >= 0 && newIndex < processor.activeBills.Count)
      {
        processor.activeBills.Remove(this);
        processor.activeBills.Insert(newIndex, this);
      }
    }

    /// <summary>
    /// Creates a copy of this bill for clipboard.
    /// </summary>
    public ProcessBill Clone()
    {
      ProcessBill clone = new ProcessBill();
      clone.processDef = processDef;
      clone.repeatMode = repeatMode;
      clone.actionWithOutput = actionWithOutput;
      clone.allowedWorker = allowedWorker;
      clone.label = label;
      clone.isSuspended = isSuspended;
      clone.ingredientSearchRadius = ingredientSearchRadius;
      clone.x = this.x;

      // Deep copy the filter
      if (processFilter != null)
      {
        clone.processFilter = new ProcessFilter(processDef);
        clone.processFilter.processDef = processDef;
        foreach (var ingredient in processFilter.allowedIngredients)
        {
          clone.processFilter.allowedIngredients.Add(ingredient);
        }
      }

      return clone;
    }

    /// <summary>
    /// Draws the ingredient search radius on the map when hovering over this bill.
    /// </summary>
    public void TryDrawIngredientSearchRadiusOnMap(IntVec3 center)
    {
      if (ingredientSearchRadius < 999 && ingredientSearchRadius > 0)
      {
        GenDraw.DrawRadiusRing(center, ingredientSearchRadius);
      }
    }

    public void ExposeData()
    {
      Scribe_Defs.Look(ref processDef, "processDef");
      Scribe_Deep.Look(ref processFilter, "processFilter");
      Scribe_Values.Look(ref repeatMode, "repeatMode", ProcessRepeatMode.Forever);
      Scribe_Values.Look(
        ref actionWithOutput,
        "actionWithOutput",
        ActionWithOutput.HaulToBestStockpile
      );
      Scribe_Values.Look(ref allowedWorker, "allowedWorker", AllowedWorker.Any);
      Scribe_References.Look(ref destinationStockpile, "destinationStockpile");
      Scribe_References.Look(ref worker, "worker");
      Scribe_Values.Look(ref label, "label");
      Scribe_Values.Look(ref isSuspended, "isSuspended", false);
      Scribe_Values.Look(ref ingredientSearchRadius, "ingredientSearchRadius", 9999);
      Scribe_Values.Look(ref x, "x", 10);

      if (Scribe.mode == LoadSaveMode.PostLoadInit)
      {
        if (processFilter == null && processDef != null)
        {
          processFilter = new ProcessFilter();
          processFilter.processDef = processDef;
        }
      }
    }
  }
}
