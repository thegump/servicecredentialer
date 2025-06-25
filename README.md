# Service Credentialer

A C# Windows service that automatically monitors a JSON credentials file and updates a target service's credentials when changes are detected.

## Features

- **File System Monitoring**: Automatically detects changes to the credentials JSON file
- **Secure Credential Handling**: Uses structured logging that masks sensitive information
- **Service Account Updates**: Updates target service credentials using Windows Service Control APIs
- **Comprehensive Logging**: Structured logging with Serilog to file and console
- **Configurable Settings**: All settings configurable via appsettings.json
- **Error Handling & Retry**: Built-in retry logic with configurable attempts and delays
- **Windows Service Support**: Can run as a Windows service

## Configuration

Configure the service through `appsettings.json`:

```json
{
  "CredentialService": {
    "CredentialsFilePath": "credentials.json",
    "TargetServiceName": "Sample Service",
    "CheckIntervalSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

### Configuration Options

- `CredentialsFilePath`: Path to the JSON file containing credentials
- `TargetServiceName`: Name of the Windows service to update
- `CheckIntervalSeconds`: Interval between periodic health checks
- `MaxRetryAttempts`: Number of retry attempts for failed updates
- `RetryDelaySeconds`: Delay between retry attempts

## Credentials File Format

The service monitors a JSON file with the following structure:

```json
{
  "Username": "service_account_user",
  "Password": "secure_password_123",
  "LastModified": "2025-06-25T10:30:00Z"
}
```

## Running the Service

### Development Mode

```bash
dotnet run
```

### Install as Windows Service

```bash
# Build the application
dotnet publish -c Release -o ./publish

# Install as Windows service (requires admin privileges)
sc create "ServiceCredentialer" binPath="C:\Path\To\Your\publish\ServiceCredentialer.exe"
sc start "ServiceCredentialer"
```

### Uninstall Windows Service

```bash
sc stop "ServiceCredentialer"
sc delete "ServiceCredentialer"
```

## Project Structure

```
ServiceCredentialer/
├── Models/
│   └── ServiceCredentials.cs      # Data models and configuration
├── Interfaces/
│   └── ICredentialServices.cs     # Service interfaces
├── Services/
│   ├── CredentialFileMonitor.cs   # File monitoring service
│   └── ServiceCredentialUpdater.cs # Service credential update logic
├── Worker.cs                      # Main background service
├── Program.cs                     # Application entry point
└── appsettings.json              # Configuration
```

## Security Considerations

- **Never log sensitive information**: The service masks usernames and never logs passwords
- **Use secure file permissions**: Ensure the credentials file has appropriate permissions
- **Run with minimal privileges**: The service should run with the minimum required permissions
- **Credential validation**: Validates credentials before attempting updates

## Logging

Logs are written to:
- Console (when running in development)
- File: `logs/service-credentialer-YYYY-MM-DD.log` (rolling daily)

Log levels can be configured in `appsettings.json`.

## Error Handling

The service includes comprehensive error handling:
- Retries failed service updates with configurable delays
- Continues running even if the target service is unavailable
- Logs all errors with appropriate detail levels
- Graceful shutdown on service stop requests

## Dependencies

- .NET 8.0
- Microsoft.Extensions.Hosting.WindowsServices
- Serilog.Extensions.Hosting
- Serilog.Sinks.Console
- Serilog.Sinks.File

## License

This project is for demonstration purposes.
