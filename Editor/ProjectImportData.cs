using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

using GodotCollections = Godot.Collections;

namespace Yarn.Godot.Editor
{
	[Serializable]
	public partial class ProjectImportData : RefCounted
	{
		[Export]
		public GodotCollections.Array<YarnProjectImporter.SerializedDeclaration> serializedDeclarations = new();

		public bool HasCompileErrors => diagnostics.Count() > 0;

		[Export]
		public bool containsImplicitLineIDs = false;

		[Export(PropertyHint.File)]
		public GodotCollections.Array<string> yarnFiles = new();

		[Serializable]
		public partial class LocalizationEntry : RefCounted
		{
			[Export]
			public string languageID;

			[Export(PropertyHint.Dir)]
			public string assetsFolder;

			[Export(PropertyHint.File)]
			public string stringsFile;
		}

		[Serializable]
		public partial class DiagnosticEntry : RefCounted
		{
			[Export(PropertyHint.File)]
			public string yarnFile;

			[Export]
			public GodotCollections.Array<string> errorMessages;
		}

		public enum ImportStatusCode
		{
			Unknown = 0,
			Succeeded = 1,
			CompilationFailed = 2,
			NeedsUpgradeFromV1 = 3,
		}

		[Export]
		public ImportStatusCode ImportStatus = ImportStatusCode.Unknown;

		[Export]
		public GodotCollections.Array<DiagnosticEntry> diagnostics = new();

		[Export]
		public GodotCollections.Array<string> sourceFilePaths = new();

		[Export]
		public GodotCollections.Array<LocalizationEntry> localizations = new();

		[Export]
		public string baseLanguageName;

		public LocalizationEntry BaseLocalizationEntry
		{
			get
			{
				try
				{
					return localizations.First(l => l.languageID == baseLanguageName);
				}
				catch (System.Exception e)
				{
					throw new System.InvalidOperationException("Project import data has no base localisation", e);
				}
			}
		}

		public bool TryGetLocalizationEntry(string languageID, out LocalizationEntry result)
		{
			foreach (var loc in this.localizations)
			{
				if (loc.languageID == languageID)
				{
					result = loc;
					return true;
				}
			}
			result = default;
			return false;
		}
	}
}