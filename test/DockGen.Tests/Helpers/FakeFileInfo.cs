using Microsoft.Extensions.FileProviders;

namespace DockGen.Tests.Helpers;

public sealed class FakeFileInfo : IFileInfo
{
    private readonly string _fileContent;

    public bool Exists => true;
    public bool IsDirectory => false;
    public DateTimeOffset LastModified => DateTimeOffset.Now;
    public long Length { get; }
    public string Name { get; }
    public string PhysicalPath { get; }

    public FakeFileInfo(string absolutePath, string fileContent)
    {
        PhysicalPath = Path.GetFullPath(absolutePath);
        Name = Path.GetFileName(PhysicalPath);
        _fileContent = fileContent;
        Length = fileContent.Length;
    }

    public Stream CreateReadStream()
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(_fileContent);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
