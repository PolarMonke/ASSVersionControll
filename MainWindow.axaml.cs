using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AegisubVersionControl.Views.Settings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

namespace AegisubVersionControl;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    string? _translated;
    string? _edited;

    List<SubtitleEntry>? _translatedLines;
    List<SubtitleEntry>? _editedLines;

    AppSettings _currentSettings = new AppSettings();

    private async void LoadTranslatedButton_Click(object sender, RoutedEventArgs e)
    {
        _translated = await FileLoader.LoadAssFileAsync("Load Translated ASS File", this);
        if (_translated != null)
        {
            LoadTranslatedButton.Background = new SolidColorBrush(Avalonia.Media.Color.Parse("#F28BEF"));
        }
    }

    private async void LoadEditedButton_Click(object sender, RoutedEventArgs e)
    {
        _edited = await FileLoader.LoadAssFileAsync("Load Translated ASS File", this);
        if (_edited != null)
        {
            LoadEditedButton.Background = new SolidColorBrush(Avalonia.Media.Color.Parse("#F28BEF"));
        }
    }

    private void FindChangesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_translated != null && _edited != null)
        {
            _translatedLines = SubsParser.ParseSubtitleData(_translated);
            _editedLines = SubsParser.ParseSubtitleData(_edited);
            List<SubtitleChange> changes = ChangesFinder.FindChanges(_translatedLines, _editedLines, _currentSettings);
            string changesReport = ChangesReporter.GetChangeReport(changes, _currentSettings);
            ChangesDisplay.Text = changesReport;
        }
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        await settings.ShowDialog(this);

        var updatedSettings = settings.SavedSettings;

        _currentSettings = updatedSettings;
        ChangesDisplay.SelectedDictionary = _currentSettings.DictionaryLink;
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(ChangesDisplay.Text);
        }
    }
}