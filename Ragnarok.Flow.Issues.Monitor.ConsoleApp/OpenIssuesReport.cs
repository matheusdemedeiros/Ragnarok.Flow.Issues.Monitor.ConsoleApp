using System.Collections.Concurrent;
using System.Text;

namespace Ragnarok.Flow.Issues.Monitor.ConsoleApp;

public class OpenIssuesReport
{
    private readonly ConcurrentBag<(int DaysSinceComment, string Line, int IssueId)> _lines = new();
    private readonly string _azureDevOpsCollection;
    private readonly string _azureDevOpsProject;
    private readonly string _azureDevOpsBaseUrl;
    private readonly string _areaTeam;

    public OpenIssuesReport(string azureDevOpsBaseUrl,
        string azureDevOpsCollection,
        string azureDevOpsProject,
        string areaTeam
        )
    {
        _azureDevOpsBaseUrl = azureDevOpsBaseUrl;
        _azureDevOpsCollection = azureDevOpsCollection;
        _azureDevOpsProject = azureDevOpsProject;
        _areaTeam = areaTeam;
    }

    public void AddIssue(string title, int issueId, Comment comment, DateTime? issueCreatedDate = null)
    {
        var author = GetAuthor(comment);
        var commentDate = GetCreationDate(comment);
        var daysSinceComment = GetCommentDays(commentDate);
        var url = GetUrl(issueId);

        var createdDateDisplay = issueCreatedDate.HasValue 
            ? $"{issueCreatedDate:dd/MM/yyyy HH:mm:ss}" 
            : "Data indisponível";

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
            .AppendLine($"📅 Na data: **{commentDate:dd/MM/yyyy HH:mm:ss}**")
            .AppendLine("<br>")
            .AppendLine($"📆 **Issue criada em:** {createdDateDisplay}")
            .AppendLine("<br>")
            .AppendLine("<br>")
            .AppendLine($"🔗 Link: {url}")
            .AppendLine("<br>")
            .AppendLine("<hr>")
            .AppendLine("<br>")
            .ToString();

        _lines.Add((daysSinceComment, line, issueId));
    }

    public string BuildReport()
    {
        if (_lines.IsEmpty)
            return string.Empty;

        var orderedIssues = _lines
            .OrderByDescending(x => x.DaysSinceComment)
            .ToList();

        var issueLines = orderedIssues
            .Select(x => x.Line)
            .ToList();

        var issueCount = _lines.Count;
        var oldestIssue = orderedIssues.First();
        var newestIssue = orderedIssues.Last();

        var title = "🔔 **Resumo diário das Issues**";
        var summary = new StringBuilder()
            .AppendLine($"👥 **Área\\Time:** {_areaTeam}")
            .AppendLine("<br>")
            .AppendLine("<br>")
            .AppendLine($"📊 **Total de Issues:** {issueCount}")
            .AppendLine("<br>")
            .AppendLine("<br>")
            .AppendLine($"⏰ **Issue com comentário mais antigo:** #{oldestIssue.IssueId} ({oldestIssue.DaysSinceComment} dias) - Link: {GetUrl(oldestIssue.IssueId)}")
            .AppendLine("<br>")
            .AppendLine("<br>")
            .AppendLine($"⚡ **Issue com comentário mais recente:** #{newestIssue.IssueId} ({newestIssue.DaysSinceComment} dias) - Link: {GetUrl(newestIssue.IssueId)}")
            .ToString()
            .Trim();

        var issuesTitle = "📋 **Issues Ordenadas por Tempo de Comentário**";
        var report = string.Join("\n", issueLines).Trim();

        return title + "<br><br>" + summary + "<br><br><hr><br>" + issuesTitle + "<br><br>" + report;
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
