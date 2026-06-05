using System.ComponentModel;

using ModelContextProtocol.Server;

using SolutionServer.Models;
using SolutionServer.Services;

namespace SolutionServer.Tools;

internal sealed class WorkspaceTools(WorkspaceService workspaceService)
{
    [McpServerTool]
    [Description("Summarizes the configured local solution or workspace by discovering the solution file and projects under the allowed root.")]
    public WorkspaceSummary GetWorkspaceSummary()
    {
        return workspaceService.GetWorkspaceSummary();
    }

    [McpServerTool]
    [Description("Lists the supported project files discovered under the configured local workspace root.")]
    public IReadOnlyList<ProjectInfo> ListProjects()
    {
        return workspaceService.ListProjects();
    }

    [McpServerTool]
    [Description("Lists files within the directory of a specific project file. The project path must be relative to the configured workspace root.")]
    public ProjectFilesResult ListProjectFiles(
        [Description("Relative path to the target project file.")] string projectPath,
        [Description("Maximum number of files to return, clamped between 1 and 500.")] int maxResults = 200)
    {
        return workspaceService.ListProjectFiles(projectPath, maxResults);
    }

    [McpServerTool]
    [Description("Reads a bounded range of lines from a text file inside the configured workspace root.")]
    public FileReadResult ReadTextFile(
        [Description("Relative path to the text file.")] string path,
        [Description("One-based starting line number.")] int startLine = 1,
        [Description("Number of lines to read, clamped between 1 and 400.")] int lineCount = 200)
    {
        return workspaceService.ReadTextFile(path, startLine, lineCount);
    }
}