using RimWorld;
using Verse;

namespace ProductionExpanded
{
  // CompProperties class is required to tell RimWorld which Comp class to instantiate
  public class CompProperties_Glower_Processor : CompProperties_Glower
  {
    public CompProperties_Glower_Processor()
    {
      this.compClass = typeof(CompGlower_Processor);
    }
  }

  public class CompGlower_Processor : CompGlower
  {
    protected override bool ShouldBeLitNow
    {
      get
      {
        // If building is destroyed/despawned, light should always be off
        if (!parent.Spawned)
        {
          return false;
        }

        CompResourceProcessor processor = parent.GetComp<CompResourceProcessor>();
        if (processor == null)
        {
          Log.Warning($"[CompGlower_Processor] No processor comp on {parent.def.defName}");
          return false;
        }

        bool isProcessing = processor.getIsProcessing();
        bool canContinue = processor.getIsReady();
        bool isFinished = processor.getIsFinished();
        bool isWaiting = processor.getIsWaitingForNextCycle();
        bool result = isProcessing && canContinue && !isFinished && !isWaiting;

        return result;
      }
    }
  }
}
