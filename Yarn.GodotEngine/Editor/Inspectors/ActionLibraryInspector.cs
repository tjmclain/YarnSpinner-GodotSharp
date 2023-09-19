#if TOOLS

using Godot;
using Yarn.GodotEngine.Actions;

namespace Yarn.GodotEngine.Editor.Inspectors
{
	[Tool]
	public partial class ActionLibraryInspector : EditorInspectorPlugin
	{
		// private Button _refreshButton;

		#region Public Methods

		public override bool _CanHandle(GodotObject obj)
		{
			return typeof(ActionLibrary).IsAssignableFrom(obj.GetType());
		}

		// TODO: this breaks; the button gets disposed, and then that logs an error in output
		// 'modules/mono/glue/runtime_interop.cpp:1324 - System.ObjectDisposedException: Cannot
		// access a disposed object. Object name: 'Godot.Button'.'
		public override void _ParseEnd(GodotObject obj)
		{
			//_refreshButton = new Button();

			//// Add the control as a direct child of EditorProperty node.
			//AddCustomControl(_refreshButton);

			//// Setup the initial state and connect to the signal to track changes.
			//_refreshButton.Text = "Refresh";
			//_refreshButton.Pressed += () => OnRefreshButtonPressed();
		}

		public override bool _ParseProperty(
			GodotObject obj,
			Variant.Type type,
			string name,
			PropertyHint hintType,
			string hintString,
			PropertyUsageFlags usageFlags,
			bool wide
		)
		{
			// Don't handle any properties for now
			return false;
		}

		#endregion Public Methods

		//private static void OnRefreshButtonPressed()
		//{
		//	GD.Print("OnRefreshButtonPressed");
		//	var dummyProperty = new EditorProperty();
		//	var editedObject = dummyProperty.GetEditedObject();
		//	if (editedObject is not ActionLibrary actionLibrary)
		//	{
		//		GD.PrintErr("GetEditedObject() is not ActionLibrary");
		//		return;
		//	}

		//	actionLibrary.Refresh();
		//}
	}
}

#endif
