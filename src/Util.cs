using System.Text.RegularExpressions;
using Andtech.Common;
using CliWrap;
using CliWrap.Buffered;

namespace Andtech.Sitrep
{

	public class Util
	{
		public static Regex ImplementsRegex = new Regex(@"^[iI]mplement((s)|(ed)|(ing))? (#(?<id>\d+))|(feature)");
		public static Regex IssueRegex = new Regex(@"#(?<id>\d+)");

		public static async Task<string> GitAsync(string input)
		{
			var arguments = ParseUtil.QuotedSplit(input);
			var result = await Cli.Wrap("git")
				.WithArguments(arguments)
				.ExecuteBufferedAsync();

			return result.StandardOutput;
		}

		public static string SanitizeTag(string tag)
		{
			tag = Regex.Replace(tag, @"\.0+$", string.Empty);
			return tag;
		}

		public static string EvaluateIssueLinks(string url, string text)
		{
			return IssueRegex.Replace(text, OnMatch);

			string OnMatch(Match match)
			{
				var id = match.Groups["id"].Value;
				return $"[#{id}]({url}/{id})";
			}
		}
	}
}
