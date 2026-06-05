using System.ComponentModel;

namespace SolutionServer.Models;

internal sealed record WorkspaceSummary(
    [property: Description("Absolute path to the configured workspace root.")] string WorkspaceRoot,
    [property: Description("Solution file path relative to the workspace root when one is discovered.")] string? SolutionFile,
    [property: Description("Number of projects discovered under the workspace root.")] int ProjectCount,
    [property: Description("Projects discovered under the workspace root.")] IReadOnlyList<ProjectInfo> Projects);

internal sealed record ProjectInfo(
    [property: Description("Project name inferred from the project file name.")] string Name,
    [property: Description("Project file path relative to the workspace root.")] string RelativePath,
    [property: Description("Project directory relative to the workspace root.")] string Directory,
    [property: Description("Target framework or frameworks declared by the project.")] string? TargetFramework,
    [property: Description("Project output type when declared.")] string? OutputType);

internal sealed record ProjectFilesResult(
    [property: Description("Project file path relative to the workspace root.")] string ProjectPath,
    [property: Description("Number of file paths returned.")] int FileCount,
    [property: Description("Files contained under the project directory, relative to the workspace root.")] IReadOnlyList<string> Files);

internal sealed record FileReadResult(
    [property: Description("File path relative to the workspace root.")] string RelativePath,
    [property: Description("One-based starting line included in the response.")] int StartLine,
    [property: Description("One-based ending line included in the response.")] int EndLine,
    [property: Description("Text content for the requested line window.")] string Content);