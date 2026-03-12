namespace Ragnarok.Flow.Issues.Monitor.ConsoleApp
{
    public class AzureDevOpsService
    {
        private readonly AzureDevOpsAPI _api;

        public AzureDevOpsService(AzureDevOpsAPI api)
        {
            _api = api;
        }

        public async Task<List<WorkItemRef>> GetAllOpenIssues()
        {
            var issues = await _api.GetAllOpenIssues();

            if (issues?.WorkItems == null || issues.WorkItems.Count == 0)
            {
                return new List<WorkItemRef>();
            }

            return issues.WorkItems;
        }

        public async Task<List<Comment>> GetAllCommentsFromIssue(int issueId)
        {
            var comments = await _api.GetAllCommentsFromIssue(issueId);

            return comments ?? [];
        }

        public async Task<Comment?> GetMostRecentlyIssueComment(int issueId)
        {
            var issueComments = await GetAllCommentsFromIssue(issueId);

            if (issueComments is null || issueComments.Count == 0)
                return null;

            var mostRecentComment = issueComments
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefault();

            if (mostRecentComment is null)
                return null;

            return mostRecentComment;
        }

        public async Task<string?> GetIssueTitle(int issueId)
        {
            return await _api.GetIssueTitle(issueId);
        }

        public async Task<DateTime?> GetIssueCreatedDate(int issueId)
        {
            return await _api.GetIssueCreatedDate(issueId);
        }
    }
}
