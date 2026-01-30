using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WallpaperRotator.Core.Entities;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Infrastructure.Storage;

/// <summary>
/// 配置驗證結果
/// </summary>
public sealed class ConfigurationValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();

    public static ConfigurationValidationResult Success() => new() { IsValid = true };

    public static ConfigurationValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// JSON 配置儲存實現 - 支援配置驗證、錯誤恢復和版本遷移
/// </summary>
public sealed class JsonConfigurationStore : IConfigurationStore
{
    private readonly ILogger<JsonConfigurationStore> _logger;
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private readonly string _legacyConfigPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private AppConfiguration? _cachedConfig;
    private const string CurrentVersion = "2.0";
    private const int MaxBackupCount = 5;

    public string ConfigFilePath => _configFilePath;
    public string ConfigDirectory => _configDirectory;

    public event EventHandler<AppConfiguration>? ConfigurationChanged;

    public JsonConfigurationStore(ILogger<JsonConfigurationStore> logger)
    {
        _logger = logger;

        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WallpaperRotator");

        _configFilePath = Path.Combine(_configDirectory, "config.json");
        _legacyConfigPath = Path.Combine(_configDirectory, "config.ps1");

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
        try
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
                _logger.LogInformation("Created config directory: {Path}", _configDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create config directory: {Path}", _configDirectory);
            throw new InvalidOperationException($"Cannot create configuration directory: {_configDirectory}", ex);
        }
    }

    /// <summary>
    /// 驗證配置有效性
    /// </summary>
    public ConfigurationValidationResult ValidateConfiguration(AppConfiguration config)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 驗證版本
        if (string.IsNullOrEmpty(config.Version))
        {
            warnings.Add("Configuration version is missing, defaulting to current version");
        }

        // 驗證一般設定
        if (config.Settings == null)
        {
            errors.Add("General settings section is missing");
        }
        else
        {
            // 驗證過渡時間
            if (config.Settings.TransitionDurationMs < 0 || config.Settings.TransitionDurationMs > 5000)
            {
                warnings.Add($"Transition duration {config.Settings.TransitionDurationMs}ms is out of recommended range (0-5000ms)");
            }

            // 驗證背景顏色格式
            if (!string.IsNullOrEmpty(config.Settings.BackgroundColor) &&
                !Regex.IsMatch(config.Settings.BackgroundColor, @"^#[0-9A-Fa-f]{6}$"))
            {
                errors.Add($"Invalid background color format: {config.Settings.BackgroundColor}. Expected format: #RRGGBB");
            }
        }

        // 驗證桌布設定
        if (config.Wallpapers != null)
        {
            ValidateOrientationWallpapers(config.Wallpapers.Landscape, "Landscape", errors, warnings);
            ValidateOrientationWallpapers(config.Wallpapers.Portrait, "Portrait", errors, warnings);
        }

        // 驗證排程設定
        if (config.Schedule?.Enabled == true && (config.Schedule.Rules == null || config.Schedule.Rules.Count == 0))
        {
            warnings.Add("Schedule is enabled but no rules are defined");
        }

        return new ConfigurationValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private void ValidateOrientationWallpapers(OrientationWallpapers? wallpapers, string orientation,
        List<string> errors, List<string> warnings)
    {
        if (wallpapers == null) return;

        foreach (var imagePath in wallpapers.Images)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                warnings.Add($"{orientation}: Empty image path found");
                continue;
            }

