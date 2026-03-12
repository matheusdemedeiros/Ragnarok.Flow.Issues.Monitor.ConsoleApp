namespace Ragnarok.Flow.Issues.Monitor.ConsoleApp
{
    public class IssuesReportFactory
    {
        private readonly AzureDevOpsApiConfig _azureDevOpsApiConfig;

        public IssuesReportFactory(AzureDevOpsApiConfig azureDevOpsApiConfig)
        {
            _azureDevOpsApiConfig = azureDevOpsApiConfig;
        }

        public OpenIssuesReport Factory()
        {
            return new OpenIssuesReport(_azureDevOpsApiConfig.BaseUrl, _azureDevOpsApiConfig.Collection, _azureDevOpsApiConfig.PAT, _azureDevOpsApiConfig.AreaTeam);
        }
    }
}
