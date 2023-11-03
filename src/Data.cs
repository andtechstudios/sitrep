using Andtech.Sitrep;

class Commit
{
	public string Tag;
	public string Hash;
	public DateTime Date;
	public Message Message { get; set; }
	public int Issue
	{
		get
		{

			if (TryParseIssue(Message.Footer, out var issue))
			{
				return issue;
			}

			return 0;

			bool TryParseIssue(string text, out int id)
			{
				id = 0;
				if (string.IsNullOrEmpty(text))
				{
					return false;
				}

				var match = Util.IssueRegex.Match(text);
				if (match.Success)
				{
					id = int.Parse(match.Groups["id"].Value);
					return true;
				}

				return false;
			}
		}
	}

	public Commit(string hash)
	{
		Hash = hash;
	}
}

class Message
{
	public string Subject;
	public string Body;
	public string Footer;

	public static Message Parse(string input)
	{
		var lines = input.Split("\n", StringSplitOptions.RemoveEmptyEntries);
		var line0 = lines.Length >= 1 ? lines[0] : string.Empty;
		var line1 = lines.Length >= 2 ? lines[1] : string.Empty;
		var line2 = lines.Length >= 3 ? lines[2] : string.Empty;

		List<string> stack = new List<string>
		{
			line0
		};
		if (Util.ImplementsRegex.IsMatch(line1))
		{
			stack.Add(string.Empty);
			stack.Add(line1);
		}
		else
		{
			stack.Add(line1);
			stack.Add(line2);
		}

		var message = new Message()
		{
			Subject = stack[0],
			Body = stack[1],
			Footer = stack[2],
		};

		return message;
	}
}
