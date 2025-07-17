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
    string _translated;
    string _edited;

    List<SubtitleEntry> _translatedLines;
    List<SubtitleEntry> _editedLines;

    AppSettings _currentSettings = new AppSettings();

    private async Task<string?> LoadAssFileAsync(string title)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return null;

            var assFileType = new FilePickerFileType("ASS/SSA Files")
            {
                Patterns = new[] { "*.ass", "*.ssa" }
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { assFileType }
            });

            if (files.Count == 0) return null;

            await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            return new(await streamReader.ReadToEndAsync());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file: {ex.Message}");
            return null;
        }
    }

    public List<SubtitleEntry> ParseSubtitleData(string fileContent)
    {
        var entries = new List<SubtitleEntry>();
        if (fileContent == null) return entries;

        string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        bool isSubsSection = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("[Events]"))
            {
                isSubsSection = true;
                continue;
            }
            if (isSubsSection && line.StartsWith("Dialogue:"))
            {
                var entry = ParseDialogueLine(line);
                if (entry != null)
                    entries.Add(entry);
            }
        }
        return entries;
    }
    private SubtitleEntry ParseDialogueLine(string line)
    {
        try
        {
            string[] parts = line.Split(new[] { ',' }, 10);
            if (parts.Length < 10) return null;

            string startTime = parts[1].Trim();
            string endTime = parts[2].Trim();

            string? textWithComment = parts.Length >= 10 ? parts[9].Trim() : null;

            string? comment = null;
            string? text = null;

            if (textWithComment != null)
            {
                var commentBuilder = new StringBuilder();
                var matches = Regex.Matches(textWithComment, @"\{.*?\}");
                foreach (Match match in matches)
                {
                    commentBuilder.Append(match.Value);
                }
                comment = commentBuilder.Length > 0 ? commentBuilder.ToString() : null;

                text = Regex.Replace(textWithComment, @"\{.*?\}", "").Trim();
            }
            return new SubtitleEntry
            {
                StartTime = startTime,
                EndTime = endTime,
                Content = text,
                Comment = comment
            };

        }
        catch (System.Exception)
        {
            return null;
        }
    }
    private bool TimecodesMatch(SubtitleEntry a, SubtitleEntry b)
    {
        return ParseTime(a.StartTime) == ParseTime(b.StartTime) &&
            ParseTime(a.EndTime) == ParseTime(b.EndTime);
    }
    private TimeSpan ParseTime(string timecode)
    {
        try
        {
            timecode = timecode.Trim()
                            .Replace('.', ':')
                            .Replace(',', ':');
            var parts = timecode.Split(':');
            if (parts.Length == 2) timecode = $"0:{timecode}";
            if (parts.Length > 3) timecode = string.Join(":", parts.Take(3));

            return TimeSpan.Parse(timecode);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }
    private bool IsEarlier(SubtitleEntry a, SubtitleEntry b)
    {
        return ParseTime(a.StartTime) < ParseTime(b.StartTime);
    }
    public List<SubtitleChange> FindChanges(List<SubtitleEntry> transLines, List<SubtitleEntry> edLines)
    {
        List<SubtitleChange> changes = new List<SubtitleChange>();
        int transIndex = 0;
        int edIndex = 0;

        while (transIndex < transLines.Count || edIndex < edLines.Count)
        {
            var transLine = transIndex < transLines.Count ? transLines[transIndex] : null;
            var edLine = edIndex < edLines.Count ? edLines[edIndex] : null;

            if (transLine != null && edLine != null)
            {
                if (TimecodesMatch(transLine, edLine))
                {
                    if (transLine.Content != edLine.Content)
                    {
                        changes.Add(
                            new SubtitleChange
                            {
                                Type = ChangeType.Modified,
                                Translated = transLine,
                                Edited = edLine,
                                ApproxPosition = transIndex
                            }
                        );
                    }
                    transIndex++;
                    edIndex++;
                }
                else if (IsEarlier(edLine, transLine))
                {
                    changes.Add(
                        new SubtitleChange
                        {
                            Type = ChangeType.Added,
                            Edited = edLine,
                            ApproxPosition = edIndex
                        }
                    );
                    edIndex++;
                }
                else
                {
                    changes.Add(
                        new SubtitleChange
                        {
                            Type = ChangeType.Deleted,
                            Translated = transLine,
                            ApproxPosition = transIndex
                        }
                    );
                    transIndex++;
                }
            }
            else if (transLine != null)
            {
                changes.Add(new SubtitleChange
                {
                    Type = ChangeType.Deleted,
                    Translated = transLine,
                    ApproxPosition = transIndex
                });
                transIndex++;
            }
            else
            {
                changes.Add(new SubtitleChange
                {
                    Type = ChangeType.Added,
                    Edited = edLine,
                    ApproxPosition = edIndex
                });
                edIndex++;
            }
        }
        return changes;
    }

    public string GetChangeReport(List<SubtitleChange> changes)
    {
        var sb = new StringBuilder();

        foreach (var change in changes)
        {
            string startTime = change.Edited?.StartTime ?? change.Translated?.StartTime ?? "";
            string endTime = change.Edited?.EndTime ?? change.Translated?.EndTime ?? "";

            if (startTime.Length >= 2 && startTime[0] == '0') startTime = startTime.Remove(0, 2);
            if (endTime.Length >= 2 && endTime[0] == '0') endTime = endTime.Remove(0, 2);

            string timeRange = _currentSettings.ShowTimeCodes ? $" [{startTime} - {endTime}]" : "";
            string indexInfo = _currentSettings.ShowIndexes ? $"[~{change.ApproxPosition}]" : "";

            switch (change.Type)
            {
                case ChangeType.Added:
                    if (!string.IsNullOrWhiteSpace(change.Edited?.Content) && _currentSettings.ShowAdded)
                    {
                        sb.AppendLine($"{indexInfo}[ДАДАДЗЕНА]{timeRange}: {change.Edited.Content}");
                    }
                    break;

                case ChangeType.Deleted:
                    if (_currentSettings.ShowDeleted)
                    {
                        sb.AppendLine($"{indexInfo}[ВЫДАЛЕНА]{timeRange}: {change.Translated.Content}");
                    }
                    break;

                case ChangeType.Modified:
                    sb.AppendLine($"{indexInfo}{timeRange}");
                    sb.AppendLine($"{change.Translated!.Content} → {change.Edited!.Content}");
                    break;
            }
        }

        return sb.ToString();
    }

    private void ApplySettings(AppSettings settings)
    {
        _currentSettings = settings;
    }

    #region Event Handlers
    private async void LoadTranslatedButton_Click(object sender, RoutedEventArgs e)
    {
        _translated = await LoadAssFileAsync("Load Translated ASS File");
        if (_translated != null)
        {
            LoadTranslatedButton.Background = new SolidColorBrush(Avalonia.Media.Color.Parse("#F28BEF"));
        }
    }

    private async void LoadEditedButton_Click(object sender, RoutedEventArgs e)
    {
        _edited = await LoadAssFileAsync("Load Edited ASS File");
        if (_edited != null)
        {
            LoadEditedButton.Background = new SolidColorBrush(Avalonia.Media.Color.Parse("#F28BEF"));
        }
    }

    private void FindChangesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_translated != null && _edited != null)
        {
            _translatedLines = ParseSubtitleData(_translated);
            _editedLines = ParseSubtitleData(_edited);
            List<SubtitleChange> changes = FindChanges(_translatedLines, _editedLines);
            string changesReport = GetChangeReport(changes);
            ChangesDisplay.Text = changesReport;
        }
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        await settings.ShowDialog(this);

        var updatedSettings = settings.SavedSettings;

        ApplySettings(updatedSettings);
    }
    
    #endregion
}