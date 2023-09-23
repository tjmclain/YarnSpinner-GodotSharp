using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Godot;
using GodotNode = Godot.Node;

namespace Yarn.GodotSharp;

using GodotDictionary = Godot.Collections.Dictionary;

public static class GodotUtility
{
	#region Classes

	public class PropertyInfo : Godot.Collections.Dictionary<string, Variant>
	{
		#region Fields

		public const string NameKey = "name";
		public const string TypeKey = "type";
		public const string HintKey = "hint";
		public const string HintStringKey = "hint_string";
		public const string DefaultValueKey = "default_value";

		#endregion Fields

		#region Properties

		public string Name
		{
			get => TryGetValue(NameKey, out var value) ? value.AsString() : string.Empty;
			set => this[NameKey] = Variant.From(value);
		}

		public Variant.Type Type
		{
			get => TryGetValue(TypeKey, out var value) ? value.VariantType : Variant.Type.Nil;
			set => this[TypeKey] = Variant.From(value);
		}

		public PropertyHint Hint
		{
			get => TryGetValue(HintKey, out var value) ? value.As<PropertyHint>() : PropertyHint.None;
			set => this[HintKey] = Variant.From(value);
		}

		public string HintString
		{
			get => TryGetValue(HintStringKey, out var value) ? value.AsString() : string.Empty;
			set => this[HintStringKey] = Variant.From(value);
		}

		public Variant DefaultValue
		{
			get => TryGetValue(DefaultValueKey, out var value) ? value.AsString() : null;
			set => this[DefaultValueKey] = Variant.From(value);
		}

		#endregion Properties

		public bool AddToProjectSettings()
		{
			string name = Name;
			if (string.IsNullOrEmpty(name))
			{
				//GD.PushError("string.IsNullOrEmpty(Name)");
				return false;
			}

			var value = ProjectSettings.GetSetting(name);
			if (value.VariantType == Variant.Type.Nil)
			{
				var defaultValue = DefaultValue;
				if (defaultValue.VariantType == Variant.Type.Nil)
				{
					//GD.PushError("defaultValue.VariantType == Variant.Type.Nil");
					return false;
				}
				ProjectSettings.SetSetting(name, defaultValue);
			}

			//GD.Print($"Add property info: {name}");
			//ProjectSettings.AddPropertyInfo((GodotDictionary)this);
			return true;
		}

		public bool RemoveFromProjectSettings()
		{
			string name = Name;
			if (string.IsNullOrEmpty(name))
			{
				//GD.PushError("string.IsNullOrEmpty(Name)");
				return false;
			}

			//ProjectSettings.SetSetting(name, Variant.From(Variant.Type.Nil));
			return true;
		}
	}

	public static class TranslationsProjectSetting
	{
		#region Fields

		public const string PropertyName = "internationalization/locale/translations";

		#endregion Fields

		#region Public Methods

		public static string[] Get()
		{
			return Array.Empty<string>();
			//var translationsSetting = ProjectSettings.GetSetting(PropertyName, Array.Empty<string>());
			//return translationsSetting.AsStringArray();
		}

		public static void Set(IEnumerable<string> files)
		{
			//ProjectSettings.SetSetting(PropertyName, files.ToArray());
			//ProjectSettings.Save();
		}

		public static void Add(string file)
		{
			var translations = new HashSet<string>(Get());
			if (translations.Add(file))
			{
				Set(translations);
			}
		}

		public static void Remove(string file)
		{
			var translations = new List<string>(Get());
			if (translations.Remove(file))
			{
				Set(translations);
			}
		}

		#endregion Public Methods
	}

	#endregion Classes

	#region Public Methods

	public static SceneTree GetSceneTree()
	{
		if (Engine.GetMainLoop() is not SceneTree sceneTree)
		{
			throw new ArgumentException("Engine.GetMainLoop() is not SceneTree sceneTree");
		}

		return sceneTree;
	}

	public static GodotNode GetCurrentScene()
	{
		var sceneTree = GetSceneTree();
		var currentScene = sceneTree.CurrentScene;
		if (currentScene == null)
		{
			GD.PrintErr("currentScene == null");
			return null;
		}

		return currentScene;
	}

	public static GodotNode GetNode(NodePath path)
	{
		var currentScene = GetCurrentScene();
		return currentScene?.GetNode(path);
	}

