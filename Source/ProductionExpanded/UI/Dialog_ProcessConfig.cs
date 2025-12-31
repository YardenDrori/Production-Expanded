using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProductionExpanded
{
  /// <summary>
  /// Configuration dialog for a ProcessBill, following vanilla Dialog_BillConfig pattern.
  /// </summary>
  public class Dialog_ProcessConfig : Window
  {
    private ProcessBill bill;
    private Building_Processor processor;
    private Vector2 scrollPosition;
    private string repeatCountBuffer;

    // Layout constants
    private const float RecipeIconSize = 34f;
    private static int RepeatModeSubdialogHeight = 180;
    private static int StoreModeSubdialogHeight = 30;
    private static int WorkerSelectionSubdialogHeight = 60;

    public override Vector2 InitialSize => new Vector2(700f, 550f);

    public Dialog_ProcessConfig(ProcessBill bill, Building_Processor processor)
    {
      this.bill = bill;
      this.processor = processor;
      this.forcePause = true;
      this.doCloseX = true;
      this.doCloseButton = true;
      this.absorbInputAroundWindow = true;
      this.closeOnClickedOutside = true;
      this.repeatCountBuffer = bill.x.ToString();
    }

    /// <summary>
    /// Called every frame to draw ingredient search radius on map.
    /// </summary>
    public override void WindowUpdate()
    {
      if (processor != null && bill.ingredientSearchRadius < 999)
      {
        bill.TryDrawIngredientSearchRadiusOnMap(processor.Position);
      }
    }

    public override void DoWindowContents(Rect inRect)
    {
      // Header with icon and title
      Text.Font = GameFont.Medium;
      Rect titleRect = new Rect(RecipeIconSize + 10f, 0f, 400f, RecipeIconSize);
      Widgets.Label(titleRect, bill.LabelCap);

      // Draw recipe icon
      Rect iconRect = new Rect(0f, 0f, RecipeIconSize, RecipeIconSize);
      if (bill.processDef?.outputDef != null)
      {
        Widgets.ThingIcon(iconRect, bill.processDef.outputDef);
      }

      Text.Font = GameFont.Small;

      // Calculate column widths
      float columnWidth = (int)((inRect.width - 20f) / 3f);
      float startY = 50f;

      // Left column - Process info
      Rect leftRect = new Rect(0f, startY, columnWidth - 10f, inRect.height - startY - CloseButSize.y);

      // Center column - Repeat mode and settings
      Rect centerRect = new Rect(columnWidth, startY, columnWidth - 10f, inRect.height - startY - CloseButSize.y);

      // Right column - Ingredient filter
      Rect rightRect = new Rect(columnWidth * 2, startY, columnWidth, inRect.height - startY - CloseButSize.y);

      // ===== LEFT COLUMN =====
      DoLeftColumn(leftRect);

      // ===== CENTER COLUMN =====
      DoCenterColumn(centerRect);

      // ===== RIGHT COLUMN =====
      DoRightColumn(rightRect);
    }

    private void DoLeftColumn(Rect rect)
    {
      Listing_Standard listing = new Listing_Standard();
      listing.Begin(rect);

      // Suspend/Resume button
      if (bill.isSuspended)
      {
        if (listing.ButtonText("Suspended"))
        {
          bill.isSuspended = false;
          SoundDefOf.Click.PlayOneShotOnCamera();
        }
      }
      else
      {
        if (listing.ButtonText("Active"))
        {
          bill.isSuspended = true;
          SoundDefOf.Click.PlayOneShotOnCamera();
        }
      }

      listing.Gap(12f);

      // Process description
      StringBuilder sb = new StringBuilder();

      if (bill.processDef != null)
      {
        if (!string.IsNullOrEmpty(bill.processDef.description))
        {
          sb.AppendLine(bill.processDef.description);
          sb.AppendLine();
        }

        // Time per cycle
        if (bill.processDef.ticksPerItem > 0)
        {
          float hours = bill.processDef.ticksPerItem / 2500f;
          sb.AppendLine("Time per Item: " + hours.ToString("F1") + " hours");
        }

        // Cycles
        if (bill.processDef.cycles > 1)
        {
          sb.AppendLine("Cycles: " + bill.processDef.cycles.ToString());
        }

        sb.AppendLine();

        // Required ingredients
        if (bill.processDef.ingredientFilter != null)
        {
          var allowed = bill.processDef.ingredientFilter.AllowedThingDefs.ToList();
          if (allowed.Count > 0)
          {
            sb.AppendLine("Accepts:");
            foreach (var def in allowed.Take(5))
            {
              sb.AppendLine("  - " + def.LabelCap);
            }
            if (allowed.Count > 5)
            {
              sb.AppendLine($"  ... and {allowed.Count - 5} more");
            }
          }
        }

        if (bill.processDef.outputDef != null)
        {
          sb.AppendLine();
          sb.AppendLine("Produces: " + bill.processDef.outputDef.LabelCap);
          if (bill.processDef.ratio > 1f)
          {
            sb.AppendLine("  x" + bill.processDef.ratio.ToString("F1"));
          }
        }
      }

      Text.Font = GameFont.Small;
      string infoText = sb.ToString();
      float textHeight = Text.CalcHeight(infoText, rect.width - 10f);
      listing.Label(infoText);

      listing.End();
    }

    private void DoCenterColumn(Rect rect)
    {
      Listing_Standard listing = new Listing_Standard();
      listing.Begin(rect);

      // ===== Repeat Mode Section =====
      Listing_Standard repeatSection = listing.BeginSection(RepeatModeSubdialogHeight);

      // Repeat mode button
      if (repeatSection.ButtonText(ProcessBillUtility.GetRepeatModeLabel(bill.repeatMode)))
      {
        ProcessBillUtility.MakeRepeatModeConfigFloatMenu(bill);
      }

      repeatSection.Gap();

      // Count settings based on mode
      if (bill.repeatMode == ProcessRepeatMode.DoXTimes)
      {
        repeatSection.Label("Repeat Count: " + bill.x);
        repeatSection.IntEntry(ref bill.x, ref repeatCountBuffer);
        if (bill.x < 1) bill.x = 1;
      }
      else if (bill.repeatMode == ProcessRepeatMode.DoUntillX)
      {
        string currentText = "Target: " + bill.x;
        if (processor != null && bill.processDef?.outputDef != null)
        {
          int current = processor.Map.resourceCounter.GetCount(bill.processDef.outputDef);
          currentText = $"Current: {current} / Target: {bill.x}";
        }
        repeatSection.Label(currentText);
        repeatSection.IntEntry(ref bill.x, ref repeatCountBuffer);
        if (bill.x < 1) bill.x = 1;
      }
      else
      {
        repeatSection.Label("This bill will repeat forever until suspended.");
      }

      listing.EndSection(repeatSection);

      listing.Gap();

      // ===== Output Destination Section =====
      Listing_Standard storeSection = listing.BeginSection(StoreModeSubdialogHeight);

      string storeModeLabel = GetOutputDestinationLabel();
      if (storeSection.ButtonText(storeModeLabel))
      {
        MakeStoreOptionsFloatMenu();
      }

      listing.EndSection(storeSection);

      listing.Gap();

      // ===== Worker Selection Section =====
      Listing_Standard workerSection = listing.BeginSection(WorkerSelectionSubdialogHeight);

      string workerLabel = GetWorkerLabel();
      if (workerSection.ButtonText(workerLabel))
      {
        MakeWorkerOptionsFloatMenu();
      }

      listing.EndSection(workerSection);

      listing.End();
    }

    private void DoRightColumn(Rect rect)
    {
      // ===== Ingredient Filter Header =====
      Text.Font = GameFont.Small;
      Rect headerRect = new Rect(rect.x, rect.y, rect.width, 24f);
      Widgets.Label(headerRect, "Allowed Ingredients:");

      // Check All / Uncheck All buttons
      Rect btnRect = new Rect(rect.x, rect.y + 28f, rect.width, 24f);
      if (Widgets.ButtonText(btnRect.LeftHalf().ContractedBy(2f), "Check All"))
      {
        if (bill.processFilter != null && bill.processDef?.ingredientFilter != null)
        {
          foreach (var def in bill.processDef.ingredientFilter.AllowedThingDefs)
          {
            if (!bill.processFilter.allowedIngredients.Contains(def))
              bill.processFilter.allowedIngredients.Add(def);
          }
        }
        SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
      }
      if (Widgets.ButtonText(btnRect.RightHalf().ContractedBy(2f), "Uncheck All"))
      {
        bill.processFilter?.allowedIngredients?.Clear();
        SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
      }

      // ===== Scrollable ingredient list with icons =====
      float listStartY = rect.y + 56f;
      float sliderHeight = 60f;
      float listHeight = rect.height - 56f - sliderHeight - 10f;
      Rect listRect = new Rect(rect.x, listStartY, rect.width, listHeight);

      var allIngredients = bill.processDef?.ingredientFilter?.AllowedThingDefs?.ToList() ?? new List<ThingDef>();
      float rowHeight = 28f;
      Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, allIngredients.Count * rowHeight);

      Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

      float curY = 0f;
      foreach (ThingDef def in allIngredients)
      {
        Rect rowRect = new Rect(0f, curY, viewRect.width, rowHeight - 2f);

        // Draw icon
        Rect iconRect = new Rect(0f, curY + 2f, 22f, 22f);
        Widgets.ThingIcon(iconRect, def);

        // Draw checkbox with label (offset for icon)
        Rect checkRect = new Rect(26f, curY, viewRect.width - 26f, rowHeight - 2f);
        bool active = bill.processFilter?.allowedIngredients?.Contains(def) ?? false;
        bool newActive = active;

        Widgets.CheckboxLabeled(checkRect, def.LabelCap, ref newActive);

        if (newActive != active)
        {
          if (bill.processFilter != null)
          {
            if (newActive)
              bill.processFilter.allowedIngredients.Add(def);
            else
              bill.processFilter.allowedIngredients.Remove(def);
          }
        }

        curY += rowHeight;
      }

      Widgets.EndScrollView();

      // ===== Ingredient Search Radius Slider (at bottom) =====
      Rect sliderRect = new Rect(rect.x, rect.y + rect.height - sliderHeight, rect.width, sliderHeight);

      string radiusText = bill.ingredientSearchRadius >= 999 ? "Unlimited" : bill.ingredientSearchRadius.ToString();
      Widgets.Label(new Rect(sliderRect.x, sliderRect.y, sliderRect.width, 24f), "Search Radius: " + radiusText);

      float displayRadius = bill.ingredientSearchRadius >= 999 ? 100f : bill.ingredientSearchRadius;
      Rect sliderBarRect = new Rect(sliderRect.x, sliderRect.y + 26f, sliderRect.width, 22f);
      float newRadius = Widgets.HorizontalSlider(sliderBarRect, displayRadius, 3f, 100f);

      if (newRadius >= 100f)
      {
        bill.ingredientSearchRadius = 9999;
      }
      else
      {
        bill.ingredientSearchRadius = (int)newRadius;
      }
    }

    private string GetOutputDestinationLabel()
    {
      switch (bill.actionWithOutput)
      {
        case ActionWithOutput.DropOnFloor:
          return "Drop on floor";
        case ActionWithOutput.HaulToBestStockpile:
          return "Haul to best stockpile";
        case ActionWithOutput.HaulToSpecificStockpile:
          string stockpileName = bill.destinationStockpile?.label ?? "None";
          return "Haul to: " + stockpileName;
        default:
          return "Unknown";
      }
    }

    private void MakeStoreOptionsFloatMenu()
    {
      var options = new List<FloatMenuOption>
      {
        new FloatMenuOption("Drop on floor", delegate
        {
          bill.actionWithOutput = ActionWithOutput.DropOnFloor;
          bill.destinationStockpile = null;
        }),
        new FloatMenuOption("Haul to best stockpile", delegate
        {
          bill.actionWithOutput = ActionWithOutput.HaulToBestStockpile;
          bill.destinationStockpile = null;
        })
      };

      // Add specific stockpiles
      if (processor?.Map != null)
      {
        foreach (Zone_Stockpile stockpile in processor.Map.zoneManager.AllZones.OfType<Zone_Stockpile>())
        {
          Zone_Stockpile localStockpile = stockpile;
          options.Add(new FloatMenuOption("Haul to: " + stockpile.label, delegate
          {
            bill.actionWithOutput = ActionWithOutput.HaulToSpecificStockpile;
            bill.destinationStockpile = localStockpile;
          }));
        }
      }

      Find.WindowStack.Add(new FloatMenu(options));
    }

    private string GetWorkerLabel()
    {
      switch (bill.allowedWorker)
      {
        case AllowedWorker.Any:
          return "Any worker";
        case AllowedWorker.Slave:
          return "Slaves only";
        case AllowedWorker.Mech:
          return "Mechs only";
        case AllowedWorker.SpecificPawn:
          return bill.worker?.LabelShort ?? "Specific pawn";
        default:
          return "Unknown";
      }
    }

    private void MakeWorkerOptionsFloatMenu()
    {
      var options = new List<FloatMenuOption>
      {
        new FloatMenuOption("Any worker", delegate
        {
          bill.allowedWorker = AllowedWorker.Any;
          bill.worker = null;
        })
      };

      // Add slaves option if Ideology is active
      if (ModsConfig.IdeologyActive)
      {
        options.Add(new FloatMenuOption("Slaves only", delegate
        {
          bill.allowedWorker = AllowedWorker.Slave;
          bill.worker = null;
        }));
      }

      // Add mechs option if Biotech is active
      if (ModsConfig.BiotechActive)
      {
        options.Add(new FloatMenuOption("Mechs only", delegate
        {
          bill.allowedWorker = AllowedWorker.Mech;
          bill.worker = null;
        }));
      }

      // Add specific colonists
      if (processor?.Map != null)
      {
        foreach (Pawn pawn in processor.Map.mapPawns.FreeColonists)
        {
          Pawn localPawn = pawn;
          options.Add(new FloatMenuOption(pawn.LabelShort, delegate
          {
            bill.allowedWorker = AllowedWorker.SpecificPawn;
            bill.worker = localPawn;
          }));
        }
      }

      Find.WindowStack.Add(new FloatMenu(options));
    }
  }
}
