using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sample.Examples;

public abstract class ExampleBase : IExample
{
    protected readonly ILoggerFactory LoggerFactory;
    protected readonly ILogger Logger;

    protected ExampleBase(ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger(GetType());
    }

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Usage { get; }
    public abstract Task<int> ExecuteAsync(string[] args);

    protected void PrintHeader(string title)
    {
        Console.WriteLine($">>> {title}");
        Console.WriteLine();
    }

    protected void PrintSuccess(string message)
    {
        Console.WriteLine($"✅ {message}");
    }

    protected void PrintError(string message)
    {
        Console.WriteLine($"❌ {message}");
    }

    protected void PrintWarning(string message)
    {
        Console.WriteLine($"⚠️ {message}");
    }

    protected static string? ParseArgument(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
