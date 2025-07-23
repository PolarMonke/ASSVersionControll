using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AegisubVersionControl.Controls;

public partial class DictionaryTextBox : UserControl
{
    const string BASELINK = "https://dictionaries.vivy.app/?q=";
    public DictionaryTextBox()
    {
        InitializeComponent();

        var menuItem = new MenuItem { Header = "Insert Dictionary Link" };
        menuItem.Click += InsertDictionaryLink_Click;
        InnerTextBox.ContextMenu = new ContextMenu
        {
            Items = { menuItem }
        };
    }
    public string Text
    {
        get => InnerTextBox.Text;
        set => InnerTextBox.Text = value;
    }

    public string SelectedText => InnerTextBox.SelectedText;
    public int SelectionStart => InnerTextBox.SelectionStart;

    private void InsertDictionaryLink_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedText))
        {
            return;
        }
        if (!SelectedText.Any(char.IsLetter))
        {
            return;
        }

        int currentSelectionStart = SelectionStart;
        int currentSelectionLength = SelectedText.Length;
        ScrollViewer? scrollViewer = FindParentScrollViewer(this);
        double currentScrollOffset = scrollViewer?.Offset.Y ?? 0;

        string word = new string(SelectedText.Where(char.IsLetter).ToArray());
        int lineEnd = GetLineEndIndex(SelectionStart);
        string link = BASELINK + word;
        InnerTextBox.Text = Text.Insert(lineEnd, link);

        int newCursorPosition = currentSelectionStart + currentSelectionLength;
        InnerTextBox.SelectionStart = newCursorPosition;
        InnerTextBox.SelectionEnd = newCursorPosition;
        InnerTextBox.Focus();
        if (scrollViewer != null)
        {
            scrollViewer.Offset = scrollViewer.Offset.WithY(currentScrollOffset);
        }
    }

    private int GetLineEndIndex(int charIndex)
    {
        int lineEnd = Text.IndexOf('\n', charIndex);
        return lineEnd == -1 ? Text.Length : lineEnd;
    }
    
    private ScrollViewer? FindParentScrollViewer(Control control)
    {
        var parent = control.Parent;
        while (parent != null)
        {
            if (parent is ScrollViewer scrollViewer)
                return scrollViewer;
            parent = parent.Parent;
        }
        return null;
    }


}