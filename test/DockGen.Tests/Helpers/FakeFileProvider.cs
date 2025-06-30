using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace DockGen.Tests.Helpers;

public sealed class FakeFileProvider : IFileProvider
{
    private readonly List<IFileInfo> _items;
    public string RootPath { get; }

    public FakeFileProvider(string rootPath, List<IFileInfo> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        RootPath = Path.GetFullPath(rootPath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var fileInfo = GetFileInfo(subpath);
        if (!fileInfo.Exists || !fileInfo.IsDirectory)
        {
            return new NotFoundDirectoryContents();
        }

        var normalizedPath = Path.GetFullPath(subpath, RootPath);
        var directoryContents = new List<IFileInfo>();
        foreach (var item in _items)
        {
            var itemDirectory = Path.GetDirectoryName(Path.GetFullPath(item.PhysicalPath!));
            if (itemDirectory == normalizedPath)
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
        var normalizedPath = Path.GetFullPath(subpath, RootPath);
        var fileInfo = _items.FirstOrDefault(item => Path.GetFullPath(item.PhysicalPath!).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));
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
