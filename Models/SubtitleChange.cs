using System.Collections.Generic;

public enum ChangeType
{
    Unchanged,
    Modified,
    Added,
    Deleted
}

public class SubtitleChange
{
    public ChangeType Type { get; set; }
    public SubtitleEntry? Translated { get; set; }
    public SubtitleEntry? Edited { get; set; }
    public int? ApproxPosition { get; set; }
    public List<string>? LinksToDics { get; set; } = null;
}