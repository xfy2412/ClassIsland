using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Views;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ClassIsland.Core;

namespace ClassIsland.Services;

public class RestartRecoveryService : IHostedService
{
    private readonly ILogger<RestartRecoveryService> _logger;
    private readonly SettingsService _settingsService;
    private const string RecoveryListFileName = "recovery_list";

    public RestartRecoveryService(ILogger<RestartRecoveryService> logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    private string RecoveryListFilePath => Path.Combine(CommonDirectories.AppRootFolderPath, RecoveryListFileName);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = ProcessRecoveryListAsync();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task SaveRecoveryListAsync(List<RecoveryWindowInfo> windows)
    {
        try
        {
            var recoveryList = new RecoveryList { Windows = windows };
            var json = JsonConvert.SerializeObject(recoveryList, Formatting.Indented);
            await File.WriteAllTextAsync(RecoveryListFilePath, json);
            _logger.LogInformation("已保存重启恢复列表，包含 {Count} 个窗口", windows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存重启恢复列表失败");
        }
    }

    private async Task ProcessRecoveryListAsync()
    {
        if (!_settingsService.Settings.EnableRestartRecovery)
        {
            return;
        }
        
        if (!File.Exists(RecoveryListFilePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(RecoveryListFilePath);
            var recoveryList = JsonConvert.DeserializeObject<RecoveryList>(json);
            
            if (recoveryList?.Windows.Any() == true)
            {
                if (_settingsService.Settings.IsDebugEnabled)
                {
                    _logger.LogInformation("检测到重启恢复列表，准备恢复 {Count} 个窗口", recoveryList.Windows.Count);
                }
                
                foreach (var windowInfo in recoveryList.Windows)
                {
                    try
                    {
                        if (_settingsService.Settings.IsDebugEnabled)
                        {
                            _logger.LogInformation("恢复窗口: {WindowType}, URI: {Uri}", windowInfo.WindowType, windowInfo.Uri);
                        }
                        await OpenWindowFromRecoveryInfo(windowInfo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "恢复窗口失败: {WindowType}", windowInfo.WindowType);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取重启恢复列表失败");
        }
        finally
        {
            try
            {
                File.Delete(RecoveryListFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除重启恢复列表文件失败");
            }
        }
    }

    private async Task OpenWindowFromRecoveryInfo(RecoveryWindowInfo windowInfo)
    {
        await Task.Yield();
        
        if (windowInfo.WindowType == "SettingsWindow" && !string.IsNullOrWhiteSpace(windowInfo.Uri))
        {
            try
            {
                if (_settingsService.Settings.IsDebugEnabled)
                {
                    _logger.LogInformation("恢复设置窗口，页面: {PageId}", windowInfo.Uri);
                }
                var settingsWindow = App.GetService<SettingsWindowNew>();
                settingsWindow.Open(windowInfo.Uri);
                settingsWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复设置窗口失败: {PageId}", windowInfo.Uri);
            }
        }
        else if (windowInfo.WindowType == "ProfileSettingsWindow" && !string.IsNullOrWhiteSpace(windowInfo.Uri))
        {
            try
            {
                if (_settingsService.Settings.IsDebugEnabled)
                {
                    _logger.LogInformation("恢复档案编辑窗口，档案: {ProfileId}", windowInfo.Uri);
                }
                var profileSettingsWindow = App.GetService<ProfileSettingsWindow>();
                profileSettingsWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复档案编辑窗口失败: {ProfileId}", windowInfo.Uri);
            }
        }
        else if (!string.IsNullOrWhiteSpace(windowInfo.Uri))
        {
            try
            {
                var uriNavigationService = App.GetService<IUriNavigationService>();
                uriNavigationService.NavigateWrapped(new Uri(windowInfo.Uri));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复 URI 导航失败: {Uri}", windowInfo.Uri);
            }
        }
    }
}