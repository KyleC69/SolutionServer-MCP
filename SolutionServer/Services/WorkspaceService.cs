// Solution: SolutionServer
// Project:   SolutionServer
// File:         WorkspaceService.cs
// Author: Kyle L. Crowder
// Build Date: 2026/06/08



using System.Xml.Linq;

using SolutionServer.Models;




namespace SolutionServer.Services;





internal sealed class WorkspaceService
{
    private const int MaxFileResults = 500;
    private const int MaxReadLines = 400;
    private const string WorkspaceRootEnvironmentVariable = "SOLUTION_SERVER_ROOT";

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
            ".git",
            ".hg",
            ".svn",
            ".vs",
            ".vscode",
            ".idea",
            "bin",
            "obj",
            "node_modules",
            "artifacts"
    };

    private static readonly string[] ProjectPatterns = ["*.csproj", "*.fsproj", "*.vbproj"];








    private static ProjectInfo CreateProjectInfo(string workspaceRoot, string projectPath)
    {
        XDocument document = XDocument.Load(projectPath, LoadOptions.None);
        XElement? root = document.Root;
        string? targetFramework = root?.Descendants().FirstOrDefault(element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")?.Value;
        string? outputType = root?.Descendants().FirstOrDefault(element => element.Name.LocalName == "OutputType")?.Value;

        return new ProjectInfo(Path.GetFileNameWithoutExtension(projectPath), ToRelativePath(workspaceRoot, projectPath), ToRelativePath(workspaceRoot, Path.GetDirectoryName(projectPath) ?? workspaceRoot), string.IsNullOrWhiteSpace(targetFramework) ? null : targetFramework, string.IsNullOrWhiteSpace(outputType) ? null : outputType);
    }








    private static IReadOnlyList<ProjectInfo> DiscoverProjects(string workspaceRoot)
    {
        ProjectInfo[] projects = ProjectPatterns.SelectMany(pattern => Directory.EnumerateFiles(workspaceRoot, pattern, SearchOption.AllDirectories)).Where(path => !IsInIgnoredDirectory(workspaceRoot, path)).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Select(path => CreateProjectInfo(workspaceRoot, path)).ToArray();

        return projects;
    }








    private static IEnumerable<string> EnumerateWorkspaceFiles(string root)
    {
        Stack<string> pending = new();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string current = pending.Pop();

            foreach (string directory in Directory.EnumerateDirectories(current).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (IgnoredDirectories.Contains(Path.GetFileName(directory)))
                {
                    continue;
                }

                pending.Push(directory);
            }

            foreach (string file in Directory.EnumerateFiles(current).OrderBy(path => path, StringComparer.OrdinalIgnoreCase)) yield return file;
        }
    }








    private static string? FindSolutionFile(string workspaceRoot)
    {
        return Directory.EnumerateFiles(workspaceRoot, "*.sln", SearchOption.TopDirectoryOnly).Concat(Directory.EnumerateFiles(workspaceRoot, "*.slnx", SearchOption.TopDirectoryOnly)).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
    }








    private static string? FindWorkspaceRoot(string startDirectory)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(startDirectory));
        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Any() || directory.EnumerateFiles("*.slnx", SearchOption.TopDirectoryOnly).Any() || directory.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }








    private static string GetValidatedPath(string workspaceRoot, string relativePath, bool mustExist)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("A relative path is required.", nameof(relativePath));
        }

        string fullPath = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
        if (!fullPath.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only files inside the configured workspace root can be accessed.");
        }

        if (mustExist && !File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            throw new FileNotFoundException("The requested path does not exist inside the workspace root.", relativePath);
        }

        return fullPath;
    }








    public WorkspaceSummary GetWorkspaceSummary()
    {
        string workspaceRoot = ResolveWorkspaceRoot();
        IReadOnlyList<ProjectInfo> projects = DiscoverProjects(workspaceRoot);
        string? solutionFile = FindSolutionFile(workspaceRoot);

        return new WorkspaceSummary(workspaceRoot, solutionFile is null ? null : ToRelativePath(workspaceRoot, solutionFile), projects.Count, projects);
    }








    private static bool IsInIgnoredDirectory(string workspaceRoot, string path)
    {
        string relativePath = Path.GetRelativePath(workspaceRoot, path);
        string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(IgnoredDirectories.Contains);
    }








    public ProjectFilesResult ListProjectFiles(string projectPath, int maxResults)
    {
        string workspaceRoot = ResolveWorkspaceRoot();
        string projectFullPath = GetValidatedPath(workspaceRoot, projectPath, true);

        if (!ProjectPatterns.Any(pattern => projectFullPath.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The supplied path is not a supported project file.");
        }

        string projectDirectory = Path.GetDirectoryName(projectFullPath) ?? throw new InvalidOperationException("The project path did not resolve to a directory.");

        int boundedResults = Math.Clamp(maxResults, 1, MaxFileResults);
        string[] files = EnumerateWorkspaceFiles(projectDirectory).Select(path => ToRelativePath(workspaceRoot, path)).Take(boundedResults).ToArray();

        return new ProjectFilesResult(ToRelativePath(workspaceRoot, projectFullPath), files.Length, files);
    }








    public IReadOnlyList<ProjectInfo> ListProjects()
    {
        string workspaceRoot = ResolveWorkspaceRoot();
        return DiscoverProjects(workspaceRoot);
    }








    public FileReadResult ReadTextFile(string path, int startLine, int lineCount)
    {
        string workspaceRoot = ResolveWorkspaceRoot();
        string fullPath = GetValidatedPath(workspaceRoot, path, true);
        int boundedStartLine = Math.Max(startLine, 1);
        int boundedLineCount = Math.Clamp(lineCount, 1, MaxReadLines);
        string[] lines = File.ReadLines(fullPath).Skip(boundedStartLine - 1).Take(boundedLineCount).ToArray();
        int endLine = lines.Length == 0 ? boundedStartLine : boundedStartLine + lines.Length - 1;

        return new FileReadResult(ToRelativePath(workspaceRoot, fullPath), boundedStartLine, endLine, string.Join(Environment.NewLine, lines));
    }








    private static string ResolveWorkspaceRoot()
    {
        string? configuredRoot = Environment.GetEnvironmentVariable(WorkspaceRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            string fullConfiguredRoot = Path.GetFullPath(configuredRoot);
            if (!Directory.Exists(fullConfiguredRoot))
            {
                throw new DirectoryNotFoundException($"The configured workspace root '{fullConfiguredRoot}' does not exist.");
            }

            return fullConfiguredRoot;
        }

        foreach (string candidate in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            string? discoveredRoot = FindWorkspaceRoot(candidate);
            if (discoveredRoot is not null)
            {
                return discoveredRoot;
            }
        }

        return Directory.GetCurrentDirectory();
    }








    private static string ToRelativePath(string workspaceRoot, string path)
    {
        return Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/');
    }
}