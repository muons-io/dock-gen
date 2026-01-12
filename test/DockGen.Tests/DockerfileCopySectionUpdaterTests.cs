using DockGen.Generator;

namespace DockGen.Tests;

public sealed class DockerfileCopySectionUpdaterTests
{
    [Fact]
    public void TryUpdate_ReplacesJsonCopyBlockInBuildStageOnly()
    {
        var original = """
                       FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
                       WORKDIR /app/
                       
                       FROM --platform="$BUILDPLATFORM" mcr.microsoft.com/dotnet/sdk:8.0 AS build
                       WORKDIR /src
                       
                       COPY ["A.csproj", "A/"]
                       COPY ["B.csproj", "B/"]
                       RUN dotnet restore "A/A.csproj"
                       COPY . .
                       
                       FROM build AS publish
                       RUN dotnet publish
                       
                       FROM base AS final
                       COPY --from=publish /app/publish .
                       """;

        var newBlock = """
                       COPY ["C.csproj", "C/"]
                       COPY ["D.csproj", "D/"]
                       """;

        var result = DockerfileCopySectionUpdater.TryUpdate(original, newBlock, out var updated);

        Assert.True(result);

        var normalized = updated.Replace("\r\n", "\n");
        Assert.Contains("COPY [\"C.csproj\", \"C/\"]\nCOPY [\"D.csproj\", \"D/\"]\nRUN dotnet restore", normalized);
        Assert.DoesNotContain("COPY [\"A.csproj\", \"A/\"]", updated);
        Assert.Contains("COPY --from=publish /app/publish .", updated);

        Assert.EndsWith("\n", updated);
    }

    [Fact]
    public void TryUpdate_ReturnsFalseWhenBuildStageMissing()
    {
        var original = """
                       FROM base AS final
                       COPY . .
                       """;
        var result = DockerfileCopySectionUpdater.TryUpdate(original, """
                                                                  COPY ["x", "y"]
                                                                  """, out var updated);

        Assert.False(result);
        Assert.Equal(original, updated);
    }

    [Fact]
    public void TryUpdate_ReturnsFalseWhenNoJsonCopyBlockInBuildStage()
    {
        var original = """
                       FROM sdk AS build
                       WORKDIR /src
                       COPY . .
                       """;
        var result = DockerfileCopySectionUpdater.TryUpdate(original, """
                                                                  COPY ["x", "y"]
                                                                  """, out var updated);

        Assert.False(result);
        Assert.Equal(original, updated);
    }
}