	public static T GetNode<T>(NodePath path) where T : GodotNode
	{
		var currentScene = GetCurrentScene();
		return currentScene?.GetNode<T>(path);
	}

	public static GodotNode GetChild(GodotNode node, Type type)
	{
		if (node == null)
		{
			GD.PrintErr("node == null");
			return null;
		}

		int count = node.GetChildCount();
		for (int i = 0; i < count; i++)
		{
			var child = node.GetChild(i);
			if (type.IsAssignableFrom(child.GetType()))
			{
				return child;
			}
		}

		return null;
	}

	// NOTE (2023-09-18): ProjectSettings.LocalizePath was not working for me. I would pass in a
	// valid global path to an asset under the project directory, but I would get the global path
	// back, not a 'res://' relative path. So, for now, I have to do the replacement manually.
	public static string LocalizePath(string globalPath)
	{
		GD.Print("LocalizePath: " + globalPath);
		string projectRoot = ProjectSettings.GlobalizePath("res://");
		return globalPath
			.Replace('\\', '/')
			.Replace(projectRoot, "res://");
	}

	// Godot prefers snake case names, but C# uses pascal and camel case names
	public static string CSharpNameToGodotName(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			GD.PushError("string.IsNullOrEmpty(value)");
			return string.Empty;
		}
		const char nullChar = '\0';
		char prev = nullChar;

		var sb = new StringBuilder();
		foreach (char c in value)
		{
			if (char.IsUpper(c))
			{
				// insert underscore before uppercase characters
				if (prev != nullChar && prev != '/')
				{
					sb.Append('_');
				}

				prev = char.ToLower(c);
				sb.Append(prev);
				continue;
			}

			if (c == '.')
			{
				prev = '/';
				sb.Append(prev);
				continue;
			}

			prev = c;
			sb.Append(prev);
		}

		string result = sb.ToString();
		// GD.Print($"{value} -> {result}");
		return result;
	}

	public static string VariableNameToFriendlyName(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			GD.PushError("string.IsNullOrEmpty(value)");
			return string.Empty;
		}

		const char nullChar = '\0';
		char prev = nullChar;
		var sb = new StringBuilder();
		foreach (char c in value)
		{
			if (char.IsLower(c))
			{
				prev = (prev == nullChar || prev == ' ')
					? char.ToUpper(c)
					: char.ToLower(c);

				sb.Append(prev);
				continue;
			}

			if (c == '_')
			{
				if (prev != nullChar && prev != ' ')
				{
					prev = ' ';
					sb.Append(prev);
				}
				continue;
			}

			if (char.IsUpper(c))
			{
				if (prev != nullChar && !char.IsUpper(prev))
				{
					sb.Append(' ');
				}
			}

			prev = c;
			sb.Append(prev);
		}

		string result = sb.ToString();
		// GD.Print($"{value} -> {result}");
		return result;
	}

	public static bool TryTranslateString(StringName key, out StringName value)
		=> TryTranslateString(key, null, out value);

	public static bool TryTranslateString(StringName key, string context, out StringName value)
	{
		value = TranslationServer.Translate(key, context);
		return !string.IsNullOrEmpty(value) && value != key;
	}

	public static Error ReadCsv(string csvFile, out string[] headers, out Dictionary<string, Dictionary<string, string>> table)
	{
		// don't out null values
		headers = System.Array.Empty<string>();
		table = new();

		if (!FileAccess.FileExists(csvFile))
		{
			return Error.FileNotFound;
		}

		using var file = FileAccess.Open(csvFile, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			var error = FileAccess.GetOpenError();
			GD.PushError($"file == null; csvFile = {csvFile}; error = {error}");
			return error;
		}

		if (file.GetLength() == 0)
		{
			GD.PushError($"file.GetLength() == 0; csvFile = {csvFile}");
			return Error.InvalidData;
		}

		headers = file.GetCsvLine();

		while (file.GetPosition() < file.GetLength())
		{
			var values = new Dictionary<string, string>();
			string[] line = file.GetCsvLine();
			if (line == null || line.Length == 0)
			{
				continue;
			}

			string id = line[0];
			if (string.IsNullOrEmpty(id))
			{
				continue;
			}

			for (int i = 0; i < headers.Length; i++)
			{
				string key = headers[i];
				values[key] = line[i];
			}

			table[id] = values;
		}

		return Error.Ok;
	}

	#endregion Public Methods
}
