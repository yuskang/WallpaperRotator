using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WallpaperRotator.Core.Entities;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Infrastructure.Storage;

/// <summary>
/// JSON 配置儲存實現
/// </summary>
public sealed class JsonConfigurationStore : IConfigurationStore
{
    private readonly ILogger<JsonConfigurationStore> _logger;
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    private AppConfiguration? _cachedConfig;

    public string ConfigFilePath => _configFilePath;

    public event EventHandler<AppConfiguration>? ConfigurationChanged;

    public JsonConfigurationStore(ILogger<JsonConfigurationStore> logger)
    {
        _logger = logger;

        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WallpaperRotator");

        _configFilePath = Path.Combine(_configDirectory, "config.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
            _logger.LogInformation("Created config directory: {Path}", _configDirectory);
        }
    }

    public async Task<AppConfiguration> LoadAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogInformation("Config file not found, creating default configuration");
                var defaultConfig = AppConfiguration.CreateDefault();
                await SaveAsync(defaultConfig);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);

            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize config, using default");
                return AppConfiguration.CreateDefault();
            }

            _cachedConfig = config;
            _logger.LogDebug("Configuration loaded from {Path}", _configFilePath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            return AppConfiguration.CreateDefault();
        }
    }

    public async Task SaveAsync(AppConfiguration configuration)
    {
        try
        {
            // 備份現有配置
            if (File.Exists(_configFilePath))
            {
                var backupPath = _configFilePath + ".backup";
                File.Copy(_configFilePath, backupPath, overwrite: true);
            }

            var json = JsonSerializer.Serialize(configuration, _jsonOptions);
            await File.WriteAllTextAsync(_configFilePath, json);

            _cachedConfig = configuration;
            _logger.LogDebug("Configuration saved to {Path}", _configFilePath);

            ConfigurationChanged?.Invoke(this, configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            throw;
        }
    }

    public async Task ResetToDefaultAsync()
    {
        var defaultConfig = AppConfiguration.CreateDefault();
        await SaveAsync(defaultConfig);
        _logger.LogInformation("Configuration reset to default");
    }

    public async Task ExportAsync(string filePath)
    {
        var config = await LoadAsync();
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        _logger.LogInformation("Configuration exported to {Path}", filePath);
    }

    public async Task<AppConfiguration> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Import file not found", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);
        var config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);

        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize imported configuration");
        }

        await SaveAsync(config);
        _logger.LogInformation("Configuration imported from {Path}", filePath);
        return config;
    }
}
