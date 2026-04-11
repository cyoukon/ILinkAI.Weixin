using ILinkai.Weixin.Sdk.Auth;

namespace ILinkai.Weixin.Cli.Commands;

public abstract class CommandBase : ICommand
{
    protected readonly AccountStore AccountStore = new();

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Task<int> ExecuteAsync(string[] args);

    protected static void Log(string message)
    {
        Console.WriteLine($"\x1b[36m[ilinkai-weixin]\x1b[0m {message}");
    }

    protected static void Error(string message)
    {
        Console.Error.WriteLine($"\x1b[31m[ilinkai-weixin]\x1b[0m {message}");
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
