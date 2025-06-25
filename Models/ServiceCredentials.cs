namespace ServiceCredentialer.Models;

/// <summary>
/// Represents service credentials loaded from JSON file
/// </summary>
public class ServiceCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Configuration settings for the credential service
/// </summary>
public class CredentialServiceOptions
{
    public const string SectionName = "CredentialService";
    
    /// <summary>
    /// Path to the JSON credentials file to monitor
    /// </summary>
    public string CredentialsFilePath { get; set; } = "credentials.json";
    
    /// <summary>
    /// Name of the target service to update
    /// </summary>
    public string TargetServiceName { get; set; } = "Sample Service";
    
    /// <summary>
    /// Interval in seconds between credential file checks
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Number of retry attempts for service updates
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Delay in seconds between retry attempts
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;
}
