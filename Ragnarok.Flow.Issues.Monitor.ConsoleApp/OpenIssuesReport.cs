using System.Collections.Concurrent;
using System.Text;

namespace Ragnarok.Flow.Issues.Monitor.ConsoleApp;

public class OpenIssuesReport
{
    private readonly ConcurrentBag<(int DaysSinceComment, string Line)> _lines = new();
    private readonly string _azureDevOpsCollection;
    private readonly string _azureDevOpsProject;
    private readonly string _azureDevOpsBaseUrl;

    public OpenIssuesReport(string azureDevOpsBaseUrl,
        string azureDevOpsCollection,
        string azureDevOpsProject
        )
    {
        _azureDevOpsBaseUrl = azureDevOpsBaseUrl;
        _azureDevOpsCollection = azureDevOpsCollection;
        _azureDevOpsProject = azureDevOpsProject;
    }

    public void AddIssue(string title, int issueId, Comment comment)
    {
        var author = GetAuthor(comment);
        var commentDate = GetCreationDate(comment);
        var daysSinceComment = GetCommentDays(commentDate);
        var url = GetUrl(issueId);

        var line = new StringBuilder()
            .AppendLine($"🔹 **ISSUE #{issueId}**")
            .AppendLine("<br>")
            .AppendLine($"📌 **{title}**")
            .AppendLine("<br>")
            .AppendLine("<br>")
            .AppendLine($"💬 Último comentário feito **há {daysSinceComment} dia{(daysSinceComment == 1 ? "" : "s")}**")
            .AppendLine("<br>")
            .AppendLine($"👤 Por: **{author}**")
            .AppendLine("<br>")
            .AppendLine($"📅 Na data: **{commentDate:dd/MM/yyyy}**")
            .AppendLine("<br>")
            .AppendLine("<br>")
            .AppendLine($"🔗 Link: {url}")
            .AppendLine("<br>")
            .AppendLine("<hr>")
            .AppendLine("<br>")
            .ToString();

        _lines.Add((daysSinceComment, line));
    }

    public string BuildReport()
    {
        if (_lines.IsEmpty)
            return string.Empty;

        var ordered = _lines
            .OrderByDescending(x => x.DaysSinceComment)
            .Select(x => x.Line)
            .ToList();

        var title = "🔔 **Resumo diário das Issues**";
        var report = string.Join("\n", ordered).Trim();

        return title + "<br><br>" + report;
    }

    private int GetCommentDays(DateTime commentDate)
    {
        return (int)(DateTime.UtcNow.Date - commentDate.Date).TotalDays;
    }

    private DateTime GetCreationDate(Comment comment)
    {
        return DateTime.SpecifyKind(comment.CreatedDate, DateTimeKind.Utc);
    }

    private string GetAuthor(Comment comment)
    {
        return comment.CreatedBy?.DisplayName?.Trim() ?? "Autor desconhecido";
    }

    private string GetUrl(int issueId)
    {
        return $"{_azureDevOpsBaseUrl}/{_azureDevOpsCollection}/{_azureDevOpsProject}/_workitems/edit/{issueId}";
    }
}
