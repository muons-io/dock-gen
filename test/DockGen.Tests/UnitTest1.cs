using DockGen.Generator;
using DockGen.Tests.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace DockGen.Tests;

public sealed class PlainAnalyserTests
{
    private readonly ILogger<PlainAnalyser> _logger = new Mock<ILogger<PlainAnalyser>>().Object;

    private static readonly IFileProvider _fileProvider = new FakeFileProvider(
    [
        new FakeDirectoryInfo("/mnt/repos/projectA/dir1"),
        new FakeDirectoryInfo("/mnt/repos/projectA/dir1/dir2"),
        new FakeDirectoryInfo("/mnt/repos/projectA/dir1/dir2/dir3"),
        new FakeDirectoryInfo("/mnt/repos/projectB"),
        new FakeFileInfo("/mnt/repos/projectB/a.csproj", "<Project></Project>"),
        new FakeDirectoryInfo("/mnt/repos/projectB/dir1/dir2/dir3"),
    ]);

    [Theory]
    // [InlineData("/mnt/repos/projectA", 0)]
    // [InlineData("/mnt/repos/projectA/dir1", 0)]
    // [InlineData("/mnt/repos/projectA/dir1/dir2", 0)]
    [InlineData("/mnt/repos/projectB", 1)]
    public async Task Analyse_WhenOnlyWorkingDirectorySpecified_ReturnExpectedNumberOfProjects(string workingDirectory, int expectedCount)
    {
        var analyser = new PlainAnalyser(_logger, _fileProvider);

        var request = new AnalyserRequest(workingDirectory);

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Equal(expectedCount, projects.Count);
    }
}
