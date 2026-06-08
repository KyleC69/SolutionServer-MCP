# The Issue

There are very distinctive differences between the Copilots used today. Visual Studio agent is anal retentive and has zero imagination. Windows Copilot is very laid back and even has a sense of humor if you talk to it for a while.  VS Code agent is the Superman of agents, it does not have the constraints the others do so can give it a task to design a game and unless there is an I/O issue it will do it. But it does have a gap in the knowledge of the .Net SDK project systems. Windows Copilot has explained to me that since VS code was not designed to be a .Net IDE it relies on heuristics to get around and find things. I noticed right away that it spent a lot of time and (tokens) to find things and navigate a .net project given the new price strategies placed on tokens I was determined to squeeze all the value I could get. So that is where this project comes in. It is a local MCP server with one parameter to set the sandbox parent, defining the topmost folder it can access. It has several tools to extract the information it needs in a deterministic focused call instead of a swarm of calls with different tools. 

# SolutionServer

SolutionServer is a production-oriented local Model Context Protocol (MCP) server built with C# and .NET 10. It is designed for public distribution and gives MCP clients a safe, focused set of tools for inspecting a local solution or workspace. To give VS Code agent the needed knowledge and structure of your .Net projects.

The server is intended for developer workflows in Visual Studio, VS Code, and other MCP-capable clients that can launch a local stdio executable directly.

## What it does
## Build the executable
SolutionServer provides a focused, read‑only API for local workspace inspection:
Publish the server before configuring your MCP client:

```bash
dotnet publish .\SolutionServer.csproj -c Release -r win-x64
```
- **`GetWorkspaceSummary`** – Returns the absolute workspace root, the solution file (if present), and a list of all recognized project files.
The published executable is written to:

`SolutionServer\bin\Release\net10.0\win-x64\publish\SolutionServer.exe`
- **`ListProjects`** – Enumerates every supported project file (`*.csproj`, `*.fsproj`, `*.vbproj`) under the workspace root.
- **`ListProjectFiles`** – Given a project file path, returns all files contained in that project’s directory tree.
- **`ReadTextFile`** – Reads a specific line range (max 400 lines) from a text file inside the workspace root.

All operations are confined to the directory specified by `SOLUTION_SERVER_ROOT`; the server never writes or executes code, ensuring a safe, sandboxed interaction.
## Visual Studio setup
`SolutionServer\bin\Release\net10.0\win-x64\publish\SolutionServer.exe`
Visual Studio discovers repository‑local MCP settings from `<solution-directory>\.mcp.json`.

This repository includes a ready‑to‑edit example in `.mcp.json` (located at the repository root):

```json
{
  "$schema": "https://json.schemastore.org/mcp.json",
  "servers": {
    "SolutionServer": {
      "type": "stdio",
      "command": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer/SolutionServer/bin/Release/net10.0/win-x64/publish/SolutionServer.exe",
      "args": [],
      "env": {
        "SOLUTION_SERVER_ROOT": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer"
      }
    }
  }
}
```

Update the `command` and `SOLUTION_SERVER_ROOT` values to match your environment.
## Configuration inputs

SolutionServer requires one environment variable:

- `SOLUTION_SERVER_ROOT` - absolute path to the local workspace root the server is allowed to inspect
## VS Code setup
      "env": {
VS Code discovers repository‑local MCP settings from `.vscode/mcp.json`.
        "SOLUTION_SERVER_ROOT": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer"        "SOLUTION_SERVER_ROOT": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer"
This repository now includes a proper sample file at `.vscode/mcp.json` (see the newly added file). The content mirrors the Visual Studio example but is placed where VS Code expects it.

```json
{
  "$schema": "https://json.schemastore.org/mcp.json",
  "servers": {
    "SolutionServer": {
      "type": "stdio",
      "command": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer/SolutionServer/bin/Release/net10.0/win-x64/publish/SolutionServer.exe",
      "args": [],
      "env": {
        "SOLUTION_SERVER_ROOT": "F:/_dev_drv_root_/Repos/SolutionServer/SolutionServer"
      }
    }
  }
}
```

Update the `command` and `SOLUTION_SERVER_ROOT` values to match your environment.
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
