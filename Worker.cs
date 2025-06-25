using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceCredentialer.Interfaces;
using ServiceCredentialer.Models;

namespace ServiceCredentialer;

/// <summary>
/// Main worker service that orchestrates credential monitoring and service updates
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ICredentialFileMonitor _fileMonitor;
    private readonly IServiceCredentialUpdater _credentialUpdater;
    private readonly CredentialServiceOptions _options;

    public Worker(
        ILogger<Worker> logger,
        ICredentialFileMonitor fileMonitor,
        IServiceCredentialUpdater credentialUpdater,
        IOptions<CredentialServiceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileMonitor = fileMonitor ?? throw new ArgumentNullException(nameof(fileMonitor));
        _credentialUpdater = credentialUpdater ?? throw new ArgumentNullException(nameof(credentialUpdater));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("ServiceCredentialer starting up...");
            _logger.LogInformation("Target Service: {ServiceName}", _options.TargetServiceName);
            _logger.LogInformation("Credentials File: {FilePath}", _options.CredentialsFilePath);
            _logger.LogInformation("Check Interval: {Interval} seconds", _options.CheckIntervalSeconds);

            // Validate service access
            var serviceAccessible = await _credentialUpdater.ValidateServiceAccessAsync(stoppingToken);
            if (!serviceAccessible)
            {
                _logger.LogWarning("Target service '{ServiceName}' is not accessible. Service will continue monitoring for changes.",
                    _options.TargetServiceName);
            }

            // Subscribe to credential changes
            _fileMonitor.CredentialsChanged += OnCredentialsChanged;

            // Start file monitoring
            await _fileMonitor.StartMonitoringAsync(stoppingToken);

            // Load initial credentials and update service
            var initialCredentials = await _fileMonitor.LoadCredentialsAsync();
            if (initialCredentials != null)
            {
                _logger.LogInformation("Processing initial credentials...");
                await ProcessCredentialsUpdate(initialCredentials, stoppingToken);
            }

            // Main service loop - periodic checks
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.CheckIntervalSeconds), stoppingToken);
                    
                    // Periodic health check
                    _logger.LogDebug("Performing periodic health check...");
                    
                    // You could add additional health checks here
                    // For example, verify the service is still accessible
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in main service loop");
                    // Continue running even if there's an error
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in ServiceCredentialer");
            throw;
        }
        finally
        {
            _logger.LogInformation("ServiceCredentialer shutting down...");
            
            // Unsubscribe from events
            if (_fileMonitor != null)
            {
                _fileMonitor.CredentialsChanged -= OnCredentialsChanged;
                await _fileMonitor.StopMonitoringAsync();
            }
        }
    }

    private async void OnCredentialsChanged(object? sender, ServiceCredentials credentials)
    {
        try
        {
            _logger.LogInformation("Credential change detected, updating service...");
            await ProcessCredentialsUpdate(credentials, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing credential change");
        }
    }

    private async Task ProcessCredentialsUpdate(ServiceCredentials credentials, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _credentialUpdater.UpdateServiceCredentialsAsync(credentials, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Service credentials updated successfully");
            }
            else
            {
                _logger.LogError("Failed to update service credentials");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service credentials");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ServiceCredentialer stop requested...");
        await base.StopAsync(cancellationToken);
    }
}
