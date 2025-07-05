using DockGen.Generator.Constants;

namespace DockGen.Generator;

/// <summary>
/// Represents a request to analyze project or solution files, including information about the working directory,
/// solution path, and project path.
/// </summary>
/// <param name="WorkingDirectory">
/// The working directory where the analysis is initiated. This directory typically contains the solution or project files.
/// </param>
/// <param name="RelativeDirectory">
/// The optional directory path to analyze. This can be a specific folder containing project files or a solution file.
/// </param>
/// <param name="RelativeSolutionPath">
/// The optional path to the solution file (.sln)/(.slnx). If provided, it will be used to analyze projects within the solution.
/// </param>
/// <param name="RelativeProjectPath">
/// The optional path to a specific project file (.csproj). If provided, it allows focusing the analysis on a single project.
/// </param>
public sealed record AnalyzerRequest(
    string WorkingDirectory,
    string? RelativeDirectory,
    string? RelativeSolutionPath = null,
    string? RelativeProjectPath = null,
    string Analyzer = DockGenConstants.SimpleAnalyzerName);
