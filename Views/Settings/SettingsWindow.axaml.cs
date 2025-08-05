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
        ShowCommentsCheckBox = this.FindControl<CheckBox>("ShowCommentsCheckBox");
        ShowRowNumberCheckBox = this.FindControl<CheckBox>("ShowRowNumberCheckBox");
        ShowTimeCodesCheckBox = this.FindControl<CheckBox>("ShowTimeCodesCheckBox");
        UnderlineChangesCheckBox = this.FindControl<CheckBox>("UnderlineChangesCheckBox");
        SaveSettingsButton = this.FindControl<Button>("SaveSettingsButton");
        LoadDefaultsButton = this.FindControl<Button>("LoadDefaultsButton");
        DictionariesComboBox = this.FindControl<ComboBox>("DictionariesComboBox");

        DictionariesComboBox.ItemsSource = Enum.GetValues(typeof(Dictionaries));

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
            ShowComments = true,
            ShowIndexes = false,
            ShowTimeCodes = true,
            UnderlineChanges = true,
            DictionaryLink = Dictionaries.Vivy
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

            if (ShowCommentsCheckBox != null)
                ShowCommentsCheckBox.IsChecked = _appSettings.ShowComments;

            if (ShowRowNumberCheckBox != null)
                ShowRowNumberCheckBox.IsChecked = _appSettings.ShowIndexes;

            if (ShowTimeCodesCheckBox != null)
                ShowTimeCodesCheckBox.IsChecked = _appSettings.ShowTimeCodes;

            if (UnderlineChangesCheckBox != null)
                UnderlineChangesCheckBox.IsChecked = _appSettings.UnderlineChanges;

            if (DictionariesComboBox != null)
                DictionariesComboBox.SelectedItem = _appSettings.DictionaryLink;
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
                ShowComments = ShowCommentsCheckBox?.IsChecked ?? true,
                ShowIndexes = ShowRowNumberCheckBox?.IsChecked ?? true,
                ShowTimeCodes = ShowTimeCodesCheckBox?.IsChecked ?? false,
                UnderlineChanges = UnderlineChangesCheckBox?.IsChecked ?? true,
                DictionaryLink = (Dictionaries)(DictionariesComboBox?.SelectedItem ?? Dictionaries.Vivy)
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
