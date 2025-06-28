using DockGen.Generator;
using DockGen.Tests.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace DockGen.Tests;

internal static class TestData
{
    private static readonly string SolutionCContent =
        """
        <Solution>
            <Folder Name="/dir1/">
                <Project Path="dir1/a.csproj" />
            </Folder>
            <Folder Name="/dir2/">
                <Project Path="dir2/b.csproj" />
            </Folder>
            <Folder Name="/dir3/">
                <Project Path="dir3/c.csproj" />
            </Folder>
            <Folder Name="/dir4/" />
        </Solution>
        """;

    private static readonly string ProjectAContent =
        """
        <Project Sdk="Microsoft.NET.Sdk">
        </Project>
        """;

    private static readonly string ProjectBContent =
        """
        <Project Sdk="Microsoft.NET.Sdk.Web">
            <ItemGroup>
                <ProjectReference Include="..\dir3\c.csproj" />
            </ItemGroup>
        </Project>
        """;

    private static readonly string ProjectCContent =
        """
        <Project Sdk="Microsoft.NET.Sdk">
        </Project>
        """;

    private static readonly string ProjectDContent =
        """
        <Project Sdk="Microsoft.NET.Sdk">
            <ItemGroup>
                <Using Include="Xunit"/>
            </ItemGroup>
        </Project>
        """;

    public static readonly IFileProvider FileProvider = new FakeFileProvider(
    [
        new FakeDirectoryInfo("/repos"),
        new FakeDirectoryInfo("/repos/projectA"),
        new FakeDirectoryInfo("/repos/projectA/dir1"),
        new FakeDirectoryInfo("/repos/projectA/dir1/dir2"),
        new FakeDirectoryInfo("/repos/projectA/dir1/dir2/dir3"),
        new FakeDirectoryInfo("/repos/projectB"),
        new FakeFileInfo("/repos/projectB/a.csproj", "<Project></Project>"),
        new FakeDirectoryInfo("/repos/projectB/dir1"),
        new FakeDirectoryInfo("/repos/projectB/dir1/dir2"),
        new FakeDirectoryInfo("/repos/projectB/dir1/dir2/dir3"),
        new FakeDirectoryInfo("/repos/projectC"),
        new FakeFileInfo("/repos/projectC/c.slnx", SolutionCContent),
        new FakeDirectoryInfo("/repos/projectC/dir1"),
        new FakeFileInfo("/repos/projectC/dir1/a.csproj", ProjectAContent),
        new FakeDirectoryInfo("/repos/projectC/dir2"),
        new FakeFileInfo("/repos/projectC/dir2/b.csproj", ProjectBContent),
        new FakeDirectoryInfo("/repos/projectC/dir3"),
        new FakeFileInfo("/repos/projectC/dir3/c.csproj", ProjectCContent),
        new FakeDirectoryInfo("/repos/projectC/dir4"),
        new FakeFileInfo("/repos/projectC/dir4/d.csproj", ProjectDContent),
    ]);
}

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


public sealed class PlainAnalyserTests
{
    private readonly ILogger<PlainAnalyser> _analyserLogger = new Mock<ILogger<PlainAnalyser>>().Object;

    [Fact]
    public async Task Analyse_WhenProjectHasNoReferences_Return0Dependencies()
    {
        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["/repos/projectB/a.csproj"]);

        var analyser = new PlainAnalyser(_analyserLogger, TestData.FileProvider, locatorMock.Object);

        var request = new AnalyserRequest("/repos/projectB");

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Empty(projects[0].Dependencies);
    }

    [Fact]
    public async Task Analyse_WhenProjectHasOneReference_Return2ProjectsAnd1Dependency()
    {
        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["/repos/projectC/dir2/b.csproj"]);

        var analyser = new PlainAnalyser(_analyserLogger, TestData.FileProvider, locatorMock.Object);

        var request = new AnalyserRequest("/repos/projectC/dir2");

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Equal(2, projects.Count);
        Assert.Single(projects[0].Dependencies);
    }

}
