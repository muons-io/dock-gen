namespace DockGen.Generator;

/// <summary>
/// Represents a request to analyze project or solution files, including information about the working directory,
/// solution path, and project path.
/// </summary>
/// <param name="WorkingDirectory">
/// The working directory where the analysis is initiated. This directory typically contains the solution or project files.
/// </param>
/// <param name="SolutionPath">
/// The optional path to the solution file (.sln)/(.slnx). If provided, it will be used to analyze projects within the solution.
/// </param>
/// <param name="ProjectPath">
/// The optional path to a specific project file (.csproj). If provided, it allows focusing the analysis on a single project.
/// </param>
public sealed record AnalyserRequest(string WorkingDirectory, string? SolutionPath = null, string? ProjectPath = null);