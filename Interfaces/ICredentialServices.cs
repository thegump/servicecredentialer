using ServiceCredentialer.Models;

namespace ServiceCredentialer.Interfaces;

/// <summary>
/// Interface for monitoring credential file changes
/// </summary>
public interface ICredentialFileMonitor : IDisposable
{
    /// <summary>
    /// Event fired when credentials file is changed
    /// </summary>
    event EventHandler<ServiceCredentials>? CredentialsChanged;
    
    /// <summary>
    /// Start monitoring the credentials file
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop monitoring the credentials file
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Load current credentials from file
    /// </summary>
    Task<ServiceCredentials?> LoadCredentialsAsync();
}

/// <summary>
/// Interface for updating service credentials
/// </summary>
public interface IServiceCredentialUpdater
{
    /// <summary>
    /// Update the target service with new credentials
    /// </summary>
    Task<bool> UpdateServiceCredentialsAsync(ServiceCredentials credentials, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate if the service exists and is accessible
    /// </summary>
    Task<bool> ValidateServiceAccessAsync(CancellationToken cancellationToken = default);
}
