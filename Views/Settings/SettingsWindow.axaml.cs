using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace AegisubVersionControl.Views.Settings;

public partial class SettingsWindow : Window
{
    private AppSettings _appSettings = new AppSettings();
    public AppSettings SavedSettings => _appSettings;
    private string _filePath;

    public SettingsWindow()
    {
        AvaloniaXamlLoader.Load(this);

        ShowAddedCheckBox = this.FindControl<CheckBox>("ShowAddedCheckBox");
        ShowDeletedCheckBox = this.FindControl<CheckBox>("ShowDeletedCheckBox");
        ShowRowNumberCheckBox = this.FindControl<CheckBox>("ShowRowNumberCheckBox");
        ShowTimeCodesCheckBox = this.FindControl<CheckBox>("ShowTimeCodesCheckBox");
        SaveSettingsButton = this.FindControl<Button>("SaveSettingsButton");
        LoadDefaultsButton = this.FindControl<Button>("LoadDefaultsButton");

        SaveSettingsButton.Click += SaveSettingsButton_Click;
        LoadDefaultsButton.Click += LoadDefaultsButton_Click;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _filePath = Path.Combine(appData, "AegisubVersionControl", "AppSettings.json");

        var settingsDir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }

        PutDefaults();

        if (File.Exists(_filePath))
        {
            LoadSettings();
        }
    }

    private void PutDefaults()
    {
        _appSettings = new AppSettings
        {
            ShowAdded = false,
            ShowDeleted = false,
            ShowIndexes = true,
            ShowTimeCodes = false
        };

        PutSettings();
    }

    private void PutSettings()
    {
        try
        {
            if (ShowAddedCheckBox != null)
                ShowAddedCheckBox.IsChecked = _appSettings.ShowAdded;

            if (ShowDeletedCheckBox != null)
                ShowDeletedCheckBox.IsChecked = _appSettings.ShowDeleted;

            if (ShowRowNumberCheckBox != null)
                ShowRowNumberCheckBox.IsChecked = _appSettings.ShowIndexes;

            if (ShowTimeCodesCheckBox != null)
                ShowTimeCodesCheckBox.IsChecked = _appSettings.ShowTimeCodes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying settings to UI: {ex}");
        }
    }

    private void LoadSettings()
    {
        try
        {
            string jsonString = File.ReadAllText(_filePath);
            _appSettings = JsonConvert.DeserializeObject<AppSettings>(jsonString) ?? new AppSettings();
            PutSettings();
        }
        catch (Exception ex)
        {
            PutDefaults();
        }
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _appSettings = new AppSettings
            {
                ShowAdded = ShowAddedCheckBox?.IsChecked ?? false,
                ShowDeleted = ShowDeletedCheckBox?.IsChecked ?? false,
                ShowIndexes = ShowRowNumberCheckBox?.IsChecked ?? true,
                ShowTimeCodes = ShowTimeCodesCheckBox?.IsChecked ?? false
            };

            string jsonString = JsonConvert.SerializeObject(_appSettings, Formatting.Indented);
            File.WriteAllText(_filePath, jsonString);
            Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex}");
        }
    }

    private void LoadDefaultsButton_Click(object sender, RoutedEventArgs e) => PutDefaults();
}
