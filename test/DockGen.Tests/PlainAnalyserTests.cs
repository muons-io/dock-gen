using DockGen.Generator;
using DockGen.Tests.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;

namespace DockGen.Tests;

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

        var analyser = new PlainAnalyser(_analyserLogger, GetTestFileProvider(), locatorMock.Object);

        var request = new AnalyserRequest("/repos/projectB");

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Empty(projects[0].Dependencies);
    }

    [Fact]
    public async Task Analyse_WhenProjectHas1ReferenceWith0Dependencies_Return1ProjectWith1Dependency()
    {
        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["/repos/projectC/dir2/b.csproj"]);

        var analyser = new PlainAnalyser(_analyserLogger, GetTestFileProvider(), locatorMock.Object);

        var request = new AnalyserRequest("/repos/projectC/dir2");

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Single(projects[0].Dependencies);
    }

    [Fact]
    public async Task Analyse_WhenProjectHas1ReferenceWith1Dependencies_Return1ProjectWith2Dependencies()
    {
        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["/repos/projectD/dir2/b.csproj"]);

        var analyser = new PlainAnalyser(_analyserLogger, GetTestFileProvider(), locatorMock.Object);

        var request = new AnalyserRequest("/repos/projectD/dir2");

        var projects = await analyser.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Single(projects[0].Dependencies);
    }

    private static FakeFileProvider GetTestFileProvider()
    {
        List<IFileInfo> items = [
            new FakeDirectoryInfo("/repos")
        ];

        List<IFileInfo> projectAItems = [
            new FakeDirectoryInfo("/repos/projectA"),
            new FakeDirectoryInfo("/repos/projectA/dir1"),
            new FakeDirectoryInfo("/repos/projectA/dir1/dir2"),
            new FakeDirectoryInfo("/repos/projectA/dir1/dir2/dir3")
        ];
        items.AddRange(projectAItems);

        List<IFileInfo> projectBItems = [
            new FakeDirectoryInfo("/repos/projectB"),
            new FakeFileInfo("/repos/projectB/a.csproj", "<Project></Project>"),
            new FakeDirectoryInfo("/repos/projectB/dir1"),
            new FakeDirectoryInfo("/repos/projectB/dir1/dir2"),
            new FakeDirectoryInfo("/repos/projectB/dir1/dir2/dir3")
        ];
        items.AddRange(projectBItems);

        List<IFileInfo> projectCItems = [
            new FakeFileInfo("/repos/projectC/c.slnx",
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
                """),
            new FakeDirectoryInfo("/repos/projectC/dir1"),
            new FakeFileInfo("/repos/projectC/dir1/a.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                </Project>
                """),
            new FakeDirectoryInfo("/repos/projectC/dir2"),
            new FakeFileInfo("/repos/projectC/dir2/b.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                    <ItemGroup>
                        <ProjectReference Include="..\dir3\c.csproj" />
                    </ItemGroup>
                </Project>
                """),
            new FakeDirectoryInfo("/repos/projectC/dir3"),
            new FakeFileInfo("/repos/projectC/dir3/c.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                </Project>
                """),
            new FakeDirectoryInfo("/repos/projectC/dir4"),
            new FakeFileInfo("/repos/projectC/dir4/d.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <ItemGroup>
                        <Using Include="Xunit"/>
                    </ItemGroup>
                </Project>
                """)
        ];
        items.AddRange(projectCItems);

        List<IFileInfo> projectDItems = [
            new FakeFileInfo("/repos/projectD/c.slnx",
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
                """),
            new FakeDirectoryInfo("/repos/projectD/dir1"),
            new FakeFileInfo("/repos/projectD/dir1/a.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                </Project>
                """),
            new FakeDirectoryInfo("/repos/projectD/dir2"),
            new FakeFileInfo("/repos/projectD/dir2/b.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                    <ItemGroup>
                        <ProjectReference Include="..\dir3\c.csproj" />
                    </ItemGroup>
                </Project>
                """),
            new FakeDirectoryInfo("/repos/projectD/dir3"),
            new FakeFileInfo("/repos/projectD/dir3/c.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <ItemGroup>
                        <ProjectReference Include="..\dir4\d.csproj" />
                    </ItemGroup>
                </Project>
                """),
            new FakeDirectoryInfo("/repos/projectD/dir4"),
            new FakeFileInfo("/repos/projectD/dir4/d.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                    <ItemGroup>
                        <Using Include="Xunit"/>
                    </ItemGroup>
                </Project>
                """)
        ];
        items.AddRange(projectDItems);

        return new FakeFileProvider(items);
    }

}