            if (!File.Exists(imagePath))
            {
                warnings.Add($"{orientation}: Image file not found: {imagePath}");
            }
        }

        if (wallpapers.RotateIntervalSeconds < 0)
        {
            errors.Add($"{orientation}: Rotation interval cannot be negative");
        }

        if (wallpapers.CurrentIndex < 0)
        {
            wallpapers.CurrentIndex = 0;
            warnings.Add($"{orientation}: Current index was negative, reset to 0");
        }
        else if (wallpapers.Images.Count > 0 && wallpapers.CurrentIndex >= wallpapers.Images.Count)
        {
            wallpapers.CurrentIndex = 0;
            warnings.Add($"{orientation}: Current index exceeded image count, reset to 0");
        }
    }

    public async Task<AppConfiguration> LoadAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            return await LoadAsyncInternal();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<AppConfiguration> LoadAsyncInternal()
    {
        try
        {
            // 檢查是否需要從 v1 PowerShell 配置遷移
            if (!File.Exists(_configFilePath) && File.Exists(_legacyConfigPath))
            {
                _logger.LogInformation("Found legacy PowerShell configuration, attempting migration...");
                var migratedConfig = await MigrateFromPowerShellAsync();
                if (migratedConfig != null)
                {
                    await SaveAsyncInternal(migratedConfig);
                    return migratedConfig;
                }
            }

            if (!File.Exists(_configFilePath))
            {
                _logger.LogInformation("Config file not found, creating default configuration");
                var defaultConfig = AppConfiguration.CreateDefault();
                await SaveAsyncInternal(defaultConfig);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Config file is empty, using default configuration");
                return await RecoverFromBackupOrDefaultAsync();
            }

            AppConfiguration? config;
            try
            {
                config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to parse configuration JSON");
                return await RecoverFromBackupOrDefaultAsync();
            }

            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize config, attempting recovery");
                return await RecoverFromBackupOrDefaultAsync();
            }

            // 版本遷移
            config = MigrateConfigurationVersion(config);

            // 驗證配置
            var validationResult = ValidateConfiguration(config);
            foreach (var warning in validationResult.Warnings)
            {
                _logger.LogWarning("Configuration warning: {Warning}", warning);
            }

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogError("Configuration error: {Error}", error);
                }
                _logger.LogWarning("Configuration has errors, attempting to fix...");
                config = SanitizeConfiguration(config);
            }

            _cachedConfig = config;
            _logger.LogDebug("Configuration loaded from {Path}", _configFilePath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading configuration");
            return await RecoverFromBackupOrDefaultAsync();
        }
    }

    /// <summary>
    /// 從 v1 PowerShell 腳本遷移配置
    /// </summary>
    private async Task<AppConfiguration?> MigrateFromPowerShellAsync()
    {
        try
        {
            var psContent = await File.ReadAllTextAsync(_legacyConfigPath);
            var config = AppConfiguration.CreateDefault();

            // 解析 PowerShell 配置
            // 範例格式: $LandscapeWallpaper = "C:\path\to\image.jpg"
            var landscapeMatch = Regex.Match(psContent, @"\$LandscapeWallpaper\s*=\s*[""'](.+?)[""']");
            if (landscapeMatch.Success)
            {
                config.Wallpapers.Landscape.Images.Add(landscapeMatch.Groups[1].Value);
            }

            var portraitMatch = Regex.Match(psContent, @"\$PortraitWallpaper\s*=\s*[""'](.+?)[""']");
            if (portraitMatch.Success)
            {
                config.Wallpapers.Portrait.Images.Add(portraitMatch.Groups[1].Value);
            }

            // 解析檢查間隔
            var intervalMatch = Regex.Match(psContent, @"\$CheckInterval\s*=\s*(\d+)");
            if (intervalMatch.Success && int.TryParse(intervalMatch.Groups[1].Value, out var interval))
            {
                // v1 使用秒為單位
                config.Wallpapers.Landscape.RotateIntervalSeconds = interval;
                config.Wallpapers.Portrait.RotateIntervalSeconds = interval;
            }

            // 備份舊配置
            var backupPath = _legacyConfigPath + ".migrated";
            File.Move(_legacyConfigPath, backupPath);

            _logger.LogInformation("Successfully migrated configuration from PowerShell v1");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate from PowerShell configuration");
            return null;
        }
    }

    /// <summary>
    /// 版本遷移處理
    /// </summary>
    private AppConfiguration MigrateConfigurationVersion(AppConfiguration config)
    {
        if (config.Version == CurrentVersion)
        {
            return config;
        }

        _logger.LogInformation("Migrating configuration from version {OldVersion} to {NewVersion}",
            config.Version ?? "unknown", CurrentVersion);

        // v1.x -> v2.0 遷移邏輯
        if (string.IsNullOrEmpty(config.Version) || config.Version.StartsWith("1."))
        {
            // 確保新欄位有預設值
            config.Settings ??= new GeneralSettings();
            config.Wallpapers ??= new WallpaperSettings();
            config.Schedule ??= new ScheduleSettings();

            // 設定預設過渡時間
            if (config.Settings.TransitionDurationMs == 0)
            {
                config.Settings.TransitionDurationMs = 200;
            }
        }

        config.Version = CurrentVersion;
        return config;
    }

    /// <summary>
    /// 修復無效配置
    /// </summary>
    private AppConfiguration SanitizeConfiguration(AppConfiguration config)
    {
        config.Settings ??= new GeneralSettings();
        config.Wallpapers ??= new WallpaperSettings();
        config.Wallpapers.Landscape ??= new OrientationWallpapers();
        config.Wallpapers.Portrait ??= new OrientationWallpapers();
        config.Schedule ??= new ScheduleSettings();

        // 修復無效的背景顏色
        if (!Regex.IsMatch(config.Settings.BackgroundColor ?? "", @"^#[0-9A-Fa-f]{6}$"))
        {
            config.Settings.BackgroundColor = "#000000";
        }

        // 修復過渡時間
        config.Settings.TransitionDurationMs = Math.Clamp(config.Settings.TransitionDurationMs, 0, 5000);

        // 移除不存在的圖片路徑
        config.Wallpapers.Landscape.Images = config.Wallpapers.Landscape.Images
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .ToList();
        config.Wallpapers.Portrait.Images = config.Wallpapers.Portrait.Images
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .ToList();

        return config;
    }

    /// <summary>
    /// 從備份恢復或使用預設配置
    /// </summary>
    private async Task<AppConfiguration> RecoverFromBackupOrDefaultAsync()
    {
        var backupPath = _configFilePath + ".backup";

        if (File.Exists(backupPath))
        {
            try
            {
                _logger.LogInformation("Attempting to recover from backup: {Path}", backupPath);
                var backupJson = await File.ReadAllTextAsync(backupPath);
                var config = JsonSerializer.Deserialize<AppConfiguration>(backupJson, _jsonOptions);

                if (config != null)
                {
                    _logger.LogInformation("Successfully recovered configuration from backup");
                    await SaveAsyncInternal(config);
                    return config;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover from backup");
            }
        }

        _logger.LogWarning("Using default configuration");
        var defaultConfig = AppConfiguration.CreateDefault();
        await SaveAsyncInternal(defaultConfig);
        return defaultConfig;
    }

    public async Task SaveAsync(AppConfiguration configuration)
    {
        await _fileLock.WaitAsync();
        try
        {
            await SaveAsyncInternal(configuration);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task SaveAsyncInternal(AppConfiguration configuration)
    {
        try
        {
            // 驗證配置
            var validationResult = ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Saving configuration with validation errors: {Errors}",
                    string.Join("; ", validationResult.Errors));
            }

            // 輪替備份
            await RotateBackupsAsync();

            // 備份現有配置
            if (File.Exists(_configFilePath))
            {
                var backupPath = _configFilePath + ".backup";
                File.Copy(_configFilePath, backupPath, overwrite: true);
            }

            // 確保版本號正確
            configuration.Version = CurrentVersion;

            var json = JsonSerializer.Serialize(configuration, _jsonOptions);

            // 使用臨時檔案寫入，確保原子性
            var tempPath = _configFilePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _configFilePath, overwrite: true);

            _cachedConfig = configuration;
            _logger.LogDebug("Configuration saved to {Path}", _configFilePath);

            ConfigurationChanged?.Invoke(this, configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            throw new InvalidOperationException("Failed to save configuration", ex);
        }
    }

    /// <summary>
    /// 輪替備份檔案
    /// </summary>
    private async Task RotateBackupsAsync()
    {
        try
        {
            var backupFiles = Directory.GetFiles(_configDirectory, "config.json.backup.*")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToList();

            // 保留最新的備份
            while (backupFiles.Count >= MaxBackupCount)
            {
                var oldestBackup = backupFiles.Last();
                File.Delete(oldestBackup);
                backupFiles.RemoveAt(backupFiles.Count - 1);
                _logger.LogDebug("Deleted old backup: {Path}", oldestBackup);
            }

            // 如果存在主備份，重命名為時間戳備份
            var mainBackup = _configFilePath + ".backup";
            if (File.Exists(mainBackup))
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var newBackupPath = $"{_configFilePath}.backup.{timestamp}";
                File.Move(mainBackup, newBackupPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to rotate backups");
        }

        await Task.CompletedTask;
    }

    public async Task ResetToDefaultAsync()
    {
        var defaultConfig = AppConfiguration.CreateDefault();
        await SaveAsync(defaultConfig);
        _logger.LogInformation("Configuration reset to default");
    }

    public async Task ExportAsync(string filePath)
    {
        try
        {
            var config = await LoadAsync();
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Configuration exported to {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export configuration to {Path}", filePath);
            throw;
        }
    }

    public async Task<AppConfiguration> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Import file not found", filePath);
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize imported configuration");
            }

            // 驗證導入的配置
            var validationResult = ValidateConfiguration(config);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Imported configuration is invalid: {string.Join("; ", validationResult.Errors)}");
            }

            await SaveAsync(config);
            _logger.LogInformation("Configuration imported from {Path}", filePath);
            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse imported configuration");
            throw new InvalidOperationException("Invalid JSON format in imported file", ex);
        }
    }

    /// <summary>
    /// 獲取快取的配置（非同步載入的快取版本）
    /// </summary>
    public AppConfiguration? GetCachedConfiguration() => _cachedConfig;
}
