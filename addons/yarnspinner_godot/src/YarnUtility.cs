using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.GodotSharp
{
	public static class YarnUtility
	{
		public static IEnumerable<string> SplitCommandText(string input)
		{
			var reader = new System.IO.StringReader(input.Normalize());

			int c;

			var results = new List<string>();
			var currentComponent = new System.Text.StringBuilder();

			while ((c = reader.Read()) != -1)
			{
				if (char.IsWhiteSpace((char)c))
				{
					if (currentComponent.Length > 0)
					{
						// We've reached the end of a run of visible characters. Add this run to the
						// result list and prepare for the next one.
						results.Add(currentComponent.ToString());
						currentComponent.Clear();
					}
					else
					{
						// We encountered a whitespace character, but didn't have any characters
						// queued up. Skip this character.
					}

					continue;
				}
				else if (c == '\"')
				{
					// We've entered a quoted string!
					while (true)
					{
						c = reader.Read();
						if (c == -1)
						{
							results.Add(currentComponent.ToString());
							return results;
						}
						else if (c == '\\')
						{
							// Possibly an escaped character!
							var next = reader.Peek();
							if (next == '\\' || next == '\"')
							{
								// It is! Skip the \ and use the character after it.
								reader.Read();
								currentComponent.Append((char)next);
							}
							else
							{
								// Oops, an invalid escape. Add the \ and whatever is after it.
								currentComponent.Append((char)c);
							}
						}
						else if (c == '\"')
						{
							// The end of a string!
							break;
						}
						else
						{
							// Any other character. Add it to the buffer.
							currentComponent.Append((char)c);
						}
					}

					results.Add(currentComponent.ToString());
					currentComponent.Clear();
				}
				else
				{
					currentComponent.Append((char)c);
				}
			}

			if (currentComponent.Length > 0)
			{
				results.Add(currentComponent.ToString());
			}

			return results;
		}
	}
}