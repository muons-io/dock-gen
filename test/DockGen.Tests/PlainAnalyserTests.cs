using DockGen.Generator;
using DockGen.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace DockGen.Tests;

public sealed class PlainAnalyserTests
{
    private readonly ILogger<PlainAnalyser> _analyserLogger = new Mock<ILogger<PlainAnalyser>>().Object;

    [Fact]
    public async Task Analyse_WhenProjectHasNoReferences_Return1ProjectWith0References()
    {
        var fileProvider = new FakeFileProvider(rootPath: "/repos", items:
        [
            new FakeDirectoryInfo("/repos/project"),
            new FakeFileInfo("/repos/project/a.csproj", "<Project></Project>"),
            new FakeDirectoryInfo("/repos/project/dir1"),
            new FakeDirectoryInfo("/repos/project/dir1/dir2"),
            new FakeDirectoryInfo("/repos/project/dir1/dir2/dir3")
        ]);

        List<string> projectFilesPath =
        [
            fileProvider.GetFileInfo("/repos/project/a.csproj").PhysicalPath!
        ];

        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var analyser = new PlainAnalyser(_analyserLogger, fileProvider, locatorMock.Object);

        var request = new AnalyserRequest(fileProvider.RootPath,"project");

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Empty(projects[0].Dependencies);
    }

    [Fact]
    public async Task Analyse_WhenProjectHas1ReferenceWith0References_Return1ProjectWith1Reference()
    {
        var fileProvider = new FakeFileProvider(rootPath: "/repos", items:
        [
            new FakeDirectoryInfo("/repos/project/dir1"),
            new FakeFileInfo("/repos/project/dir1/a.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <ItemGroup>
                        <ProjectReference Include="..\dir2\b.csproj" />
                    </ItemGroup>
                </Project>
                """),
            new FakeDirectoryInfo("/repos/project/dir2"),
            new FakeFileInfo("/repos/project/dir2/b.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <ItemGroup>
                        <Using Include="Xunit"/>
                    </ItemGroup>
                </Project>
                """)
        ]);

        List<string> projectFilesPath =
        [
            fileProvider.GetFileInfo("/repos/project/dir1/a.csproj").PhysicalPath!
        ];

        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var analyser = new PlainAnalyser(_analyserLogger, fileProvider, locatorMock.Object);

        var request = new AnalyserRequest(fileProvider.RootPath,"project/dir1");

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Single(projects[0].Dependencies);
    }

    [Fact]
    public async Task Analyse_WhenProjectHas1ReferenceWith1Reference_Return1ProjectWith2References()
    {
        var fileProvider = new FakeFileProvider(rootPath: "/repos", items:
        [
            new FakeDirectoryInfo("/repos/project/dir1"),
            new FakeFileInfo("/repos/project/dir1/a.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <ItemGroup>
                        <ProjectReference Include="..\dir2\b.csproj" />
                    </ItemGroup>
                </Project>
                """),
            new FakeDirectoryInfo("/repos/project/dir2"),
            new FakeFileInfo("/repos/project/dir2/b.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <ItemGroup>
                        <ProjectReference Include="..\dir3\c.csproj" />
                    </ItemGroup>
                </Project>
                """),
            new FakeDirectoryInfo("/repos/project/dir3"),
            new FakeFileInfo("/repos/project/dir3/c.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                </Project>
                """)
        ]);

        List<string> projectFilesPath =
        [
            fileProvider.GetFileInfo("/repos/project/dir1/a.csproj").PhysicalPath!
        ];

        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var analyser = new PlainAnalyser(_analyserLogger, fileProvider, locatorMock.Object);

        var request = new AnalyserRequest(fileProvider.RootPath,"project/dir1");

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Equal(2, projects[0].Dependencies.Count);
    }
}
