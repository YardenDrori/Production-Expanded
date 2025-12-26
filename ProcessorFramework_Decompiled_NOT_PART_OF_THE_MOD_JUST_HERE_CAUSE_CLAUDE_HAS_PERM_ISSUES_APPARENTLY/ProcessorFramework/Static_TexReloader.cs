using System.Reflection;
using Verse;

namespace ProcessorFramework;

public static class Static_TexReloader
{
	public static void Reload(Thing t, string texPath)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Graphic value = GraphicDatabase.Get(t.def.graphicData.graphicClass, texPath, ShaderDatabase.LoadShader(t.def.graphicData.shaderType.shaderPath), t.def.graphicData.drawSize, t.DrawColor, t.DrawColorTwo, (string)null);
		typeof(Thing).GetField("graphicInt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(t, value);
		if (t.Map != null)
		{
			t.Map.mapDrawer.MapMeshDirty(t.Position, 1uL);
		}
	}
}
