# SolutionServer

SolutionServer is a production-oriented local Model Context Protocol (MCP) server built with C# and .NET 10. It is designed for public distribution and gives MCP clients a safe, focused set of tools for inspecting a local solution or workspace.

The server is intended for developer workflows in Visual Studio, VS Code, and other MCP-capable clients that can launch a local stdio executable directly.

## What it does

SolutionServer exposes a small, practical toolset for local workspace inspection:

- `GetWorkspaceSummary` - discovers the current workspace root, solution file, and available projects
- `ListProjects` - lists supported project files under the configured workspace root
- `ListProjectFiles` - enumerates files under a selected project directory
- `ReadTextFile` - reads a bounded range of lines from a text file inside the configured workspace root

The server only inspects files inside the configured `SOLUTION_SERVER_ROOT` directory and rejects paths outside that boundary.

## Requirements

- .NET 10 SDK or newer on the client machine
- An MCP-capable client such as Visual Studio or VS Code
- A published `SolutionServer` executable available on disk

## Build the executable

Publish the server before configuring your MCP client:

`dotnet publish .\SolutionServer.csproj -c Release -r win-x64`

The published executable is typically written to:

`SolutionServer\bin\Release\net10.0\win-x64\publish\SolutionServer.exe`

## Configuration inputs

SolutionServer requires one environment variable:

- `SOLUTION_SERVER_ROOT` - absolute path to the local workspace root the server is allowed to inspect

## Visual Studio setup

Visual Studio discovers repository-local MCP settings from `<solution-directory>\.mcp.json`.

This repository includes a ready-to-edit example in `.mcp.json`:

```json
{
  "$schema": "https://json.schemastore.org/mcp.json",
  "servers": {
    "SolutionServer": {
      "type": "stdio",
      "command": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer/SolutionServer/bin/Release/net10.0/win-x64/publish/SolutionServer.exe",
      "args": [],
      "env": {
        "SOLUTION_SERVER_ROOT": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer"        "SOLUTION_SERVER_ROOT": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer"
      }
    }
  }
}
```

Update the executable path and `SOLUTION_SERVER_ROOT` to match your environment.

## VS Code setup

VS Code discovers repository-local MCP settings from `.vscode/mcp.json`.

This repository includes a ready-to-edit example in `.vscode/mcp.json`:

```json
{
  "$schema": "https://json.schemastore.org/mcp.json",
  "servers": {
    "SolutionServer": {
      "type": "stdio",
      "command": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer/SolutionServer/bin/Release/net10.0/win-x64/publish/SolutionServer.exe",
      "args": [],
      "env": {
        "SOLUTION_SERVER_ROOT": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer"        "SOLUTION_SERVER_ROOT": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer"
      }
    }
  }
}
```

Update the executable path and `SOLUTION_SERVER_ROOT` before using the server.

## Local executable development

To run the built executable directly from a local publish output:

```json
{
  "servers": {
    "SolutionServer": {
      "type": "stdio",
      "command": "<ABSOLUTE PATH TO SolutionServer.exe>",
      "args": [],
      "env": {
        "SOLUTION_SERVER_ROOT": "<ABSOLUTE WORKSPACE ROOT>"        "SOLUTION_SERVER_ROOT": "<ABSOLUTE WORKSPACE ROOT>"
      }
    }
  }
}
```

## Example prompts

Once connected, try prompts such as:

- `Summarize the current solution using SolutionServer.`
- `List the projects in my workspace.`
- `Show me the files in SolutionServer/SolutionServer.csproj's project directory.`
- `Read the first 120 lines of SolutionServer/Program.cs.`

## Packaging and publishing checklist

Before publishing to NuGet.org:

1. Update the repository URLs in `SolutionServer.csproj` and `.mcp/server.json`
2. Verify the package ID and version are correct
3. Review the checked-in `.mcp.json` and `.vscode/mcp.json` samples so they point to valid executable and workspace paths
4. Build and test locally
5. Review `RELEASE_NOTES.md`
6. Pack the server
7. Publish the `.nupkg` to NuGet.org

Pack the server:

`dotnet pack -c Release`

Publish the server:

`dotnet nuget push bin/Release/*.nupkg --api-key <your-api-key> --source https://api.nuget.org/v3/index.json`

## Public package metadata

The package is configured to:

- pack as an MCP server NuGet package
- publish as a self-contained single-file executable
- use native AOT
- embed `.mcp/server.json` for MCP discovery on NuGet.org
- include this README, release notes, and an MIT license file in the package

## Security model

SolutionServer is intentionally limited to read-oriented local inspection. It does not write files, execute arbitrary shell commands, or access paths outside the configured workspace root.

Because the server exposes local file content to an MCP client, only configure it for directories you trust the connected AI workflow to inspect.

## Repository notes

Before publishing for broad public consumption, replace the placeholder repository URL values in:

- `SolutionServer.csproj`
- `.mcp/server.json`

## More information

- [Use MCP servers in Visual Studio](https://learn.microsoft.com/visualstudio/ide/mcp-servers)
- [Use MCP servers in VS Code](https://code.visualstudio.com/docs/copilot/customization/mcp-servers)
- [MCP servers in NuGet packages](https://learn.microsoft.com/nuget/concepts/nuget-mcp)
- [Model Context Protocol](https://modelcontextprotocol.io/)