using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Ragnarok.Flow.Issues.Monitor.ConsoleApp
{
    public class AzureDevOpsAPI
    {
        private readonly AzureDevOpsApiConfig _azureDevOpsApiConfig;
        private readonly string _azureDevOpsBaseUrl = string.Empty;

            public AzureDevOpsAPI(AzureDevOpsApiConfig azureDevOpsApiConfig)
            {
                _azureDevOpsApiConfig = azureDevOpsApiConfig;
                _azureDevOpsBaseUrl = $"{_azureDevOpsApiConfig.BaseUrl}/{_azureDevOpsApiConfig.Collection}/{_azureDevOpsApiConfig.Project}";
            }

            public async Task<WiqlResponse?> GetAllOpenIssues()
            {
                using var httpClient = CreateHttpClient();

                var wiql = new
                {
                    query = $@"
            SELECT 
                [System.Id],
                [System.Title]
            FROM WorkItems
            WHERE
                [System.WorkItemType] = 'Issue'
                AND [System.State] <> 'Closed'
                AND [System.AreaPath] UNDER '{_azureDevOpsApiConfig.AreaTeam}'"
                };

                var wiqlResponse = await httpClient.PostAsync(
                    $"{_azureDevOpsBaseUrl}/_apis/wit/wiql?api-version=7.0",
                    new StringContent(JsonConvert.SerializeObject(wiql), Encoding.UTF8, "application/json")
                );

                var wiqlContent = await wiqlResponse.Content.ReadAsStringAsync();
                var wiqlData = JsonConvert.DeserializeObject<WiqlResponse>(wiqlContent);

                return wiqlData;
            }

            public async Task<List<Comment>?> GetAllCommentsFromIssue(int issueId)
            {
                using var httpClient = CreateHttpClient();

                var response = await httpClient.GetAsync(
                $"{_azureDevOpsBaseUrl}/_apis/wit/workItems/{issueId}/comments?api-version=7.0-preview.3"
                );

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"⚠️ Erro {response.StatusCode} ao buscar comentários do item {issueId}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var commentsResponse = JsonConvert.DeserializeObject<CommentsResponse>(content);

                return commentsResponse?.Comments;
            }

            public async Task<string?> GetIssueTitle(int issueId)
            {
                using var httpClient = CreateHttpClient();

                var response = await httpClient.GetAsync(
                    $"{_azureDevOpsBaseUrl}/_apis/wit/workitems/{issueId}?fields=System.Title&api-version=7.0"
                );

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<WorkItemDetailsResponse>(content);

                return result?.Fields?.Title;
            }

        public async Task<DateTime?> GetIssueCreatedDate(int issueId)
        {
            using var httpClient = CreateHttpClient();

            var response = await httpClient.GetAsync(
                $"{_azureDevOpsBaseUrl}/_apis/wit/workitems/{issueId}?fields=System.CreatedDate&api-version=7.0"
            );

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<WorkItemDetailsResponse>(content);

            return result?.Fields?.CreatedDate;
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

            string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_azureDevOpsApiConfig.PAT}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            return httpClient;
        }
    }
}
