using RimWorld;
using Verse;

namespace ProductionExpanded
{
    // ═══════════════════════════════════════════════════════════════════════════
    // STEP 1: MINIMAL WORKING COMP
    // ═══════════════════════════════════════════════════════════════════════════
    //
    // This is your first working RimWorld component!
    //
    // ARCHITECTURE:
    // - CompProperties_ResourceProcessor = Configuration (from XML)
    // - CompResourceProcessor = Runtime logic (this class)
    //
    // Think of CompProperties as a "struct" that holds settings, and the Comp
    // as the "instance" with state and behavior.
    //
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Configuration class for CompResourceProcessor.
    /// RimWorld reads this from XML and passes it to the Comp.
    /// </summary>
    public class CompProperties_ResourceProcessor : CompProperties
    {
        // How many units of material this building can hold
        public int maxCapacity = 50;

        // CONSTRUCTOR IS REQUIRED!
        // This tells RimWorld which Comp class to create
        public CompProperties_ResourceProcessor()
        {
            this.compClass = typeof(CompResourceProcessor);
        }
    }

    /// <summary>
    /// The actual component that handles passive resource processing.
    /// Attached to buildings via XML.
    /// </summary>
    public class CompResourceProcessor : ThingComp
    {
        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES - Access Configuration
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Access our typed properties (XML configuration).
        /// Props is a base class field that RimWorld populates.
        /// We cast it to our specific type for easy access.
        /// </summary>
        private CompProperties_ResourceProcessor Props =>
            (CompProperties_ResourceProcessor)props;

        // ═══════════════════════════════════════════════════════════════════════
        // LIFECYCLE METHODS - RimWorld calls these automatically
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Called when the building spawns in the world.
        /// This is like a constructor, but happens AFTER the building is placed.
        ///
        /// @param respawningAfterLoad - True if loading from save, false if newly built
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // Log a message so we know it worked!
            // This will appear in the dev console (Ctrl+F12 in dev mode)
            Log.Message($"[Production Expanded] Resource Processor spawned! Max capacity: {Props.maxCapacity}");
        }

        /// <summary>
        /// Called every ~4 seconds (250 ticks).
        /// This is where your main update logic will go.
        ///
        /// WHY CompTickRare instead of CompTick?
        /// - CompTick = 60 times per second (expensive!)
        /// - CompTickRare = once per 250 ticks (~4 seconds) - much more efficient!
        ///
        /// For passive conversion, we don't need to check things 60 times per second.
        /// Every few seconds is plenty.
        /// </summary>
        public override void CompTickRare()
        {
            base.CompTickRare();

            // Your processing logic will go here in Step 2!
            // For now, this just exists to prove the comp is ticking.
        }

        /// <summary>
        /// Called when player inspects the building.
        /// Return value is displayed in the info panel.
        ///
        /// This is like a "toString()" method for the UI.
        /// </summary>
        public override string CompInspectStringExtra()
        {
            // For now, just show that we're idle
            // In Step 2, we'll show progress percentage here
            return "Processor Status: Idle";
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SAVE/LOAD - This will be needed in Step 2
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Called when saving/loading the game.
        ///
        /// CRITICAL: Any state that needs to persist across saves must be
        /// serialized here using Scribe_* methods.
        ///
        /// Think of this like writing to/reading from a file, but RimWorld handles
        /// the actual file I/O for you.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            // In Step 2, we'll save/load our state here
            // For now, nothing to save (we have no state)
        }
    }
}
