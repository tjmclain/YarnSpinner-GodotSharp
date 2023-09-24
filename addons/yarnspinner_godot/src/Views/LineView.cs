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
		if (externalToken.IsCancellationRequested)
		{
			externalToken.ThrowIfCancellationRequested();
			return;
		}

		if (LineText == null)
		{
			GD.PushError("LineText == null");
			return;
		}

		Show();
		ContinueButton?.Show();

		SetCharacterName(line.CharacterName);

		LineText.Text = line.TextWithoutCharacterName;
		LineText.VisibleCharacters = -1;

		if (TextAnimationEffect != null)
		{
			SafeDisposeInternalTokenSource();
			using (var cts = CreateLinkedTokenSource(externalToken))
			{
				await TextAnimationEffect.Animate(LineText, cts.Token);

				if (cts.IsCancellationRequested)
				{
					SafeDisposeInternalTokenSource();
					cts.Token.ThrowIfCancellationRequested();
					return;
				}
			}
		}

		SafeDisposeInternalTokenSource();
		using (var cts = CreateLinkedTokenSource(externalToken))
		{
			var awaiter = new CancellationTokenAwaiter(cts.Token);
			await awaiter;
		}

		ContinueButton?.Hide();

		SafeDisposeInternalTokenSource();

		// request an interruption if no one else has yet
		if (!externalToken.IsCancellationRequested)
		{
			interruptLine?.Invoke();
		}
	}

	public virtual void SetCharacterName(string characterName)
	{
		if (string.IsNullOrEmpty(characterName))
		{
			CharacterNameContainer?.Hide();
		}
		else
		{
			CharacterNameContainer?.Show();
		}

		if (CharacterNameText != null)
		{
			CharacterNameText.Text = characterName;
		}
	}

	public virtual async Task DismissLine(LocalizedLine line)
	{
		SafeDisposeInternalTokenSource();
		ContinueButton?.Hide();
		Hide();
		await Task.CompletedTask;
	}

	public virtual void ContinueDialogue()
	{
		SafeCancelInternalTokenSource();
	}
}
