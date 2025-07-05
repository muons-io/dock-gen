using Microsoft.Extensions.FileProviders;

namespace DockGen.Tests.Helpers;

public sealed class FakeDirectoryInfo : IFileInfo
{
    public bool Exists => true;
    public bool IsDirectory => true;

    public DateTimeOffset LastModified => DateTimeOffset.Now;
    public long Length { get; } = 0;
    public string Name { get; }
    public string PhysicalPath { get; }

    public FakeDirectoryInfo(string absolutePath)
    {
        PhysicalPath = Path.GetFullPath(absolutePath);
        Name = new DirectoryInfo(PhysicalPath).Name;
    }

    public Stream CreateReadStream()
    {
        throw new NotSupportedException("Cannot read from a directory.");
    }
}
