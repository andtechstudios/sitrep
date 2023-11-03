using System.Text.RegularExpressions;
using Andtech.Common;
using Andtech.Sitrep;
using Humanizer;

var devMode = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEV_MODE"));
var issueUrl = Environment.GetEnvironmentVariable("ISSUE_URL");
var tagLevels = 10;
var tagsString = await Util.GitAsync("tag --list \"v*\" --sort=-v:refname");
var tags = Regex.Split(tagsString.Trim(), @"\s+")
	.Take(tagLevels)
	.ToList();

var commits = (await ReadCommitsAsync())
	.Where(FilterCommit);

if (devMode)
{
	foreach (var group in commits.GroupBy(x => x.Tag))
	{
		Log.WriteLine($"# {group.Key}");
		WriteCommits(group);
		Log.WriteLine();
	}
}
else
{
	foreach (var group in commits.GroupBy(x => x.Date.Date))
	{
		Log.WriteLine($"# {group.Key:yyyy-MM-dd}");
		WriteCommits(group);
		Log.WriteLine();
	}
}

void WriteCommits(IEnumerable<Commit> commits)
{
	HashSet<int> issuesToIgnore = new HashSet<int>();
	foreach (var commit in commits)
	{
		string line;
		// Add message
		if (string.IsNullOrEmpty(commit.Message.Body))
		{
			line = commit.Message.Subject.Humanize();
		}
		else
		{
			line = commit.Message.Body.Humanize();
		}

		if (devMode)
		{
			// Add issue hyperlink
			if (commit.Issue > 0 && !issuesToIgnore.Contains(commit.Issue))
			{
				line += $" (#{commit.Issue})";
				// Ignore this issue number in future runs
				issuesToIgnore.Add(commit.Issue);
			}

			// Convert issue hyperlinks to markdown
			if (!string.IsNullOrEmpty(issueUrl))
			{
				line = Util.EvaluateIssueLinks(issueUrl, line);
			}
		}		

		// Print the line
		Log.WriteLine($"* {line}");
	}
}

bool FilterCommit(Commit commit)
{
	if (commit.Message.Subject.StartsWith("wip:"))
	{
		return false;
	}
	if (commit.Message.Subject.StartsWith("Merge branch"))
	{
		return false;
	}

	if (!devMode)
	{
		if (string.IsNullOrEmpty(commit.Message.Body))
		{
			return false;
		}
	}

	return true;
}

async Task<IEnumerable<Commit>> ReadCommitsAsync()
{
	var allCommits = new List<Commit>();
	// Loop through tags (most recent to oldest)
	for (int i = 0; i < tags.Count; i++)
	{
		var range = i + 1 < tags.Count ? $"{tags[i + 1]}..{tags[i]}" : $"{tags[i]}";
		var lines = (await Util.GitAsync($"log --pretty=format:%h|%ad|%s --date=iso {range}"))
			.Split("\n", StringSplitOptions.RemoveEmptyEntries);

		var highestDate = DateTime.UnixEpoch;
		var commits = new List<Commit>();
		foreach (var line in lines)
		{
			var tokens = line.Split("|");
			var hash = tokens[0];
			var date = DateTime.Parse(tokens[1]);
			var message = await Util.GitAsync($"show --format=%B -s {hash}");

			var commit = new Commit(hash)
			{
				Tag = tags[i],
				Message = Message.Parse(message),
			};
			allCommits.Add(commit);

			// Remember highest date
			if (date > highestDate)
			{
				highestDate = date;
			}
		}

		// Finalize
		foreach (var commit in commits)
		{
			commit.Date = highestDate;
		}
		allCommits.AddRange(commits);
	}

	return allCommits;
}
