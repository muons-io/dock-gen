using DockGen.Generator;
using Microsoft.Extensions.Logging;
using Moq;

namespace DockGen.Tests;

public sealed class ProjectFileLocatorTests
{
    private readonly ILogger<ProjectFileLocator> _locatorLogger = new Mock<ILogger<ProjectFileLocator>>().Object;

    [Theory]
    [InlineData("/repos/projectA", 0)]
    [InlineData("/repos/projectA/dir1", 0)]
    [InlineData("/repos/projectA/dir1/dir2", 0)]
    [InlineData("/repos/projectB", 1)]
    [InlineData("/repos/projectB/dir1", 0)]
    [InlineData("/repos/projectC", 4)]
    [InlineData("/repos/projectC/dir1", 1)]
    [InlineData("/repos/projectC/dir2", 1)]
    public async Task Locate_WhenOnlyWorkingDirectorySpecified_ReturnExpectedNumberOfProjects(string workingDirectory, int expectedCount)
    {
        var locator = new ProjectFileLocator(_locatorLogger, TestData.FileProvider);

        var request = new AnalyserRequest(workingDirectory);

        var projects = await locator.LocateProjectFilesAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Equal(expectedCount, projects.Count);
    }
}
