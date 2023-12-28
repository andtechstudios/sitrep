using System.Text.RegularExpressions;
using Andtech.Common;
using Andtech.Sitrep;
using Humanizer;

// Locals
var devMode = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEV_MODE"));
var issueUrl = Environment.GetEnvironmentVariable("ISSUE_URL");
var tagLevels = 6;

// Begin program
var tagsString = await Util.GitAsync("tag --list \"v*\" --sort=-v:refname");
var tags = Regex.Split(tagsString.Trim(), @"\s+")
	.Take(tagLevels)
	.ToList();
DateTime currentDay = DateTime.UnixEpoch;
HashSet<int> issuesToIgnore = new HashSet<int>();
// Loop through tags (most recent to oldest)
for (int i = 0; i < tags.Count; i++)
{
	var range = i + 1 < tags.Count ? $"{tags[i + 1]}..{tags[i]}" : $"{tags[i]}";
	var lines = (await Util.GitAsync($"log --pretty=format:%h|%ad|%s --date=iso {range}"))
		.Split("\n", StringSplitOptions.RemoveEmptyEntries);

	// Heading
	if (devMode)
	{
		Log.WriteLine($"# {tags[i]}");
	}

	// Iterate through commits
	bool hasAnyCommits = false;
	foreach (var line in lines)
	{
		var tokens = line.Split("|");
		var hash = tokens[0];
		var date = DateTime.Parse(tokens[1]);
		var message = await Util.GitAsync($"show --format=%B -s {hash}");

		var commit = new Commit(hash)
		{
			Tag = tags[i],
			Date = date,
			Message = Message.Parse(message),
		};
		if (!FilterCommit(commit))
		{
			continue;
		}

		// Process commit
		if (devMode)
		{

		}
		else
		{
			if (commit.Date != currentDay)
			{
				// Date heading
				Log.WriteLine($"# {commit.Date:yyyy-MM-dd}");
				currentDay = commit.Date;
			}
		}
		ProcessCommit(commit);
		hasAnyCommits = true;
	}
	
	if (hasAnyCommits)
	{
		Log.WriteLine();
	}
}

void ProcessCommit(Commit commit)
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
