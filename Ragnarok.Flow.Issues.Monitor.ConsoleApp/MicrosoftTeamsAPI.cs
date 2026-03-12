using System.Net.Http.Json;

namespace Ragnarok.Flow.Issues.Monitor.ConsoleApp
{
    public class MicrosoftTeamsAPI
    {
        private readonly MicrosoftTeamsConfig _microsoftTeamsConfig;

        public MicrosoftTeamsAPI(MicrosoftTeamsConfig microsoftTeamsConfig)
        {
            _microsoftTeamsConfig = microsoftTeamsConfig;
        }

        public async Task<bool> SendMessageAsync(string message)
        {
            using var httpClient = new HttpClient();
            var payload = new
            {
                text = message
            };

            var response = await httpClient.PostAsJsonAsync(_microsoftTeamsConfig.WebhookUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }
    }
}
