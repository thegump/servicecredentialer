# Copilot Instructions

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a C# Windows service that periodically monitors a JSON credentials file and updates a target service's credentials when changes are detected.

## Key Features
- File system monitoring for credential changes
- Secure credential handling
- Service account credential updates
- Comprehensive logging and error handling
- Configurable monitoring intervals

## Architecture Guidelines
- Use dependency injection for service management
- Implement proper async/await patterns
- Follow SOLID principles
- Use structured logging with Serilog
- Implement proper error handling and retry logic
- Use configuration management for settings

## Security Considerations
- Never log sensitive credential information
- Use secure string handling for passwords
- Implement proper credential validation
- Use Windows service account with minimal required permissions
