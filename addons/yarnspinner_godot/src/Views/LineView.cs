using Godot;
using System;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views;

[GlobalClass]
public partial class LineView : Control, IRunLineHandler
{
	[Export]
	public RichTextLabel CharacterNameText { get; set; }

	[Export]
	public RichTextLabel LineText { get; set; }

	[Export]
	public Button ContinueButton { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= AdvanceDialogue;
			ContinueButton.Pressed += AdvanceDialogue;
		}
	}

	public override void _ExitTree()
	{
		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= AdvanceDialogue;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public Task RunLine(LocalizedLine line, Action interruptLine)
	{
		throw new NotImplementedException();
	}

	public Task InterruptLine(LocalizedLine line)
	{
		throw new NotImplementedException();
	}

	public Task DismissLine(LocalizedLine line)
	{
		throw new NotImplementedException();
	}

	public void AdvanceDialogue()
	{
	}
}
