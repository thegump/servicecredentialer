using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceCredentialer.Interfaces;
using ServiceCredentialer.Models;

namespace ServiceCredentialer.Services;

/// <summary>
/// Service responsible for monitoring credential file changes
/// </summary>
public class CredentialFileMonitor : ICredentialFileMonitor
{
    private readonly ILogger<CredentialFileMonitor> _logger;
    private readonly CredentialServiceOptions _options;
    private FileSystemWatcher? _fileWatcher;
    private DateTime _lastFileWrite = DateTime.MinValue;
    private readonly object _lockObject = new();
    private bool _disposed = false;

    public event EventHandler<ServiceCredentials>? CredentialsChanged;

    public CredentialFileMonitor(
        ILogger<CredentialFileMonitor> logger,
        IOptions<CredentialServiceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_options.CredentialsFilePath) ?? Directory.GetCurrentDirectory();
            var fileName = Path.GetFileName(_options.CredentialsFilePath);

            _logger.LogInformation("Starting credential file monitoring for {FilePath}", _options.CredentialsFilePath);

            // Ensure directory exists
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory {Directory}", directory);
            }

            // Create sample credentials file if it doesn't exist
            if (!File.Exists(_options.CredentialsFilePath))
            {
                await CreateSampleCredentialsFileAsync();
            }

            // Set up file system watcher
            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Error += OnWatcherError;

            _logger.LogInformation("File monitoring started successfully");

            // Load initial credentials
            var initialCredentials = await LoadCredentialsAsync();
            if (initialCredentials != null)
            {
                _logger.LogInformation("Loaded initial credentials for user {Username}", 
                    MaskUsername(initialCredentials.Username));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start credential file monitoring");
            throw;
        }
    }

    public Task StopMonitoringAsync()
    {
        try
        {
            _fileWatcher?.Dispose();
            _fileWatcher = null;
            _logger.LogInformation("File monitoring stopped");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping file monitoring");
            throw;
        }
    }

    public async Task<ServiceCredentials?> LoadCredentialsAsync()
    {
        try
        {
            if (!File.Exists(_options.CredentialsFilePath))
            {
                _logger.LogWarning("Credentials file not found at {FilePath}", _options.CredentialsFilePath);
                return null;
            }

            var fileInfo = new FileInfo(_options.CredentialsFilePath);
            var jsonContent = await File.ReadAllTextAsync(_options.CredentialsFilePath);
            
            var credentials = JsonSerializer.Deserialize<ServiceCredentials>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (credentials != null)
            {
                credentials.LastModified = fileInfo.LastWriteTime;
                _logger.LogDebug("Credentials loaded successfully for user {Username}", 
                    MaskUsername(credentials.Username));
            }

            return credentials;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse credentials JSON file");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load credentials from file");
            return null;
        }
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            lock (_lockObject)
            {
                // Debounce multiple file system events
                var fileInfo = new FileInfo(e.FullPath);
                if (fileInfo.LastWriteTime <= _lastFileWrite.AddSeconds(1))
                {
                    return;
                }
                _lastFileWrite = fileInfo.LastWriteTime;
            }

            _logger.LogInformation("Credentials file changed, reloading...");

            // Small delay to ensure file write is complete
            await Task.Delay(500);

            var credentials = await LoadCredentialsAsync();
            if (credentials != null)
            {
                _logger.LogInformation("Credentials updated for user {Username}", 
                    MaskUsername(credentials.Username));
                CredentialsChanged?.Invoke(this, credentials);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file change event");
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "File system watcher error occurred");
    }

    private async Task CreateSampleCredentialsFileAsync()
    {
        try
        {
            var sampleCredentials = new ServiceCredentials
            {
                Username = "sample_user",
                Password = "sample_password_123",
                LastModified = DateTime.UtcNow
            };

            var jsonContent = JsonSerializer.Serialize(sampleCredentials, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_options.CredentialsFilePath, jsonContent);
            _logger.LogInformation("Created sample credentials file at {FilePath}", _options.CredentialsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sample credentials file");
        }
    }

    private static string MaskUsername(string username)
    {
        if (string.IsNullOrEmpty(username) || username.Length <= 3)
            return "***";
        
        return username[..3] + new string('*', username.Length - 3);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _fileWatcher?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
