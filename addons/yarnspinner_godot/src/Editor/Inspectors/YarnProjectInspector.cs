using Godot;
using System;

namespace Yarn.GodotSharp.Editor.Inspectors;

public partial class YarnProjectInspector : EditorInspectorPlugin
{
	public override bool _CanHandle(GodotObject obj)
	{
		return typeof(YarnProject).IsAssignableFrom(obj.GetType());
	}

	public override void _ParseBegin(GodotObject @object)
	{
		base._ParseBegin(@object);
	}

	public override void _ParseEnd(GodotObject @object)
	{
		base._ParseEnd(@object);
	}
}