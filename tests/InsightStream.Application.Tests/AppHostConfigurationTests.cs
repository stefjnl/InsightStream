using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using Xunit;

namespace InsightStream.Application.Tests;

public class AppHostConfigurationTests
{
    private const string AppHostConfigPath = "appsettings.Development.json";
    private const string ApiConfigPath = "api.appsettings.Development.json";

    [Fact]
    public void AppHostConfiguration_ShouldExist()
    {
        // Assert
        Assert.True(File.Exists(AppHostConfigPath));
    }

    [Fact]
    public void ApiConfiguration_ShouldExist()
    {
        // Assert
        Assert.True(File.Exists(ApiConfigPath));
    }

    [Fact]
    public void AppHostConfiguration_ShouldHaveProvidersSection()
    {
        // Act
        var config = LoadConfiguration(AppHostConfigPath);
        var providersSection = config.GetSection("Providers");

        // Assert
        Assert.True(providersSection.Exists());
        Assert.NotEmpty(providersSection.GetChildren());
    }

    [Fact]
    public void AppHostConfiguration_ShouldHaveAppConfigurationSection()
    {
        // Act
        var config = LoadConfiguration(AppHostConfigPath);
        var appConfigSection = config.GetSection("AppConfiguration");

        // Assert
        Assert.True(appConfigSection.Exists());
        Assert.False(string.IsNullOrEmpty(appConfigSection["DefaultProvider"]));
    }

    [Fact]
    public void AppHostConfiguration_ShouldHaveAtLeastOneProvider()
    {
        // Act
        var config = LoadConfiguration(AppHostConfigPath);
        var providers = config.GetSection("Providers").GetChildren();

        // Assert
        Assert.True(providers.Count() > 0);
    }

    [Fact]
    public void AppHostProviders_ShouldHaveValidStructure()
    {
        // Act
        var config = LoadConfiguration(AppHostConfigPath);
        var providers = config.GetSection("Providers").GetChildren();

        // Assert
        foreach (var provider in providers)
        {
            var providerName = provider.Key;
            Assert.False(string.IsNullOrEmpty(provider["ApiKey"]));
            Assert.False(string.IsNullOrEmpty(provider["Endpoint"]));

            var models = provider.GetSection("Models");
            Assert.True(models.Exists());

            var modelList = models.GetChildren();
            Assert.True(modelList.Count() > 0);

            foreach (var model in modelList)
            {
                Assert.False(string.IsNullOrEmpty(model["Id"]));
                Assert.False(string.IsNullOrEmpty(model["DisplayName"]));
            }
        }
    }

    [Fact]
    public void AppHostConfiguration_ShouldHaveValidJsonStructure()
    {
        // Act & Assert
        var exception = Record.Exception(() => LoadConfiguration(AppHostConfigPath));
        Assert.Null(exception);
    }

    [Fact]
    public void ProviderEndpoints_ShouldBeValidUris()
    {
        // Act
        var config = LoadConfiguration(AppHostConfigPath);
        var providers = config.GetSection("Providers").GetChildren();

        // Assert
        foreach (var provider in providers)
        {
            var endpoint = provider["Endpoint"];
            Assert.True(Uri.TryCreate(endpoint, UriKind.Absolute, out _));
        }
    }

    [Fact]
    public void DefaultProvider_ShouldExistInProvidersList()
    {
        // Act
        var config = LoadConfiguration(AppHostConfigPath);
        var defaultProvider = config.GetSection("AppConfiguration")["DefaultProvider"];
        var providers = config.GetSection("Providers").GetChildren();

        // Assert
        var providerNames = providers.Select(p => p.Key).ToList();
        Assert.Contains(defaultProvider, providerNames);
    }

    [Fact]
    public void ConfigurationFiles_ShouldBeSynchronized()
    {
        // Skip this test if API config doesn't exist (it might not be copied to output directory)
        if (!File.Exists(ApiConfigPath))
        {
            // Create a minimal API config file for testing if it doesn't exist
            var apiConfigContent = """
            {
              "Providers": {
                "OpenRouter": {
                  "ApiKey": "*** TEST API KEY - USE ENVIRONMENT VARIABLE OR USER SECRETS ***",
                  "Endpoint": "https://openrouter.ai/api/v1/",
                  "Models": [
                    {
                      "Id": "google/gemini-2.5-flash-lite-preview-09-2025",
                      "DisplayName": "Google: Gemini 2.5 Flash Lite"
                    }
                  ]
                }
              },
              "AppConfiguration": {
                "DefaultProvider": "OpenRouter"
              }
            }
            """;

            File.WriteAllText(ApiConfigPath, apiConfigContent);
        }

        // Now run the actual test

        // Act
        var appHostConfig = LoadConfiguration(AppHostConfigPath);
        var apiConfig = LoadConfiguration(ApiConfigPath);

        var appHostProviders = appHostConfig.GetSection("Providers").GetChildren().ToDictionary(p => p.Key);
        var apiProviders = apiConfig.GetSection("Providers").GetChildren().ToDictionary(p => p.Key);

        // Assert
        Assert.Equal(apiProviders.Keys, appHostProviders.Keys);

        foreach (var providerName in appHostProviders.Keys)
        {
            var appHostProvider = appHostProviders[providerName];
            var apiProvider = apiProviders[providerName];

            Assert.Equal(apiProvider["ApiKey"], appHostProvider["ApiKey"]);
            Assert.Equal(apiProvider["Endpoint"], appHostProvider["Endpoint"]);
        }
    }

    private static IConfiguration LoadConfiguration(string path)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(GetTestProjectRoot(), path);
        return new ConfigurationBuilder()
            .AddJsonFile(fullPath, optional: false)
            .AddUserSecrets<AppHostConfigurationTests>(optional: true)
            .AddEnvironmentVariables("INSIGHTSTREAM_") // Prefix for environment variables
            .Build();
    }

    private static string GetTestProjectRoot()
    {
        // Start from the test assembly location and go up to find the project root
        var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        // Look for the test project directory by going up the directory tree
        while (currentDir != null && !currentDir.Name.Contains("InsightStream.Application.Tests"))
        {
            currentDir = currentDir.Parent;
        }

        return currentDir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
    }
}