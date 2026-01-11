using System.Text.RegularExpressions;

namespace DockGen.Generator;

public static class DockerfileCopySectionUpdater
{
    private static readonly Regex FromRegex = new("^FROM\\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BuildStageRegex = new("^FROM\\s+.*\\s+AS\\s+build\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CopyJsonRegex = new("^\\s*COPY\\s+\\[\\\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryUpdate(string originalDockerfile, string newCopyBlock, out string updatedDockerfile)
    {
        if (string.IsNullOrWhiteSpace(originalDockerfile))
        {
            updatedDockerfile = originalDockerfile;
            return false;
        }

        var newline = DetectNewline(originalDockerfile);
        var lines = SplitLines(originalDockerfile);

        var buildStageStart = FindBuildStageStart(lines);
        if (buildStageStart < 0)
        {
            updatedDockerfile = originalDockerfile;
            return false;
        }

        var stageEnd = FindStageEnd(lines, buildStageStart + 1);
        if (stageEnd < 0)
        {
            stageEnd = lines.Length;
        }

        var copyStart = -1;
        var copyEndExclusive = -1;

        for (var i = buildStageStart + 1; i < stageEnd; i++)
        {
            if (!CopyJsonRegex.IsMatch(lines[i]))
            {
                continue;
            }

            copyStart = i;

            var j = i;
            while (j < stageEnd && CopyJsonRegex.IsMatch(lines[j]))
            {
                j++;
            }

            copyEndExclusive = j;
            break;
        }

        if (copyStart < 0 || copyEndExclusive < 0)
        {
            updatedDockerfile = originalDockerfile;
            return false;
        }

        var newCopyLines = SplitLines(NormalizeBlock(newCopyBlock, newline));

        var updatedLines = new List<string>(lines.Length - (copyEndExclusive - copyStart) + newCopyLines.Length);
        for (var i = 0; i < copyStart; i++)
        {
            updatedLines.Add(lines[i]);
        }

        updatedLines.AddRange(newCopyLines);

        for (var i = copyEndExclusive; i < lines.Length; i++)
        {
            updatedLines.Add(lines[i]);
        }

        updatedDockerfile = string.Join(newline, updatedLines);
        if (!updatedDockerfile.EndsWith(newline, StringComparison.Ordinal))
        {
            updatedDockerfile += newline;
        }

        return true;
    }

    private static string DetectNewline(string text)
    {
        return text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    }

    private static string[] SplitLines(string text)
    {
        return text.Replace("\r\n", "\n").Split('\n');
    }

    private static string NormalizeBlock(string block, string newline)
    {
        var normalized = block.Replace("\r\n", "\n").Replace("\r", "\n");
        normalized = normalized.TrimEnd('\n');
        return normalized.Replace("\n", newline);
    }

    private static int FindBuildStageStart(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (BuildStageRegex.IsMatch(lines[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindStageEnd(string[] lines, int startIndex)
    {
        for (var i = startIndex; i < lines.Length; i++)
        {
            if (FromRegex.IsMatch(lines[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
