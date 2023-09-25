using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using Yarn.GodotSharp.Views.Effects;

namespace Yarn.GodotSharp.Views;

[GlobalClass]
public partial class LineView : AsyncViewControl, IRunLineHandler
{
	[Export]
	public RichTextLabel LineText { get; set; } = null;

	[Export]
	public RichTextLabel CharacterNameText { get; set; } = null;

	[Export]
	public Control CharacterNameContainer { get; set; } = null;

	[Export]
	public TextEffect TextAnimationEffect { get; set; } = null;

	[Export]
	public BaseButton ContinueButton { get; set; } = null;

	[Export]
	public StringName ContinueInputAction { get; set; } = string.Empty;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= ContinueDialogue;
			ContinueButton.Pressed += ContinueDialogue;

			ContinueButton.Hide();
		}

		SafeDisposeInternalTokenSource();
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

	public virtual async Task RunLine(
		LocalizedLine line,
		Action interruptLine,
		CancellationToken externalToken
	)
	{
		GD.Print("lvrl LineView.RunLine");

		if (externalToken.IsCancellationRequested)
		{
			//externalToken.ThrowIfCancellationRequested();
			return;
		}

		if (LineText == null)
		{
			GD.PushError("lvrl LineText == null");
			return;
		}

		CallDeferred(CanvasItem.MethodName.Show);
		ContinueButton?.CallDeferred(CanvasItem.MethodName.Show);

		// test if this is necessary. Do I crash if I set a UI value from another thread?
		LineText.SetDeferred(RichTextLabel.PropertyName.Text, line.TextWithoutCharacterName);
		LineText.VisibleCharacters = -1;

		SetCharacterName(line.CharacterName);

		if (TextAnimationEffect != null)
		{
			SafeDisposeInternalTokenSource();
			using (var cts = CreateLinkedTokenSource(externalToken))
			{
				GD.Print("TextAnimationEffect.Animate: Begin");

				try
				{
					await TextAnimationEffect.Animate(LineText, cts.Token);
				}
				catch (OperationCanceledException)
				{
					GD.Print("Skipped TextAnimationEffect.Animate");
				}
				GD.Print("TextAnimationEffect.Animate: Finish");
			}
		}

		SafeDisposeInternalTokenSource();
		using (var cts = CreateLinkedTokenSource(externalToken))
		{
			GD.Print("await CancellationTokenAwaiter: Begin");
			var awaiter = new CancellationTokenAwaiter(cts.Token);

			try
			{
				await awaiter;
			}
			catch (OperationCanceledException)
			{
			}

			GD.Print("await CancellationTokenAwaiter: Finish");
		}

		ContinueButton?.Hide();

		SafeDisposeInternalTokenSource();

		// request an interruption if no one else has yet
		if (!externalToken.IsCancellationRequested)
		{
			GD.Print("interruptLine.Invoke");
			interruptLine?.Invoke();
		}
	}

	public virtual void SetCharacterName(string characterName)
	{
		if (string.IsNullOrEmpty(characterName))
		{
			CharacterNameContainer?.CallDeferred(CanvasItem.MethodName.Hide);
		}
		else
		{
			CharacterNameContainer?.CallDeferred(CanvasItem.MethodName.Show);
		}

		CharacterNameText?.SetDeferred(RichTextLabel.PropertyName.Text, characterName);
	}

	public virtual async Task DismissLine(LocalizedLine line)
	{
		GD.Print("LineView.DismissLine");
		SafeDisposeInternalTokenSource();
		ContinueButton?.CallDeferred(CanvasItem.MethodName.Hide);
		CallDeferred(CanvasItem.MethodName.Hide);
		await Task.CompletedTask;
	}

	public virtual void ContinueDialogue()
	{
		GD.Print("LineView.ContinueDialogue");
		SafeCancelInternalTokenSource();
	}
}
