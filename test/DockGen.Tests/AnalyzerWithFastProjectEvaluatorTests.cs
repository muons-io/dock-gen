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

public sealed class AnalyzerWithFastProjectEvaluatorTests
{
    private readonly ILogger<Analyzer> _analyzerLogger = new Mock<ILogger<Analyzer>>().Object;
    private readonly ILogger<FastProjectEvaluator> _fastEvaluatorLogger = new Mock<ILogger<FastProjectEvaluator>>().Object;

    static AnalyzerWithFastProjectEvaluatorTests()
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

        var fastProjectEvaluator = new FastProjectEvaluator(fileProvider, relevantFileLocatorMock.Object, _fastEvaluatorLogger);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, fastProjectEvaluator, fastProjectEvaluator, fastProjectEvaluator);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project", Analyzer: DockGenConstants.FastAnalyzerName);

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

        var fastProjectEvaluator = new FastProjectEvaluator(fileProvider, relevantFileLocatorMock.Object, _fastEvaluatorLogger);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, fastProjectEvaluator, fastProjectEvaluator, fastProjectEvaluator);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project/dir1", Analyzer: DockGenConstants.FastAnalyzerName);

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

        var fastProjectEvaluator = new FastProjectEvaluator(fileProvider, relevantFileLocatorMock.Object, _fastEvaluatorLogger);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, fastProjectEvaluator, fastProjectEvaluator, fastProjectEvaluator);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project/dir1", Analyzer: DockGenConstants.FastAnalyzerName);

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

        var fastProjectEvaluator = new FastProjectEvaluator(fileProvider, relevantFileLocatorMock.Object, _fastEvaluatorLogger);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, fastProjectEvaluator, fastProjectEvaluator, fastProjectEvaluator);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project", Analyzer: DockGenConstants.FastAnalyzerName);

        var projects = await analyzer.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Equal(3, projects.Count);
        Assert.Collection(
            projects.OrderBy(x => x.ProjectName),
            project =>
            {
                Assert.Equal("a.csproj", project.ProjectName);
                Assert.Equal(2, project.Dependencies.Count);
            },
            project =>
            {
                Assert.Equal("b.csproj", project.ProjectName);
                Assert.Single(project.Dependencies);
            },
            project =>
            {
                Assert.Equal("c.csproj", project.ProjectName);
                Assert.Empty(project.Dependencies);
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

        var fastProjectEvaluator = new FastProjectEvaluator(fileProvider, relevantFileLocatorMock.Object, _fastEvaluatorLogger);

        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, fastProjectEvaluator, fastProjectEvaluator, fastProjectEvaluator);

        var request = new AnalyzerRequest(fileProvider.RootPath,"project", Analyzer: DockGenConstants.FastAnalyzerName);

        var projects = await analyzer.AnalyseAsync(request, CancellationToken.None);

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Collection(
            projects,
            project =>
            {
                Assert.Equal("a.csproj", project.ProjectName);
                Assert.Empty(project.Dependencies);
            });
    }

    [Fact]
    public async Task Analyse_WhenProjectIsWebSdk_CapturesProjectSdkProperty()
    {
        var fileProvider = new FakeFileProvider(rootPath: "/repos", items:
        [
            new FakeDirectoryInfo("/repos/project"),
            new FakeFileInfo("/repos/project/web.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                </Project>
                """)
        ]);

        List<string> projectFilesPath =
        [
            fileProvider.GetFileInfo("/repos/project/web.csproj").PhysicalPath!
        ];

        var locatorMock = new Mock<IProjectFileLocator>();
        locatorMock
            .Setup(x => x.LocateProjectFilesAsync(It.IsAny<AnalyzerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectFilesPath);

        var relevantFileLocatorMock = new Mock<IRelevantFileLocator>();
        relevantFileLocatorMock
            .Setup(x => x.GetRelevantFilesAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var fastProjectEvaluator = new FastProjectEvaluator(fileProvider, relevantFileLocatorMock.Object, _fastEvaluatorLogger);
        var analyzer = new Analyzer(_analyzerLogger, locatorMock.Object, fastProjectEvaluator, fastProjectEvaluator, fastProjectEvaluator);

        var request = new AnalyzerRequest(fileProvider.RootPath, "project", Analyzer: DockGenConstants.FastAnalyzerName);

        var projects = await analyzer.AnalyseAsync(request, CancellationToken.None);

        Assert.Single(projects);
        Assert.True(projects[0].Properties.TryGetValue("MSBuildProjectSdk", out var sdk));
        Assert.Equal("Microsoft.NET.Sdk.Web", sdk);
    }
}
