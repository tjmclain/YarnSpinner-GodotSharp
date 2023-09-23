using Godot;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Godot.HttpRequest;

namespace Yarn.GodotSharp.Views;

[GlobalClass]
public partial class OptionsListView : Control, IRunOptionsHandler
{
	protected Control _optionViewsParentNode = null;

	private CancellationTokenSource _optionViewsCancellationSource;

	protected readonly List<OptionView> _optionViewsPool = new();

	[Export]
	public virtual PackedScene OptionViewPrototype { get; set; }

	[Export]
	public virtual Control OptionViewsParentNode
	{
		get => _optionViewsParentNode ?? this;
		set => _optionViewsParentNode = value;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Clear pool and recycling any existing views
		_optionViewsPool.Clear();
		RecyleOptionViews();
	}

	public virtual async Task RunOptions(DialogueOption[] options, Action<int> selectOption)
	{
		if (options == null)
		{
			GD.PushError("options == null");
			return;
		}

		var tasks = new List<Task<int>>(options.Length);
		_optionViewsCancellationSource = new CancellationTokenSource();

		for (int i = 0; i < options.Length; i++)
		{
			var option = options[i];
			if (!TryGetOptionView(out var optionView))
			{
				continue;
			}

			optionView.SetOption(option, i);

			var task = Task.Run(async () =>
				{
					var awaiter = ToSignal(optionView, OptionView.SignalName.OptionSelected);
					await awaiter;
					var result = awaiter.GetResult();
					if (result == null || result.Length == 0)
					{
						GD.PushError("result == null || result.Length == 0");
						return -1;
					}
					return result[0].AsInt32();
				},
				_optionViewsCancellationSource.Token
			);

			tasks.Add(task);
		}

		var selected = await Task.WhenAny(tasks);
		int selectedIndex = selected.Result;
		GD.Print($"RunOptions: selectedIndex = {selectedIndex}");

		CancelOptionViewTasks();
		selectOption?.Invoke(selectedIndex);
	}

	public virtual async Task DismissOptions(DialogueOption[] options, int selectedOptionIndex)
	{
		CancelOptionViewTasks();
		RecyleOptionViews();
		await Task.CompletedTask;
	}

	protected virtual bool TryGetOptionView(out OptionView optionView)
	{
		if (_optionViewsPool.Any())
		{
			optionView = _optionViewsPool.First();
			optionView.Show();
			return true;
		}

		if (OptionViewPrototype != null)
		{
			optionView = OptionViewPrototype.InstantiateOrNull<OptionView>();
			if (optionView != null)
			{
				return true;
			}
		}

		GD.PushError($"Failed to create OptionView; OptionViewPrototype = {OptionViewPrototype}");
		optionView = null;
		return false;
	}

	protected virtual void RecyleOptionViews()
	{
		var optionViews = OptionViewsParentNode.GetChildren()
			.Where(x => typeof(OptionView).IsAssignableFrom(x.GetType()))
			.Select(x => x as OptionView);

		foreach (var optionView in optionViews)
		{
			if (optionView == null)
			{
				GD.PushError("optionView == null");
				continue;
			}

			optionView.Hide();
		}

		_optionViewsPool.AddRange(optionViews);
	}

	protected virtual void CancelOptionViewTasks()
	{
		if (_optionViewsCancellationSource == null)
		{
			return;
		}

		if (_optionViewsCancellationSource.IsCancellationRequested)
		{
			return;
		}

		_optionViewsCancellationSource.Cancel();
	}
}
