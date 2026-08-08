using System.Linq;
using LudeonTK;
using Verse;

namespace ProductionExpanded
{
  /// <summary>
  /// Dev-mode tools for tracking down why two stacks of the same item render differently.
  /// </summary>
  public static class DebugActions_Inspect
  {
    [DebugAction(
      "Production Expanded",
      "Dump draw info for cell",
      allowedGameStates = AllowedGameStates.PlayingOnMap,
      actionType = DebugActionType.ToolMap
    )]
    private static void DumpDrawInfo()
    {
      Map map = Find.CurrentMap;
      if (map == null)
      {
        return;
      }

      IntVec3 cell = UI.MouseCell();
      foreach (Thing thing in cell.GetThingList(map).ToList())
      {
        string comps =
          thing is ThingWithComps twc && !twc.AllComps.NullOrEmpty()
            ? string.Join(", ", twc.AllComps.Select(c => c.GetType().Name))
            : "none";

        Log.Message(
          $"[Production Expanded] {thing.def.defName} x{thing.stackCount} ({thing.LabelCap})\n"
            + $"  stuff:       {thing.Stuff?.defName ?? "none"}\n"
            + $"  style:       {thing.StyleDef?.defName ?? "none"}\n"
            + $"  DrawColor:   {thing.DrawColor} / two {thing.DrawColorTwo}\n"
            + $"  def color:   {thing.def.graphicData?.color.ToString() ?? "none"}\n"
            + $"  texPath:     {thing.def.graphicData?.texPath ?? "none"}\n"
            + $"  graphic:     {thing.Graphic}\n"
            + $"  material:    {thing.Graphic?.MatSingle?.color.ToString() ?? "none"}\n"
            + $"  comps:       {comps}"
        );
      }
    }
  }
}
