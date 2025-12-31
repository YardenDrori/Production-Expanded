using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  public class Dialog_ProcessIngredients : Window
  {
    private ProcessBill bill;
    private Vector2 scrollPosition;
    private List<ThingDef> allPossibleIngredients;

    public Dialog_ProcessIngredients(ProcessBill bill)
    {
      this.bill = bill;
      this.doCloseX = true;
      this.forcePause = true;
      this.absorbInputAroundWindow = true;
      this.closeOnClickedOutside = true;

      // Cache possible ingredients from the Def
      this.allPossibleIngredients = bill.processDef.ingredientFilter.AllowedThingDefs.ToList();
    }

    public override Vector2 InitialSize => new Vector2(400f, 600f);

    public override void DoWindowContents(Rect inRect)
    {
      Text.Font = GameFont.Medium;
      Widgets.Label(inRect.TopPartPixels(30f), "Ingredients");
      Text.Font = GameFont.Small;

      Rect btnRect = new Rect(0f, 35f, inRect.width, 30f);
      if (Widgets.ButtonText(btnRect.LeftPart(0.48f), "Check All"))
      {
        foreach (var def in allPossibleIngredients)
          bill.processFilter.allowedIngredients.Add(def);
      }
      if (Widgets.ButtonText(btnRect.RightPart(0.48f), "Uncheck All"))
      {
        bill.processFilter.allowedIngredients.Clear();
      }

      Rect listRect = new Rect(0f, 70f, inRect.width, inRect.height - 70f);
      Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, allPossibleIngredients.Count * 28f);

      Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

      float curY = 0f;
      foreach (ThingDef def in allPossibleIngredients)
      {
        Rect rowRect = new Rect(0f, curY, viewRect.width, 24f);

        bool active = bill.processFilter.allowedIngredients.Contains(def);
        bool newActive = active;

        Widgets.CheckboxLabeled(rowRect, def.LabelCap, ref newActive);

        if (newActive != active)
        {
          if (newActive)
            bill.processFilter.allowedIngredients.Add(def);
          else
            bill.processFilter.allowedIngredients.Remove(def);
        }

        curY += 28f;
      }

      Widgets.EndScrollView();
    }
  }
}
