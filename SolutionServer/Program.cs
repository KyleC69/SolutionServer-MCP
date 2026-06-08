using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SolutionServer.Services;





namespace SolutionServer;





internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

        builder.Services.AddSingleton<WorkspaceService>();

        // Add the MCP services: the transport to use (stdio) and the tools to register.
        builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

        await builder.Build().RunAsync();
    }
}
