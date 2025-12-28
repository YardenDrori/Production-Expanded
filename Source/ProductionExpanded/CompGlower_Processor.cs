using Verse;

namespace ProductionExpanded
{
    public class CompGlower_Processor : CompGlower
    {
        protected override bool ShouldBeLitNow
        {
            get
            {
                CompResourceProcessor processor = parent.GetComp<CompResourceProcessor>();
                if (processor == null)
                {
                    Log.Warning($"[CompGlower_Processor] No processor comp on {parent.def.defName}");
                    return false;
                }

                bool isProcessing = processor.getIsProcessing();
                bool canContinue = processor.CanContinueProcessing();
                bool isFinished = processor.getIsFinished();
                bool isWaiting = processor.getIsWaitingForNextCycle();
                bool result = isProcessing && canContinue && !isFinished && !isWaiting;

                // Log occasionally for debugging
                if (Find.TickManager.TicksGame % 250 == 0)
                {
                    Log.Message($"[Glower] {parent.def.defName}: proc={isProcessing}, can={canContinue}, fin={isFinished}, wait={isWaiting} -> lit={result}");
                }

                return result;
            }
        }
    }
}
