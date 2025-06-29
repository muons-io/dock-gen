using DockGen.Tests.Helpers;
using Microsoft.Extensions.FileProviders;

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