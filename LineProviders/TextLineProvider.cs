namespace Yarn.Godot.LineProviders
{
	public partial class TextLineProvider : LineProviderBehaviour
	{
		public override LocalizedLine GetLocalizedLine(Line line)
		{
			var text = Tr(line.ID);
			return new LocalizedLine()
			{
				TextID = line.ID,
				RawText = text,
				Substitutions = line.Substitutions,
				Metadata = YarnProject.lineMetadata.GetMetadata(line.ID),
			};
		}
	}
}
