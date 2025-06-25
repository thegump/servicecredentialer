using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ServiceCredentialer;
using ServiceCredentialer.Interfaces;
using ServiceCredentialer.Models;
using ServiceCredentialer.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/service-credentialer-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("Starting ServiceCredentialer service...");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog as the logging provider
    builder.Services.AddSerilog();

    // Configure options
    builder.Services.Configure<CredentialServiceOptions>(
        builder.Configuration.GetSection(CredentialServiceOptions.SectionName));

    // Register services
    builder.Services.AddSingleton<ICredentialFileMonitor, CredentialFileMonitor>();
    builder.Services.AddSingleton<IServiceCredentialUpdater, ServiceCredentialUpdater>();

    // Add the worker service
    builder.Services.AddHostedService<Worker>();

    // Configure as Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "ServiceCredentialer";
    });

    var host = builder.Build();

    Log.Information("ServiceCredentialer service configured successfully");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ServiceCredentialer service failed to start");
    throw;
}
finally
{
    Log.Information("ServiceCredentialer service stopped");
    await Log.CloseAndFlushAsync();
}
