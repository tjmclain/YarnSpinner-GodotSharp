using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using Yarn.GodotSharp.Views.Effects;

namespace Yarn.GodotSharp.Views;

[GlobalClass]
public partial class LineView : AsyncViewControl, IRunLineHandler
{
	private Control _characterNameContainer;

	[Export]
	public RichTextLabel LineText { get; set; } = null;

	[Export]
	public RichTextLabel CharacterNameText { get; set; } = null;

	[Export]
	public Control CharacterNameContainer
	{
		get => _characterNameContainer ?? CharacterNameText;
		set => _characterNameContainer = value;
	}

	[Export]
	public TextEffect TextAnimationEffect { get; set; } = null;

	[Export]
	public BaseButton ContinueButton { get; set; } = null;

	[Export]
	public StringName ContinueInputAction { get; set; } = string.Empty;

	#region Godot.Node

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= ContinueDialogue;
			ContinueButton.Pressed += ContinueDialogue;

			ContinueButton.Hide();
		}

		Hide();
	}

	public override void _ExitTree()
	{
		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= ContinueDialogue;
		}
	}

	public override void _Input(InputEvent evt)
	{
		if (!Visible)
		{
			return;
		}

		if (string.IsNullOrEmpty(ContinueInputAction))
		{
			return;
		}

		if (!evt.IsActionPressed(ContinueInputAction))
		{
			return;
		}

		ContinueDialogue();
	}

	#endregion Godot.Node

	#region IRunLineHandler

	public virtual async Task RunLine(
		LocalizedLine line,
		Action interruptLine,
		CancellationToken externalToken
	)
	{
		GD.Print($"LineView.RunLine: Name = {Name}");

		if (externalToken.IsCancellationRequested)
		{
			return;
		}

		if (LineText == null)
		{
			GD.PushError("LineView.RunLine: LineText == null");
			return;
		}

		CallDeferred(CanvasItem.MethodName.Show);
		ContinueButton?.CallDeferred(CanvasItem.MethodName.Show);

		// test if this is necessary. Do I crash if I set a UI value from another thread?
		LineText.SetDeferred(RichTextLabel.PropertyName.Text, line.TextWithoutCharacterName);
		LineText.SetDeferred(RichTextLabel.PropertyName.VisibleCharacters, -1);

		CallDeferred(MethodName.SetCharacterName, line);

		if (TextAnimationEffect != null)
		{
			using (var cts = CreateLinkedTokenSource(externalToken))
			{
				GD.Print("LineView.RunLine: TextAnimationEffect.Animate");
				try
				{
					await TextAnimationEffect.Animate(LineText, cts.Token);
				}
				catch (OperationCanceledException)
				{
					GD.Print("LineView.RunLine: TextAnimationEffect.CancelAnimation");
					TextAnimationEffect.CallDeferred(TextEffect.MethodName.CancelAnimation, LineText);
				}
			}
		}

		using (var cts = CreateLinkedTokenSource(externalToken))
		{
			GD.Print("LineView.RunLine: WaitForCancellation");
			try
			{
				await WaitForCancellation(cts.Token);
			}
			catch (OperationCanceledException)
			{
			}
		}

		ContinueButton?.CallDeferred(CanvasItem.MethodName.Hide);

		interruptLine?.Invoke();
	}

	public virtual async Task DismissLine(LocalizedLine line)
	{
		GD.Print("LineView.DismissLine");

		SafeDisposeInternalTokenSource();

		ContinueButton?.CallDeferred(CanvasItem.MethodName.Hide);
		CallDeferred(CanvasItem.MethodName.Hide);

		await Task.CompletedTask;
	}

	#endregion IRunLineHandler

	public virtual void SetCharacterName(LocalizedLine line)
	{
		if (CharacterNameContainer != null)
		{
			CharacterNameContainer.Visible = !string.IsNullOrEmpty(line?.CharacterName);
		}

		if (CharacterNameText != null)
		{
			CharacterNameText.Text = line?.CharacterName;
		}
	}

	public virtual void ContinueDialogue()
	{
		SafeCancelInternalTokenSource();
	}
}