namespace Nitrox.Analyzers.Models;

/// <summary>
///     Models a location that can be intercepted.
/// </summary>
internal readonly record struct CallLocation(string FilePath, int Line, int Character, string InterceptableLocationSyntax)
{
    public string FileName
    {
        get
        {
            int lastSlashIndex = FilePath.LastIndexOf('/');
            if (lastSlashIndex == -1)
            {
                return "";
            }
            return FilePath[(lastSlashIndex + 1)..^3];
        }
    }
}
