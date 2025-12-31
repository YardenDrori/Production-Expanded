using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProductionExpanded
{
  public class Building_Processor : Building
  {
    public List<ProcessBill> activeBills = new List<ProcessBill>();

    /// <summary>
    /// Gets the list of ALL ProcessDefs this building supports (based on XML usage).
    /// </summary>
    public List<ProcessDef> ProcessesStack
    {
      get
      {
        return DefDatabase<ProcessDef>.AllDefs.Where(p => p.recipeUsers.Contains(def)).ToList();
      }
    }

    /// <summary>
    /// Gets all ThingDefs that are currently allowed as input based on active bills.
    /// Used by WorkGivers to find what materials to haul.
    /// </summary>
    public HashSet<ThingDef> ValidIngredients
    {
      get
      {
        var result = new HashSet<ThingDef>();
        foreach (var bill in activeBills)
        {
          // Skip if bill is paused or suspended (if we add that later)
          if (bill.processFilter != null)
          {
            foreach (var ingredient in bill.processFilter.allowedIngredients)
            {
              result.Add(ingredient);
            }
          }
        }
        return result;
      }
    }

    /// <summary>
    /// Finds the ProcessBill that allows a specific ingredient.
    /// Returns null if no active bill accepts this ingredient.
    /// </summary>
    public ProcessBill GetBillForIngredient(ThingDef ingredientDef)
    {
      foreach (var bill in activeBills)
      {
        if (
          bill.processFilter != null
          && bill.processFilter.Allows(ingredientDef)
          && !bill.IsFulfilled()
        )
        {
          return bill;
        }
      }
      return null;
    }

    // ============ BILL MANAGEMENT METHODS ============

    /// <summary>
    /// Creates a new bill for the given process.
    /// Returns the created bill.
    /// </summary>
    public ProcessBill CreateBill(ProcessDef process)
    {
      var bill = new ProcessBill();
      bill.Parent = this;
      bill.processDef = process;
      bill.processFilter = new ProcessFilter(process);
      bill.processFilter.processDef = process; // Link to filter
      bill.isSuspended = false;
      bill.repeatMode = ProcessRepeatMode.DoXTimes;
      bill.x = 10;
      bill.ingredientSearchRadius = 9999;
      bill.label = process.label;
      bill.worker = null;
      bill.destinationStockpile = null;

      activeBills.Add(bill);
      return bill;
    }

    /// <summary>
    /// Deletes a specific bill.
    /// </summary>
    public void DeleteBill(ProcessBill bill)
    {
      activeBills.Remove(bill);
    }

    /// <summary>
    /// Clears all bills.
    /// </summary>
    public void ClearAllBills()
    {
      activeBills.Clear();
    }

    // ============ SAVE/LOAD ============

    public override void ExposeData()
    {
      base.ExposeData();

      // Save/load active bills
      Scribe_Collections.Look(ref activeBills, "activeBills", LookMode.Deep);

      // Ensure list is never null after load
      if (activeBills == null)
      {
        activeBills = new List<ProcessBill>();
      }

      // Re-link parent after load
      if (Scribe.mode == LoadSaveMode.PostLoadInit)
      {
        foreach (var bill in activeBills)
        {
          bill.Parent = this;
        }
      }
    }

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
