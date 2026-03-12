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
            var enableLogging = context.Configuration.GetValue<bool>("WindowsServiceConfigs:EnableLogging");

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
            var cronExpression = context.Configuration["WindowsServiceConfigs:MonitorCronExpression"];
            var runOnStart = context.Configuration.GetValue<bool>("WindowsServiceConfigs:RunIssueMonitorOnStart");

            if (string.IsNullOrWhiteSpace(cronExpression))
            {
                throw new ArgumentException("A expressão CRON para o monitor de issues não pode estar vazia. Verifique o appsettings.json.");
            }

            AzureDevOpsApiConfig azureDevOpsApiConfig = new();
            context.Configuration.Bind("AzureDevOpsApi", azureDevOpsApiConfig);
            services.AddSingleton(azureDevOpsApiConfig);

            MicrosoftTeamsConfig microsoftTeamsConfig = new();
            context.Configuration.Bind("MicrosoftTeams", microsoftTeamsConfig);
            services.AddSingleton(microsoftTeamsConfig);

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
                    .WithCronSchedule(cronExpression, x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"))));

                if (runOnStart)
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
