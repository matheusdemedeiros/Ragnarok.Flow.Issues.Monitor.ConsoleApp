public record AzureDevOpsApiConfig
{
    public string Collection { get; init; }
    public string Project { get; init; }
    public string BaseUrl { get; init; }
    public string PAT { get; init; }
    public string AreaTeam { get; init; }
}

public record MicrosoftTeamsConfig
{
    public string WebhookUrl { get; init; }
}

public record WindowsServiceConfig
{
    public bool RunIssueMonitorOnStart { get; init; }
    public string MonitorCronExpression { get; init; }
    public bool EnableLogging { get; init; }
}