using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls.Embedding.Offscreen;

public static class ChangesFinder
{
    private static bool TimecodesMatch(SubtitleEntry a, SubtitleEntry b)
    {
        return ParseTime(a.StartTime) == ParseTime(b.StartTime) &&
            ParseTime(a.EndTime) == ParseTime(b.EndTime);
    }
    private static TimeSpan ParseTime(string timecode)
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
    private static bool IsEarlier(SubtitleEntry a, SubtitleEntry b)
    {
        return ParseTime(a.StartTime) < ParseTime(b.StartTime);
    }
    static string UnderlineWord(string word)
    {
        return string.Concat(word.Select(c => $"{c}\u0332"));
    }
    static string[] Tokenize(string input)
    {
        var matches = Regex.Matches(input, @"[\w']+|\s+|[^\w\s']");
        return matches.Select(m => m.Value).ToArray();
    }
    private static (List<string>, List<string>) DiffAlign(string[] transWords, string[] edWords)
    {
        int transLen = transWords.Length;
        int edLen = edWords.Length;

        int[,] lcsTable = new int[transLen + 1, edLen + 1];

        for (int i = transLen - 1; i >= 0; i--)
        {
            for (int j = edLen - 1; j >= 0; j--)
            {
                if (transWords[i] == edWords[j])
                {
                    lcsTable[i, j] = 1 + lcsTable[i + 1, j + 1];
                }
                else
                {
                    lcsTable[i, j] = Math.Max(lcsTable[i + 1, j], lcsTable[i, j + 1]);
                }
            }
        }
        var alignedTrans = new List<string>();
        var alignedEd = new List<string>();

        int transIndex = 0;
        int edIndex = 0;

        while (transIndex < transLen && edIndex < edLen)
        {
            if (transWords[transIndex] == edWords[edIndex])
            {
                alignedTrans.Add(transWords[transIndex]);
                alignedEd.Add(edWords[edIndex]);
                transIndex++;
                edIndex++;
            }
            else if (lcsTable[transIndex + 1, edIndex] >= lcsTable[transIndex, edIndex + 1])
            {
                alignedTrans.Add(UnderlineWord(transWords[transIndex]));
                transIndex++;
            }
            else
            {
                alignedEd.Add(UnderlineWord(edWords[edIndex]));
                edIndex++;
            }
        }

        while (transIndex < transLen)
            alignedTrans.Add(UnderlineWord(transWords[transIndex++]));

        while (edIndex < edLen)
            alignedEd.Add(UnderlineWord(edWords[edIndex++]));

        return (alignedTrans, alignedEd);
    }

    public static List<SubtitleChange> FindChanges(List<SubtitleEntry> transLines, List<SubtitleEntry> edLines, AppSettings appSettings)
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
                        if (appSettings.UnderlineChanges && transLine.Content != null && edLine.Content != null)
                        {
                            string[] transWords = Tokenize(transLine.Content);
                            string[] edWords = Tokenize(edLine.Content);

                            List<string> underlinedTrans;
                            List<string> underlinedEd;
                            (underlinedTrans, underlinedEd) = DiffAlign(transWords, edWords);

                            transLine.Content = string.Join("", underlinedTrans);
                            edLine.Content = string.Join("", underlinedEd);

                        }
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
                            ApproxPosition = edIndex,
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
}