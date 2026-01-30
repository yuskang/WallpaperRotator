using WallpaperRotator.Core.Entities;

namespace WallpaperRotator.Core.Interfaces;

/// <summary>
/// 配置儲存介面
/// </summary>
public interface IConfigurationStore
{
    /// <summary>
    /// 載入配置
    /// </summary>
    Task<AppConfiguration> LoadAsync();

    /// <summary>
    /// 儲存配置
    /// </summary>
    Task SaveAsync(AppConfiguration configuration);

    /// <summary>
    /// 配置變更事件
    /// </summary>
    event EventHandler<AppConfiguration>? ConfigurationChanged;

    /// <summary>
    /// 重設為預設值
    /// </summary>
    Task ResetToDefaultAsync();

    /// <summary>
    /// 匯出配置
    /// </summary>
    Task ExportAsync(string filePath);

    /// <summary>
    /// 匯入配置
    /// </summary>
    Task<AppConfiguration> ImportAsync(string filePath);

    /// <summary>
    /// 配置檔案路徑
    /// </summary>
    string ConfigFilePath { get; }
}
