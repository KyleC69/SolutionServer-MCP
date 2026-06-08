// Solution: SolutionServer
// Project:   SolutionServer
// File:         WorkspaceTools.cs
// Author: Kyle L. Crowder
// Build Date: 2026/06/08



using System.ComponentModel;

using ModelContextProtocol.Server;

using SolutionServer.Models;
using SolutionServer.Services;




namespace SolutionServer.Tools;





[McpServerToolType]
internal sealed class WorkspaceTools(WorkspaceService workspaceService)
{
    [McpServerTool]
    [Description("Provides a summary of the workspace, including the absolute root path, the solution file (if any), and a list of all discovered project files.")]
    public WorkspaceSummary GetWorkspaceSummary()
    {
        return workspaceService.GetWorkspaceSummary();
    }








    [McpServerTool]
    [Description("Given a relative project file path, returns the full set of files within that project's directory tree, optionally limited by a maximum result count.")]
    public ProjectFilesResult ListProjectFiles([Description("Relative path to the target project file.")] string projectPath, [Description("Maximum number of files to return, clamped between 1 and 500.")] int maxResults = 200)
    {
        return workspaceService.ListProjectFiles(projectPath, maxResults);
    }








    [McpServerTool]
    [Description("Returns a list of all supported project files (`*.csproj`, `*.fsproj`, `*.vbproj`) located under the configured workspace root.")]
    public IReadOnlyList<ProjectInfo> ListProjects()
    {
        return workspaceService.ListProjects();
    }








    [McpServerTool]
    [Description("Retrieves a specific range of lines from a text file within the workspace root. The caller specifies the file path, a one‑based start line, and the number of lines to read (capped at 400).")]
    public FileReadResult ReadTextFile([Description("Relative path to the text file.")] string path, [Description("One-based starting line number.")] int startLine = 1, [Description("Number of lines to read, clamped between 1 and 400.")] int lineCount = 200)
    {
        return workspaceService.ReadTextFile(path, startLine, lineCount);
    }
}