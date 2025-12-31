using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProductionExpanded
{
  /// <summary>
  /// ITab for selecting and managing processes on a Building_Processor.
  /// Follows vanilla ITab_Bills pattern.
  /// </summary>
  public class ITab_ProcessSelection : ITab
  {
    private float viewHeight = 1000f;
    private Vector2 scrollPosition;
    private ProcessBill mouseoverBill = null;

    private static readonly Vector2 WinSize = new Vector2(420f, 480f);

    [TweakValue("Interface", 0f, 128f)]
    private static float PasteX = 48f;

    [TweakValue("Interface", 0f, 128f)]
    private static float PasteY = 3f;

    [TweakValue("Interface", 0f, 32f)]
    private static float PasteSize = 24f;

    private const float TopAreaHeight = 35f;
    private const float BillInterfaceSpacing = 6f;
    private const float ExtraViewHeight = 60f;
    private const int MaxBillCount = 15;

    protected Building_Processor SelProcessor => (Building_Processor)SelThing;

    public ITab_ProcessSelection()
    {
      size = WinSize;
      labelKey = "TabBills";
    }

    protected override void FillTab()
    {
      // ===== Paste Button (Top Right) =====
      Rect pasteRect = new Rect(WinSize.x - PasteX, PasteY, PasteSize, PasteSize);
      if (ProcessBillUtility.Clipboard != null)
      {
        // Check if clipboard bill is valid for this processor
        bool canPaste = CanPasteClipboard();

        if (!canPaste)
        {
          GUI.color = Color.gray;
          Widgets.DrawTextureFitted(pasteRect, TexButton.Paste, 1f);
          GUI.color = Color.white;
          if (Mouse.IsOver(pasteRect))
          {
            TooltipHandler.TipRegion(
              pasteRect,
              "Cannot paste: process not available on this building"
            );
          }
        }
        else if (SelProcessor.activeBills.Count >= MaxBillCount)
        {
          GUI.color = Color.gray;
          Widgets.DrawTextureFitted(pasteRect, TexButton.Paste, 1f);
          GUI.color = Color.white;
          if (Mouse.IsOver(pasteRect))
          {
            TooltipHandler.TipRegion(pasteRect, "Cannot paste: maximum bills reached (15)");
          }
        }
        else
        {
          if (Widgets.ButtonImageFitted(pasteRect, TexButton.Paste, Color.white))
          {
            PasteFromClipboard();
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
          }
          if (Mouse.IsOver(pasteRect))
          {
            TooltipHandler.TipRegion(
              pasteRect,
              "Paste bill: " + ProcessBillUtility.Clipboard.LabelCap
            );
          }
        }
      }

      // ===== Main Content Area =====
      Rect mainRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
      mouseoverBill = DoListing(mainRect);
    }

    /// <summary>
    /// Draws the bill listing with Add button and scrollable bill list.
    /// Returns the bill under mouse for ingredient radius display.
    /// </summary>
    private ProcessBill DoListing(Rect rect)
    {
      ProcessBill result = null;

      Widgets.BeginGroup(rect);
      Text.Font = GameFont.Small;

      // ===== Add Process Button =====
      if (SelProcessor.activeBills.Count < MaxBillCount)
      {
        Rect addButtonRect = new Rect(0f, 0f, 150f, 29f);
        if (Widgets.ButtonText(addButtonRect, "Add Bill"))
        {
          Find.WindowStack.Add(new FloatMenu(GetProcessOptions()));
        }
      }

      Text.Anchor = TextAnchor.UpperLeft;
      GUI.color = Color.white;

      // ===== Scrollable Bill List =====
      Rect outRect = new Rect(0f, TopAreaHeight, rect.width, rect.height - TopAreaHeight);
      Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

      Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

      float curY = 0f;

      // Draw each bill
      for (int i = 0; i < SelProcessor.activeBills.Count; i++)
      {
        ProcessBill bill = SelProcessor.activeBills[i];
        Rect billRect = bill.DoInterface(0f, curY, viewRect.width, i, SelProcessor);

        if (Mouse.IsOver(billRect))
        {
          result = bill;
        }

        curY += billRect.height + BillInterfaceSpacing;
      }

      // Update view height for scroll
      if (Event.current.type == EventType.Layout)
      {
        viewHeight = curY + ExtraViewHeight;
      }

      Widgets.EndScrollView();
      Widgets.EndGroup();

      return result;
    }

    /// <summary>
    /// Gets the list of available processes as FloatMenuOptions.
    /// </summary>
    private List<FloatMenuOption> GetProcessOptions()
    {
      List<FloatMenuOption> options = new List<FloatMenuOption>();

      foreach (ProcessDef processDef in SelProcessor.ProcessesStack)
      {
        ProcessDef localDef = processDef;

        // Get icon if there's an output
        Texture2D icon = null;
        if (processDef.outputDef != null)
        {
          icon = processDef.outputDef.uiIcon;
        }

        options.Add(
          new FloatMenuOption(
            processDef.LabelCap,
            delegate
            {
              SelProcessor.CreateBill(localDef);
              SoundDefOf.Tick_High.PlayOneShotOnCamera();
            },
            iconTex: icon,
            iconColor: Color.white
          )
        );
      }

      if (options.Count == 0)
      {
        options.Add(new FloatMenuOption("No processes available", null));
      }

      return options;
    }

    /// <summary>
    /// Checks if the clipboard bill can be pasted to this processor.
    /// </summary>
    private bool CanPasteClipboard()
    {
      if (ProcessBillUtility.Clipboard?.processDef == null)
        return false;

      // Check if this processor supports the process
      return SelProcessor.ProcessesStack.Contains(ProcessBillUtility.Clipboard.processDef);
    }

    /// <summary>
    /// Pastes the clipboard bill to this processor.
    /// </summary>
    private void PasteFromClipboard()
    {
      if (!CanPasteClipboard())
        return;

      ProcessBill newBill = ProcessBillUtility.Clipboard.Clone();
      newBill.Parent = SelProcessor;

      // Recreate filter for this processor
      if (newBill.processFilter == null && newBill.processDef != null)
      {
        newBill.processFilter = new ProcessFilter(newBill.processDef);
        newBill.processFilter.processDef = newBill.processDef;
      }

      SelProcessor.activeBills.Add(newBill);
    }

    /// <summary>
    /// Called each frame to draw ingredient search radius.
    /// </summary>
    public override void TabUpdate()
    {
      if (mouseoverBill != null && SelProcessor != null)
      {
        mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelProcessor.Position);
        mouseoverBill = null;
      }
    }
  }
}
