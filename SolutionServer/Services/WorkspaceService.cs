using System.Xml.Linq;

using SolutionServer.Models;

namespace SolutionServer.Services;

internal sealed class WorkspaceService
{
    private const string WorkspaceRootEnvironmentVariable = "SOLUTION_SERVER_ROOT";
    private const int MaxFileResults = 500;
    private const int MaxReadLines = 400;
    private static readonly string[] ProjectPatterns = ["*.csproj", "*.fsproj", "*.vbproj"];
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

    public WorkspaceSummary GetWorkspaceSummary()
    {
        var workspaceRoot = ResolveWorkspaceRoot();
        var projects = DiscoverProjects(workspaceRoot);
        var solutionFile = FindSolutionFile(workspaceRoot);

        return new WorkspaceSummary(
            workspaceRoot,
            solutionFile is null ? null : ToRelativePath(workspaceRoot, solutionFile),
            projects.Count,
            projects);
    }

    public IReadOnlyList<ProjectInfo> ListProjects()
    {
        var workspaceRoot = ResolveWorkspaceRoot();
        return DiscoverProjects(workspaceRoot);
    }

    public ProjectFilesResult ListProjectFiles(string projectPath, int maxResults)
    {
        var workspaceRoot = ResolveWorkspaceRoot();
        var projectFullPath = GetValidatedPath(workspaceRoot, projectPath, mustExist: true);

        if (!ProjectPatterns.Any(pattern => projectFullPath.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The supplied path is not a supported project file.");
        }

        var projectDirectory = Path.GetDirectoryName(projectFullPath)
            ?? throw new InvalidOperationException("The project path did not resolve to a directory.");

        var boundedResults = Math.Clamp(maxResults, 1, MaxFileResults);
        var files = EnumerateWorkspaceFiles(projectDirectory)
            .Select(path => ToRelativePath(workspaceRoot, path))
            .Take(boundedResults)
            .ToArray();

        return new ProjectFilesResult(ToRelativePath(workspaceRoot, projectFullPath), files.Length, files);
    }

    public FileReadResult ReadTextFile(string path, int startLine, int lineCount)
    {
        var workspaceRoot = ResolveWorkspaceRoot();
        var fullPath = GetValidatedPath(workspaceRoot, path, mustExist: true);
        var boundedStartLine = Math.Max(startLine, 1);
        var boundedLineCount = Math.Clamp(lineCount, 1, MaxReadLines);
        var lines = File.ReadLines(fullPath).Skip(boundedStartLine - 1).Take(boundedLineCount).ToArray();
        var endLine = lines.Length == 0 ? boundedStartLine : boundedStartLine + lines.Length - 1;

        return new FileReadResult(
            ToRelativePath(workspaceRoot, fullPath),
            boundedStartLine,
            endLine,
            string.Join(Environment.NewLine, lines));
    }

    private static IReadOnlyList<ProjectInfo> DiscoverProjects(string workspaceRoot)
    {
        var projects = ProjectPatterns
            .SelectMany(pattern => Directory.EnumerateFiles(workspaceRoot, pattern, SearchOption.AllDirectories))
            .Where(path => !IsInIgnoredDirectory(workspaceRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => CreateProjectInfo(workspaceRoot, path))
            .ToArray();

        return projects;
    }

    private static ProjectInfo CreateProjectInfo(string workspaceRoot, string projectPath)
    {
        var document = XDocument.Load(projectPath, LoadOptions.None);
        var root = document.Root;
        var targetFramework = root?
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")?
            .Value;
        var outputType = root?
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "OutputType")?
            .Value;

        return new ProjectInfo(
            Path.GetFileNameWithoutExtension(projectPath),
            ToRelativePath(workspaceRoot, projectPath),
            ToRelativePath(workspaceRoot, Path.GetDirectoryName(projectPath) ?? workspaceRoot),
            string.IsNullOrWhiteSpace(targetFramework) ? null : targetFramework,
            string.IsNullOrWhiteSpace(outputType) ? null : outputType);
    }

    private static string ResolveWorkspaceRoot()
    {
        var configuredRoot = Environment.GetEnvironmentVariable(WorkspaceRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var fullConfiguredRoot = Path.GetFullPath(configuredRoot);
            if (!Directory.Exists(fullConfiguredRoot))
            {
                throw new DirectoryNotFoundException($"The configured workspace root '{fullConfiguredRoot}' does not exist.");
            }

            return fullConfiguredRoot;
        }

        foreach (var candidate in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var discoveredRoot = FindWorkspaceRoot(candidate);
            if (discoveredRoot is not null)
            {
                return discoveredRoot;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? FindWorkspaceRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Any()
                || directory.EnumerateFiles("*.slnx", SearchOption.TopDirectoryOnly).Any()
                || directory.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? FindSolutionFile(string workspaceRoot)
    {
        return Directory.EnumerateFiles(workspaceRoot, "*.sln", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(workspaceRoot, "*.slnx", SearchOption.TopDirectoryOnly))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static IEnumerable<string> EnumerateWorkspaceFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            foreach (var directory in Directory.EnumerateDirectories(current).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (IgnoredDirectories.Contains(Path.GetFileName(directory)))
                {
                    continue;
                }

                pending.Push(directory);
            }

            foreach (var file in Directory.EnumerateFiles(current).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }

    private static bool IsInIgnoredDirectory(string workspaceRoot, string path)
    {
        var relativePath = Path.GetRelativePath(workspaceRoot, path);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(IgnoredDirectories.Contains);
    }

    private static string GetValidatedPath(string workspaceRoot, string relativePath, bool mustExist)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("A relative path is required.", nameof(relativePath));
        }

        var fullPath = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
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

    private static string ToRelativePath(string workspaceRoot, string path)
    {
        return Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/');
    }
}