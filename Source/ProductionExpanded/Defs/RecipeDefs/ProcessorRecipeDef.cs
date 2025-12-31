using Verse;

namespace ProductionExpanded
{
    public class ProcessorRecipeDef : RecipeDef
    {
        public int ticksPerItem = 100;
        public int cycles = 1;
        public float ratio = 1f;

        public ThingDef inputType = null;
        public ThingDef outputType = null;
    }
}
