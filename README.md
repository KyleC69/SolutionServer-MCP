# SolutionServer



**UNDER ACTIVE DEVELOPMENT -- FEATURES ARE BEING ADDED DAILY - UPDATE OFTEN**



SolutionServer is a production‑oriented local Model Context Protocol (MCP) server written in C# targeting .NET 10. It provides a read‑only API for inspecting a local workspace – listing projects, enumerating files, and reading file contents – which can be consumed by VS Code, Visual Studio, or any MCP‑compatible client.

## Features

- **Workspace summary** – absolute root path, solution file (if any), and discovered projects.
- **Project discovery** – supports `*.csproj`, `*.fsproj`, `*.vbproj`.
- **File enumeration** – walks the workspace while respecting ignored directories (`bin`, `obj`, `.git`, etc.).
- **Bounded file reads** – safe line‑range reads (max 400 lines).
- **MCP integration** – ready‑to‑use `.mcp` manifest and VS Code/Visual Studio configuration samples.

## Getting Started

See the detailed instructions in the [project README](SolutionServer/README.md) for building the executable and configuring MCP in your editor.

## Architecture

The server consists of three main parts:

1. **`Program.cs`** – sets up a generic host, registers logging, the `WorkspaceService`, and the MCP tools.
2. **`WorkspaceService`** – core logic for discovering the workspace, projects, and files. It enforces the `SOLUTION_SERVER_ROOT` environment variable to sandbox access.
3. **MCP tools** – thin wrappers (`WorkspaceTools`) exposing the service methods to the MCP protocol.

The code follows a clean‑architecture style: the service is independent of the hosting layer, making it easy to unit‑test.

## Contributing

- Follow the existing coding style (tabs vs spaces, naming conventions).
- Add or update unit tests under a `Tests` project (not currently present).
- Keep documentation in sync with code changes.

## License

MIT – see the `LICENSE.txt` file.
