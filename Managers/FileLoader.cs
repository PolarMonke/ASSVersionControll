using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

public static class FileLoader
{
    public static async Task<string?> LoadAssFileAsync(string title, Window window)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(window);
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
}