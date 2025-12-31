using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using LudeonTK;

namespace ProductionExpanded
{
  public class ITab_ProcessSelection : ITab
  {
    private float viewHeight = 1000f;
    private Vector2 scrolllPosition;
    private bool mouseOverBill = false;
    private static readonly Vector2 WinSize = new Vector2(420f,480f);
    [TweakValue("Interface", 0f, 128f)]
    private static float PasteX = 48f;
    [TweakValue("Interface", 0f, 128f)]
    private static float PasteY = 3f;
    [TweakValue("Interface", 0f, 32f)]
    private static float PasteSize = 24f;

    public ITab_ProcessSelection()
    {
        size = WinSize; 
        labelKey = "TabBills"; 
    }

    protected override void FillTab() {
    Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
		Rect pasteRect = new Rect(WinSize.x - PasteX, PasteY, PasteSize, PasteSize);
    if (Widgets.ButtonImageFitted(pasteRect, TexButton.Paste, Color.white))
    {
        // Logic to paste the bill
    }

    }

  //   private static readonly Vector2 WinSize = new Vector2(420f, 480f);
  //   private Vector2 scrollPosition;
  //   private float viewHeight;
  //
  //   public ITab_ProcessSelection()
  //   {
  //     this.size = WinSize;
  //     this.labelKey = "TabBills";
  //   }
  //
  //   protected Building_Processor SelProcessor => (Building_Processor)SelThing;
  //
  //   protected override void FillTab()
  //   {
  //     Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
  //
  //     // Header
  //     Rect headerRect = rect.TopPartPixels(30f);
  //     Text.Font = GameFont.Medium;
  //     Widgets.Label(headerRect, "Processes");
  //     Text.Font = GameFont.Small;
  //
  //     // Add Bill Button
  //     Rect btnRect = new Rect(rect.width - 120f, 0f, 120f, 30f);
  //     if (Widgets.ButtonText(btnRect, "Add Process"))
  //     {
  //       Find.WindowStack.Add(new FloatMenu(GetProcessOptions()));
  //     }
  //
  //     // Bill List
  //     Rect listRect = new Rect(0f, 40f, rect.width, rect.height - 40f);
  //     Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);
  //
  //     Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
  //
  //     float curY = 0f;
  //     if (SelProcessor.activeBills.Count == 0)
  //     {
  //       Widgets.Label(new Rect(0f, curY, viewRect.width, 30f), "No active processes.");
  //       curY += 30f;
  //     }
  //     else
  //     {
  //       for (int i = 0; i < SelProcessor.activeBills.Count; i++)
  //       {
  //         ProcessBill bill = SelProcessor.activeBills[i];
  //         Rect rowRect = new Rect(0f, curY, viewRect.width, 80f); // Height for config rows
  //         DrawBillRow(rowRect, bill, i);
  //         curY += 80f;
  //       }
  //     }
  //
  //     viewHeight = curY;
  //     Widgets.EndScrollView();
  //   }
  //
  //   private List<FloatMenuOption> GetProcessOptions()
  //   {
  //     List<FloatMenuOption> list = new List<FloatMenuOption>();
  //     foreach (ProcessDef def in SelProcessor.ProcessesStack)
  //     {
  //       list.Add(
  //         new FloatMenuOption(
  //           def.LabelCap,
  //           delegate
  //           {
  //             SelProcessor.CreateBill(def);
  //           }
  //         )
  //       );
  //     }
  //     if (list.Count == 0)
  //     {
  //       list.Add(new FloatMenuOption("No available processes", null));
  //     }
  //     return list;
  //   }
  //
  //   private void DrawBillRow(Rect rect, ProcessBill bill, int index)
  //   {
  //     // Background alternate
  //     if (index % 2 == 0)
  //     {
  //       Widgets.DrawAltRect(rect);
  //     }
  //
  //     Widgets.DrawBox(rect); // Border
  //     rect = rect.ContractedBy(4f);
  //
  //     // 1st Line: Label and Delete
  //     Rect line1 = rect.TopPartPixels(24f);
  //     Rect delRect = new Rect(line1.xMax - 24f, line1.y, 24f, 24f);
  //     if (Widgets.ButtonImage(delRect, TexButton.CloseXSmall))
  //     {
  //       SelProcessor.DeleteBill(bill);
  //       SoundDefOf.Click.PlayOneShotOnCamera(null);
  //     }
  //
  //     Rect labelRect = new Rect(line1.x, line1.y, line1.width - 30f, 24f);
  //     Text.Font = GameFont.Small;
  //     Text.Anchor = TextAnchor.MiddleLeft;
  //     Widgets.Label(labelRect, bill.processDef.LabelCap);
  //     Text.Anchor = TextAnchor.UpperLeft;
  //
  //     // 2nd Line: Ingredients button and Repeat Mode
  //     Rect line2 = new Rect(rect.x, line1.yMax + 4f, rect.width, 24f);
  //
  //     // Ingredients Button
  //     if (Widgets.ButtonText(new Rect(line2.x, line2.y, 100f, 24f), "Ingredients"))
  //     {
  //       Find.WindowStack.Add(new Dialog_ProcessIngredients(bill));
  //     }
  //
  //     // Repeat Mode Button
  //     Rect modeRect = new Rect(line2.x + 110f, line2.y, 150f, 24f);
  //     string modeLabel = GetRepeatModeLabel(bill);
  //     if (Widgets.ButtonText(modeRect, modeLabel))
  //     {
  //       List<FloatMenuOption> options = new List<FloatMenuOption>
  //       {
  //         new FloatMenuOption("Do Forever", () => bill.repeatMode = ProcessRepeatMode.Forever),
  //         new FloatMenuOption("Do X Times", () => bill.repeatMode = ProcessRepeatMode.DoXTimes),
  //       };
  //
  //       if (bill.processDef.outputDef != null)
  //       {
  //         options.Add(
  //           new FloatMenuOption("Do Until X", () => bill.repeatMode = ProcessRepeatMode.DoUntillX)
  //         );
  //       }
  //       Find.WindowStack.Add(new FloatMenu(options));
  //     }
  //
  //     // X Counter Input (Active only for DoX or DoUntilX)
  //     if (bill.repeatMode != ProcessRepeatMode.Forever)
  //     {
  //       Rect countRect = new Rect(modeRect.xMax + 10f, line2.y, 60f, 24f);
  //       string buffer = bill.x.ToString();
  //       Widgets.TextFieldNumeric(countRect, ref bill.x, ref buffer, 1f, 9999f);
  //
  //       // +/- buttons could be added here for UX
  //     }
  //
  //     // 3rd Line: Status/Progress info (Optional, maybe future)
  //   }
  //
  //   private string GetRepeatModeLabel(ProcessBill bill)
  //   {
  //     switch (bill.repeatMode)
  //     {
  //       case ProcessRepeatMode.Forever:
  //         return "Do Forever";
  //       case ProcessRepeatMode.DoXTimes:
  //         return "Do X times";
  //       case ProcessRepeatMode.DoUntillX:
  //         return "Do until you have X";
  //       default:
  //         return "Unknown";
  //     }
  //   }
  // }
}
