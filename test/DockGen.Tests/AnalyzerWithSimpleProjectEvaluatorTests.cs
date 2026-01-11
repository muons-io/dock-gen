using DockGen.Generator;
using DockGen.Generator.Constants;
using DockGen.Generator.Evaluators;
using DockGen.Generator.Locators;
using DockGen.Tests.Helpers;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using Moq;
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace DockGen.Tests;

public sealed class AnalyzerWithSimpleProjectEvaluatorTests
{
    private readonly ILogger<Analyzer> _analyzerLogger = new Mock<ILogger<Analyzer>>().Object;

    /// <summary>
    /// Static constructor that initializes MSBuild for this test class by
    /// registering the default MSBuild instance once before any tests run.
    /// This is required so that types depending on MSBuild APIs can be created
    /// without each test having to perform MSBuildLocator initialization.
    /// </summary>
    static AnalyzerWithSimpleProjectEvaluatorTests()
    {
        MSBuildLocator.RegisterDefaults();
    }

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
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyzerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var relevantFileLocatorMock = new Mock<IRelevantFileLocator>();
        relevantFileLocatorMock
            .Setup(x => x.GetRelevantFilesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var simpleProjectEvaluator = new SimpleProjectEvaluator(fileProvider, relevantFileLocatorMock.Object);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, simpleProjectEvaluator, null!);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project", Analyzer: DockGenConstants.SimpleAnalyzerName);

        var projects = await analyzer.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Empty(projects[0].Dependencies);
    }

    [Fact]
    public async Task Analyse_WhenProjectHas1ReferenceWith0References_Return1ProjectWith1Reference()
    {
        var fileProvider = new FakeFileProvider(rootPath: "/repos", items:
        [
            new FakeDirectoryInfo("/repos/project"),
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
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyzerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var relevantFileLocatorMock = new Mock<IRelevantFileLocator>();
        relevantFileLocatorMock
            .Setup(x => x.GetRelevantFilesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var simpleProjectEvaluator = new SimpleProjectEvaluator(fileProvider, relevantFileLocatorMock.Object);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, simpleProjectEvaluator, null!);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project/dir1", Analyzer: DockGenConstants.SimpleAnalyzerName);

        var projects = await analyzer.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Single(projects[0].Dependencies);
    }

    [Fact]
    public async Task Analyse_WhenDirectoryHas1ProjectWith11ReferenceWith1Reference_Return1ProjectWith2References()
    {
        var fileProvider = new FakeFileProvider(rootPath: "/repos", items:
        [
            new FakeDirectoryInfo("/repos/project"),
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
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyzerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var relevantFileLocatorMock = new Mock<IRelevantFileLocator>();
        relevantFileLocatorMock
            .Setup(x => x.GetRelevantFilesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var simpleProjectEvaluator = new SimpleProjectEvaluator(fileProvider, relevantFileLocatorMock.Object);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, simpleProjectEvaluator, null!);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project/dir1", Analyzer: DockGenConstants.SimpleAnalyzerName);

        var projects = await analyzer.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Equal(2, projects[0].Dependencies.Count);
    }

    [Fact]
    public async Task Analyse_WhenDirectoryHas3ProjectsWith1ReferenceWith1Reference_Return1ProjectWith2References()
    {
        var fileProvider = new FakeFileProvider(rootPath: "/repos", items:
        [
            new FakeDirectoryInfo("/repos/project"),
            new FakeFileInfo("/repos/project/Directory.Build.props",
                """
                <Project>
                    <PropertyGroup>
                        <CustomProperty1>CustomProperty1Value</CustomProperty1>
                        <CustomProperty2>CustomProperty2Value</CustomProperty2>
                    </PropertyGroup>
                </Project>
                """),
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
            fileProvider.GetFileInfo("/repos/project/dir1/a.csproj").PhysicalPath!,
            fileProvider.GetFileInfo("/repos/project/dir2/b.csproj").PhysicalPath!,
            fileProvider.GetFileInfo("/repos/project/dir3/c.csproj").PhysicalPath!
        ];

        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyzerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var relevantFileLocatorMock = new Mock<IRelevantFileLocator>();
        relevantFileLocatorMock
            .Setup(x => x.GetRelevantFilesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var simpleProjectEvaluator = new SimpleProjectEvaluator(fileProvider, relevantFileLocatorMock.Object);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, simpleProjectEvaluator, null!);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project", Analyzer: DockGenConstants.SimpleAnalyzerName);

        var projects = await analyzer.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Equal(3, projects.Count);
        Assert.Collection(
            projects.OrderBy(x => x.ProjectName),
            project =>
            {
                Assert.Equal("a.csproj", project.ProjectName);
                Assert.Equal(2, project.Dependencies.Count);
                Assert.Contains(project.Properties, x => x.Key == "CustomProperty1" && x.Value == "CustomProperty1Value");
                Assert.Contains(project.Properties, x => x.Key == "CustomProperty2" && x.Value == "CustomProperty2Value");
            },
            project =>
            {
                Assert.Equal("b.csproj", project.ProjectName);
                Assert.Single(project.Dependencies);
                Assert.Contains(project.Properties, x => x.Key == "CustomProperty1" && x.Value == "CustomProperty1Value");
                Assert.Contains(project.Properties, x => x.Key == "CustomProperty2" && x.Value == "CustomProperty2Value");
            },
            project =>
            {
                Assert.Equal("c.csproj", project.ProjectName);
                Assert.Empty(project.Dependencies);
                Assert.Contains(project.Properties, x => x.Key == "CustomProperty1" && x.Value == "CustomProperty1Value");
                Assert.Contains(project.Properties, x => x.Key == "CustomProperty2" && x.Value == "CustomProperty2Value");
            });
    }

    [Fact]
    public async Task Analyse_WhenDirectoryContainsProjectAndBuildProps_Return1ProjectWith2Properties()
    {
        var fileProvider = new FakeFileProvider(rootPath: "/repos", items:
        [
            new FakeDirectoryInfo("/repos/project"),
            new FakeFileInfo("/repos/project/Directory.Build.props",
                """
                <Project>
                    <PropertyGroup>
                        <CustomProperty1>CustomProperty1Value</CustomProperty1>
                        <CustomProperty2>CustomProperty2Value</CustomProperty2>
                    </PropertyGroup>
                </Project>
                """),
            new FakeDirectoryInfo("/repos/project/dir1"),
            new FakeFileInfo("/repos/project/dir1/a.csproj",
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
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyzerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var relevantFileLocatorMock = new Mock<IRelevantFileLocator>();
        relevantFileLocatorMock
            .Setup(x => x.GetRelevantFilesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var simpleProjectEvaluator = new SimpleProjectEvaluator(fileProvider, relevantFileLocatorMock.Object);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, simpleProjectEvaluator, null!);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project", Analyzer: DockGenConstants.SimpleAnalyzerName);

        var projects = await analyzer.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Collection(
            projects,
            project =>
            {
                Assert.Equal("a.csproj", project.ProjectName);
                Assert.Empty(project.Dependencies);
                Assert.Contains(project.Properties, x => x.Key == "CustomProperty1" && x.Value == "CustomProperty1Value");
                Assert.Contains(project.Properties, x => x.Key == "CustomProperty2" && x.Value == "CustomProperty2Value");
            });
    }
}
