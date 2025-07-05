using System.Collections;
using Microsoft.Extensions.FileProviders;

namespace DockGen.Tests.Helpers;

public sealed class FakeDirectoryContents : IDirectoryContents
{
    private readonly List<IFileInfo> _items;

    public FakeDirectoryContents(List<IFileInfo> items)
    {
        _items = items;
    }

    public bool Exists => _items.Count > 0;

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}