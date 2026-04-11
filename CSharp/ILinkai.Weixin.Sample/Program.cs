using Microsoft.Extensions.Logging;
using ILinkai.Weixin.Sample.Examples;

namespace ILinkai.Weixin.Sample;

class Program
{
    static async Task<int> Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger("ILinkai.Weixin.Sample");

        Console.WriteLine("========================================");
        Console.WriteLine("  ILinkai Weixin SDK 使用示例");
        Console.WriteLine("========================================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            var registry = new ExampleRegistry(loggerFactory);
            registry.PrintUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            var registry = new ExampleRegistry(loggerFactory);
            var example = registry.GetExample(command);

            if (example != null)
            {
                return await example.ExecuteAsync(args);
            }
            else
            {
                registry.PrintUsage();
                return 0;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "执行命令时发生错误");
            return 1;
        }
    }
}
