using Microsoft.Extensions.Logging;
using Quartz;

namespace Ragnarok.Flow.Issues.Monitor.ConsoleApp
{
    [DisallowConcurrentExecution]
    public class IssueMonitorJob : IJob
    {
        private readonly ILogger<IssueMonitorJob> _logger;
        private AzureDevOpsService _azureDevOpsService;
        private MicrosoftTeamsAPI _teamsAPI;
        private readonly IssuesReportFactory _issuesReportFactory;

        public IssueMonitorJob(
                ILogger<IssueMonitorJob> logger,
                AzureDevOpsService azureDevOpsService,
                MicrosoftTeamsAPI teamsAPI,
                IssuesReportFactory issuesReportFactory
            )
        {
            _logger = logger;
            _azureDevOpsService = azureDevOpsService;
            _teamsAPI = teamsAPI;
            _issuesReportFactory = issuesReportFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Iniciando execução do IssueMonitorJob em {time}", DateTime.Now);

                var openIssues = await GetAllOpenIssuesAsync();
                var report = await BuildIssuesReport(openIssues);

                if (string.IsNullOrEmpty(report))
                {
                    _logger.LogInformation("Nenhuma issue pendente encontrada.");
                    return;
                }

                var result = await _teamsAPI.SendMessageAsync(report);

                if (result)
                    _logger.LogInformation("Resumo diário enviado com sucesso ao Microsoft Teams.");
                else
                    _logger.LogWarning("Falha ao enviar resumo diário ao Microsoft Teams.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico durante a execução do IssueMonitorJob");
            }
        }

        private async Task<string> BuildIssuesReport(List<WorkItemRef> openIssues)
        {
            var openIssuesReport = _issuesReportFactory.Factory();

            var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };
            await Parallel.ForEachAsync(openIssues, options, async (item, ct) =>
            {
                try
                {
                    var comment = await _azureDevOpsService.GetMostRecentlyIssueComment(item.Id);
                    if (comment is null)
                        return;

                    var title = await _azureDevOpsService.GetIssueTitle(item.Id);
                    openIssuesReport.AddIssue(title!, item.Id, comment);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao processar issue {id}", item.Id);
                }
            });

            return openIssuesReport.BuildReport();
        }

        private async Task<List<WorkItemRef>> GetAllOpenIssuesAsync()
        {
            return await _azureDevOpsService.GetAllOpenIssues();
        }
    }
}
