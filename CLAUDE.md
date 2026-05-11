# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
- **Project Type**: ASP.NET Core Web API (.NET 9.0)
- **Solution Name**: BioLicense_Portal
- **Main Project**: `src/webAPI/BioLicense_Portal.csproj`
- **Architecture**: Minimal API with OpenAPI documentation support

## Development Commands
```bash
# Build the solution
dotnet build

# Run the application
dotnet run --project src/webAPI

# Run in development mode with hot reload
dotnet watch run --project src/webAPI

# Open API documentation (when running)
# Navigate to https://localhost:5001/swagger or http://localhost:5000/swagger
```

## Project Structure
```
BioLicense_Portal/
├── BioLicense_Portal.sln           # Solution file
└── src/
    └── webAPI/
        ├── BioLicense_Portal.csproj    # Main project file
        ├── Program.cs                  # Application entry point and routing
        ├── appsettings.json           # Configuration settings
        ├── appsettings.Development.json # Development-specific settings
        └── BioLicense_Portal.http     # HTTP requests file (likely for REST client)
```

## Key Components
1. **Program.cs**: Contains all routing and middleware configuration using the minimal API pattern
2. **WeatherForecast Endpoint**: The default example endpoint at `/weatherforecast`
3. **OpenAPI Support**: Automatically configured for API documentation

## Configuration
- **Target Framework**: .NET 9.0
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled (top-level usings automatically added)
- **HTTPS Redirection**: Enabled by default
- **OpenAPI**: Enabled and accessible in development environment

## Development Notes
- The project uses modern .NET minimal API pattern (no Startup.cs)
- Configuration is managed through `appsettings.json` and environment-specific files
- The API includes built-in OpenAPI/Swagger documentation
- HTTPS redirection is enabled - ensure your development environment supports HTTPS or configure to use HTTP