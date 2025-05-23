﻿using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flow.Launcher.Plugin.ShortcutPlugin.App.Contracts.Services;
using Flow.Launcher.Plugin.ShortcutPlugin.App.Contracts.ViewModels;
using Flow.Launcher.Plugin.ShortcutPlugin.App.Helpers;
using Flow.Launcher.Plugin.ShortcutPlugin.Common.Helper;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using CommonConstants = Flow.Launcher.Plugin.ShortcutPlugin.Common.Helper.Constants;
using Constants = Flow.Launcher.Plugin.ShortcutPlugin.App.Helpers.Constants;

namespace Flow.Launcher.Plugin.ShortcutPlugin.App.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IShortcutsService _shortcutsService;
    private readonly IVariablesService _variablesService;

    [ObservableProperty]
    public partial ElementTheme CurrentTheme
    {
        get; set;
    }

    [ObservableProperty]
    public partial string VersionDescription
    {
        get; set;
    }

    private int _themeIndex;

    public int ThemeIndex
    {
        get => _themeIndex;
        set
        {
            if (_themeIndex == value)
            {
                return;
            }

            _themeIndex = value;
            SwitchThemeCommand.Execute((ElementTheme)value);
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    public partial string ShortcutsPath
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string VariablesPath
    {
        get; set;
    } = string.Empty;


    public bool IsDevMode =>
#if DEBUG
            true;
#else
            false;
#endif


    public SettingsViewModel(IThemeSelectorService themeSelectorService, ILocalSettingsService localSettingsService, IShortcutsService shortcutsService, IVariablesService variablesService)
    {
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _shortcutsService = shortcutsService;
        _variablesService = variablesService;

        VersionDescription = GetVersionDescription();

        CurrentTheme = _themeSelectorService.Theme;
        ThemeIndex = (int)_themeSelectorService.Theme;
    }

    public async Task OnNavigatedTo(object parameter)
    {
        var pluginDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.FullName;
        var shortcutsPath = ShortcutUtilities.GetShortcutsPath(pluginDirectory);

        if (!string.IsNullOrEmpty(shortcutsPath))
        {
            ShortcutsPath = shortcutsPath;
        }

        var variablesPath = VariableUtilities.GetVariablesPath(pluginDirectory);

        if (!string.IsNullOrEmpty(variablesPath))
        {
            VariablesPath = variablesPath;
        }
    }
    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }

    public async Task SetShortcutsPath(string path)
    {
        ShortcutsPath = path;

        await _localSettingsService.SaveSettingAsync(Constants.ShortcutPathKey, path);

        await _shortcutsService.RefreshShortcutsAsync();
    }

    public async Task SetVariablesPath(string path)
    {
        VariablesPath = path;

        await _localSettingsService.SaveSettingAsync(Constants.VariablesPathKey, path);

        await _variablesService.RefreshVariablesAsync();
    }

    [RelayCommand]
    private async Task SwitchTheme(ElementTheme theme)
    {
        if (CurrentTheme == theme)
        {
            return;
        }

        CurrentTheme = theme;
        await _themeSelectorService.SetThemeAsync(theme);
    }


    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
