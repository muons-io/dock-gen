using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace DockGen.Tests.Helpers;

public sealed class FakeFileProvider : IFileProvider
{
    private readonly List<IFileInfo> _items;

    public FakeFileProvider(List<IFileInfo> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var fileInfo = this.GetFileInfo(subpath);
        if (!fileInfo.Exists || !fileInfo.IsDirectory)
        {
            return new NotFoundDirectoryContents();
        }

        var directoryContents = new List<IFileInfo>();
        foreach (var item in _items)
        {
            if (Path.GetPathRoot(item.PhysicalPath!) == subpath)
            {
                directoryContents.Add(item);
            }
        }

        if (directoryContents.Count == 0)
        {
            return new NotFoundDirectoryContents();
        }

        return new FakeDirectoryContents(directoryContents);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        var normalized = Path.(subpath);
        var fileInfo = _items.FirstOrDefault(item => item.PhysicalPath?.Equals(normalized, StringComparison.OrdinalIgnoreCase) == true);
        if (fileInfo is null)
        {
            return new NotFoundFileInfo(subpath);
        }

        return fileInfo;
    }

    public IChangeToken Watch(string filter)
    {
        return new ConfigurationReloadToken();
    }
}
