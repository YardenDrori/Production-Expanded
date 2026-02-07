using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProductionExpanded
{
  public class WorkGiver_FillProcessor : WorkGiver_Scanner
  {
    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
      var tracker = pawn.Map?.GetComponent<MapComponent_ProcessorTracker>();
      if (tracker != null)
      {
        return tracker.processorsNeedingFill;
      }
      return null;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      if (!(t is Building_Processor processor))
        return false;

      CompResourceProcessor comp = processor.GetComp<CompResourceProcessor>();
      if (comp == null || !comp.getIsReady() || comp.getCapacityRemaining() <= 0)
        return false;

      if (t.IsForbidden(pawn))
        return false;
      if (!pawn.CanReserve(t, 1, -1, null, forced))
        return false;

      if (comp.getIsProcessing())
      {
        Bill_Production activeBill = comp.GetActiveBill();
        if (activeBill == null) return false;

        var settings = activeBill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings == null) return false;

        // STATIC recipes lock once processing starts
        if (settings.isStaticRecipe) return false;

        // RATIO recipes can add more while processing
        if (comp.getCapacityRemaining() <= 0) return false;

        // Only accept matching input
        return FindIngredient(pawn, processor, comp.getInputItem()) != null;
      }

      // Check bills
      foreach (Bill bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        if (bill.recipe.fixedIngredientFilter == null)
          continue;

        var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings == null) continue;

        // Pre-check for STATIC recipes: verify ALL ingredients available
        if (settings.isStaticRecipe && settings.ingredients != null)
        {
          bool allAvailable = true;
          foreach (var procIng in settings.ingredients)
          {
            bool foundAny = false;

            if (procIng.thingDefs != null)
            {
              foreach (var def in procIng.thingDefs)
              {
                if (bill.ingredientFilter.Allows(def) &&
                    pawn.Map.listerThings.ThingsOfDef(def).Any(thing => !thing.IsForbidden(pawn)))
                {
                  foundAny = true;
                  break;
                }
              }
            }

            if (!foundAny && procIng.categoryDefs != null)
            {
              foundAny = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver).Any(thing =>
                !thing.IsForbidden(pawn)
                && procIng.categoryDefs.Any(cat => cat.ContainedInThisOrDescendant(thing.def))
                && bill.ingredientFilter.Allows(thing)
              );
            }

            if (!foundAny)
            {
              allAvailable = false;
              break;
            }
          }

          if (!allAvailable)
            continue; // Skip this bill, missing ingredients
        }

        if (FindIngredientForBill(pawn, processor, (Bill_Production)bill) != null)
          return true;
      }
      return false;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
      if (!(t is Building_Processor processor))
        return null;

      CompResourceProcessor comp = processor.GetComp<CompResourceProcessor>();
      if (comp == null || !comp.getIsReady())
        return null;

      if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        return null;

      // If already processing - only RATIO recipes can add more ingredients
      if (comp.getIsProcessing())
      {
        Bill_Production activeBill = comp.GetActiveBill();
        if (activeBill == null) return null;

        var settings = activeBill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings == null) return null;

        // STATIC recipes lock once processing starts
        if (settings.isStaticRecipe) return null;

        // RATIO recipes can add more while processing
        if (comp.getCapacityRemaining() <= 0) return null;

        Thing ingredient = FindMatchingIngredient(pawn, processor, activeBill, comp);
        if (ingredient != null)
        {
          float capacityFactor = settings.capacityFactor;
          int maxItemsThatFit = Mathf.Max(1, (int)(comp.getCapacityRemaining() / capacityFactor));

          Job job = JobMaker.MakeJob(JobDefOf_ProductionExpanded.PE_FillProcessor, t, ingredient);
          job.count = Mathf.Min(ingredient.stackCount, maxItemsThatFit);
          job.bill = activeBill;
          return job;
        }
        return null;
      }

      // Not processing yet - check bills
      foreach (Bill_Production bill in processor.BillStack)
      {
        if (!bill.ShouldDoNow())
          continue;

        var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
        if (settings == null) continue;

        // Pre-check for STATIC recipes: verify ALL ingredients available
        if (settings.isStaticRecipe && settings.ingredients != null)
        {
          bool allAvailable = true;
          foreach (var procIng in settings.ingredients)
          {
            bool foundAny = false;

            if (procIng.thingDefs != null)
            {
              foreach (var def in procIng.thingDefs)
              {
                if (bill.ingredientFilter.Allows(def) &&
                    pawn.Map.listerThings.ThingsOfDef(def).Any(thing => !thing.IsForbidden(pawn)))
                {
                  foundAny = true;
                  break;
                }
              }
            }

            if (!foundAny && procIng.categoryDefs != null)
            {
              foundAny = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver).Any(thing =>
                !thing.IsForbidden(pawn)
                && procIng.categoryDefs.Any(cat => cat.ContainedInThisOrDescendant(thing.def))
                && bill.ingredientFilter.Allows(thing)
              );
            }

            if (!foundAny)
            {
              allAvailable = false;
              break;
            }
          }

          if (!allAvailable)
            continue; // Skip this bill, missing ingredients
        }

        // Find ingredient for this bill
        Thing ingredient = FindMatchingIngredient(pawn, processor, bill, comp);
        if (ingredient != null)
        {
          float capacityFactor = settings.capacityFactor;

          int maxItemsThatFit = settings.isStaticRecipe
            ? int.MaxValue  // No capacity limit for static
            : Mathf.Max(1, (int)(comp.getCapacityRemaining() / capacityFactor));

          Job job = JobMaker.MakeJob(JobDefOf_ProductionExpanded.PE_FillProcessor, t, ingredient);
          job.count = Mathf.Min(ingredient.stackCount, maxItemsThatFit);
          job.bill = bill;
          return job;
        }
      }

      return null;
    }

    private Thing FindIngredient(Pawn pawn, Building_Processor processor, ThingDef def)
    {
      return GenClosest.ClosestThingReachable(
        pawn.Position,
        pawn.Map,
        ThingRequest.ForDef(def),
        PathEndMode.ClosestTouch,
        TraverseParms.For(pawn),
        9999f,
        (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x)
      );
    }

    private Thing FindIngredientForBill(
      Pawn pawn,
      Building_Processor processor,
      Bill_Production bill
    )
    {
      return GenClosest.ClosestThingReachable(
        pawn.Position,
        pawn.Map,
        ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
        PathEndMode.ClosestTouch,
        TraverseParms.For(pawn),
        bill.ingredientSearchRadius,
        (Thing x) =>
          !x.IsForbidden(pawn)
          && pawn.CanReserve(x)
          && bill.recipe.fixedIngredientFilter.Allows(x)
          && bill.ingredientFilter.Allows(x)
      );
    }

    private Thing FindMatchingIngredient(Pawn pawn, Building_Processor processor, Bill_Production bill, CompResourceProcessor comp)
    {
      var settings = bill.recipe.GetModExtension<RecipeExtension_Processor>();
      if (settings == null) return null;

      bool isStatic = settings.isStaticRecipe;

      if (!isStatic)
      {
        // RATIO recipe - find the single input type
        ThingDef requiredDef = comp.getInputItem();
        if (requiredDef == null) return null;

        return GenClosest.ClosestThingReachable(
          pawn.Position, pawn.Map,
          ThingRequest.ForDef(requiredDef),
          PathEndMode.ClosestTouch,
          TraverseParms.For(pawn),
          bill.ingredientSearchRadius,
          (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x) && bill.ingredientFilter.Allows(x)
        );
      }

      // STATIC recipe - find next needed ingredient
      for (int i = 0; i < settings.ingredients.Count; i++)
      {
        int needed = comp.GetIngredientNeeded(i);
        int received = comp.GetIngredientReceived(i);

        if (received >= needed) continue; // This slot satisfied

        var procIng = settings.ingredients[i];

        // Search for ANY valid option (first match)
        if (procIng.thingDefs != null)
        {
          foreach (var thingDef in procIng.thingDefs)
          {
            Thing thing = GenClosest.ClosestThingReachable(
              pawn.Position, pawn.Map,
              ThingRequest.ForDef(thingDef),
              PathEndMode.ClosestTouch,
              TraverseParms.For(pawn),
              bill.ingredientSearchRadius,
              (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x) && bill.ingredientFilter.Allows(x)
            );
            if (thing != null) return thing;
          }
        }

        // Try categories if thingDefs didn't match
        if (procIng.categoryDefs != null)
        {
          Thing thing = GenClosest.ClosestThingReachable(
            pawn.Position, pawn.Map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            bill.ingredientSearchRadius,
            (Thing x) => !x.IsForbidden(pawn)
              && pawn.CanReserve(x)
              && bill.ingredientFilter.Allows(x)
              && procIng.categoryDefs.Any(cat => cat.ContainedInThisOrDescendant(x.def))
          );
          if (thing != null) return thing;
        }
      }

      return null; // All ingredients satisfied or none available
    }
  }
}
