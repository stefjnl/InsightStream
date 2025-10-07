# InsightStream Application Tests

## Configuration Setup

This test project uses a comprehensive configuration approach that supports multiple sources for API keys and settings.

### Configuration Loading Order

The tests load configuration in the following order (later sources override earlier ones):

1. `appsettings.json` (base configuration)
2. `appsettings.Test.json` (test-specific configuration)
3. `appsettings.Development.json` (development overrides)
4. User Secrets (for sensitive data like API keys)
5. Environment Variables (prefixed with `INSIGHTSTREAM_`)

### Setting Up API Keys

#### Option 1: User Secrets (Recommended for Development)

Use .NET's built-in User Secrets to store sensitive API keys:

```bash
# Set OpenRouter API key
dotnet user-secrets set "Providers:OpenRouter:ApiKey" "your-openrouter-api-key-here"

# Set NanoGPT API key
dotnet user-secrets set "Providers:NanoGPT:ApiKey" "your-nanogpt-api-key-here"
```

#### Option 2: Environment Variables

Set environment variables with the `INSIGHTSTREAM_` prefix:

```bash
# Windows PowerShell
$env:INSIGHTSTREAM_Providers__OpenRouter__ApiKey = "your-openrouter-api-key-here"
$env:INSIGHTSTREAM_Providers__NanoGPT__ApiKey = "your-nanogpt-api-key-here"

# Windows Command Prompt
set INSIGHTSTREAM_Providers__OpenRouter__ApiKey=your-openrouter-api-key-here
set INSIGHTSTREAM_Providers__NanoGPT__ApiKey=your-nanogpt-api-key-here

# Bash/Linux
export INSIGHTSTREAM_Providers__OpenRouter__ApiKey="your-openrouter-api-key-here"
export INSIGHTSTREAM_Providers__NanoGPT__ApiKey="your-nanogpt-api-key-here"
```

### Configuration Files

#### appsettings.Test.json (Safe to commit)
Contains non-sensitive configuration that can be safely committed to source control:
- Provider endpoints
- Model configurations
- Non-sensitive settings

#### appsettings.Development.json (Contains placeholders)
Contains the same structure as the test file but with placeholder values for API keys.
**Never commit real API keys to this file!**

### Running Tests

The tests will automatically load configuration from the available sources. If no API keys are configured, some tests may be skipped or use mock services.

### Best Practices

1. **Never commit API keys** to source control
2. **Use User Secrets** for local development
3. **Use Environment Variables** for CI/CD pipelines
4. **Use test/sandbox API keys** when available
5. **Mock external dependencies** for unit tests

### Troubleshooting

If tests fail due to missing configuration:

1. Check that `appsettings.Test.json` exists and has valid JSON
2. Verify API keys are set via User Secrets or Environment Variables
3. Ensure the test project can access the configuration files
4. Check that provider endpoints are reachable

### Example User Secrets Setup

```json
{
  "Providers": {
    "OpenRouter": {
      "ApiKey": "sk-or-v1-your-actual-key-here"
    },
    "NanoGPT": {
      "ApiKey": "your-actual-nanogpt-key-here"
    }
  }
}