using System.Collections.Generic;
using System.Text;

public static class ChangesReporter
{
    public static string GetChangeReport(List<SubtitleChange> changes, AppSettings currentSettings)
    {
        var sb = new StringBuilder();

        foreach (var change in changes)
        {
            string startTime = change.Edited?.StartTime ?? change.Translated?.StartTime ?? "";
            string endTime = change.Edited?.EndTime ?? change.Translated?.EndTime ?? "";

            if (startTime.Length >= 2 && startTime[0] == '0') startTime = startTime.Remove(0, 2);
            if (endTime.Length >= 2 && endTime[0] == '0') endTime = endTime.Remove(0, 2);

            string timeRange = currentSettings.ShowTimeCodes ? $" [{startTime} - {endTime}]" : "";
            string indexInfo = currentSettings.ShowIndexes ? $"[~{change.ApproxPosition}]" : "";

            switch (change.Type)
            {
                case ChangeType.Added:
                    if (!string.IsNullOrWhiteSpace(change.Edited?.Content) && currentSettings.ShowAdded)
                    {
                        sb.AppendLine($"{indexInfo}[ДАДАДЗЕНА]{timeRange}: {change.Edited.Content}");
                    }
                    break;

                case ChangeType.Deleted:
                    if (currentSettings.ShowDeleted)
                    {
                        sb.AppendLine($"{indexInfo}[ВЫДАЛЕНА]{timeRange}: {change.Translated?.Content}");
                    }
                    break;

                case ChangeType.Modified:
                    sb.AppendLine($"{indexInfo}{timeRange}");
                    sb.AppendLine($"{change.Translated!.Content} → {change.Edited!.Content}");
                    break;
            }
            if (currentSettings.ShowComments && change.Edited != null && change.Edited.Comment != null)
            {
                sb.AppendLine($"[КАМЕНТАРЫЙ] {change.Edited.Comment}");
            }
        }

        return sb.ToString();
    }

}