using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Ragnarok.Flow.Issues.Monitor.ConsoleApp;

try
{
    var builder = Host.CreateDefaultBuilder(args)
        .UseWindowsService(options =>
        {
            options.ServiceName = "Ragnarok Issue Monitor Service";
        })
        .ConfigureAppConfiguration((context, config) =>
        {
            var environmentName = context.HostingEnvironment.EnvironmentName;
            config.SetBasePath(AppContext.BaseDirectory);
            config.AddJsonFile($"appsettings.{environmentName}.json", optional: false, reloadOnChange: true);
        })
        .UseSerilog((context, services, configuration) =>
        {
            var enableLogging = services.GetService<WindowsServiceConfig>()?.EnableLogging ?? false;

            if (!enableLogging)
            {
                configuration.MinimumLevel.Fatal();
                return;
            }

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext();
        })
        .ConfigureServices((context, services) =>
        {
            AzureDevOpsApiConfig azureDevOpsApiConfig = new();
            context.Configuration.Bind("AzureDevOpsApi", azureDevOpsApiConfig);
            services.AddSingleton(azureDevOpsApiConfig);

            MicrosoftTeamsConfig microsoftTeamsConfig = new();
            context.Configuration.Bind("MicrosoftTeams", microsoftTeamsConfig);
            services.AddSingleton(microsoftTeamsConfig);

            WindowsServiceConfig windowsServiceConfig = new();
            context.Configuration.Bind("WindowsServiceConfigs", windowsServiceConfig);
            services.AddSingleton(windowsServiceConfig);

            if (string.IsNullOrWhiteSpace(windowsServiceConfig.MonitorCronExpression))
            {
                throw new ArgumentException("A expressão CRON para o monitor de issues não pode estar vazia. Verifique o appsettings.json.");
            }

            services.AddSingleton<AzureDevOpsAPI>();
            services.AddSingleton<AzureDevOpsService>();

            services.AddSingleton<MicrosoftTeamsAPI>();
            services.AddSingleton<IssuesReportFactory>();

            services.AddQuartz(q =>
            {
                var jobKey = new JobKey("IssueMonitorJob");
                q.AddJob<IssueMonitorJob>(opts => opts.WithIdentity(jobKey));

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("IssueMonitorJob-trigger")
                    .WithCronSchedule(windowsServiceConfig.MonitorCronExpression, x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"))));

                if (windowsServiceConfig.RunIssueMonitorOnStart)
                {
                    q.AddTrigger(opts => opts
                        .ForJob(jobKey)
                        .WithIdentity("IssueMonitorJob-startup")
                        .StartNow());
                }
            });

            services.AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

            services.AddSingleton<IssueMonitorJob>();
        });

    var host = builder.Build();

    Log.Information("Issue Monitor Service iniciado com sucesso.");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha fatal ao iniciar o serviço.");
}
finally
{
    Log.CloseAndFlush();
}
