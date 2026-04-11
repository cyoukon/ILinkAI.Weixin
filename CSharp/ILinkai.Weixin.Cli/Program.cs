using ILinkai.Weixin.Cli.Commands;

namespace ILinkai.Weixin.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var registry = new CommandRegistry();

        if (args.Length == 0)
        {
            registry.PrintUsage();
            return 0;
        }

        var commandName = args[0].ToLowerInvariant();
        var command = registry.GetCommand(commandName);

        if (command != null)
        {
            return await command.ExecuteAsync(args);
        }
        else
        {
            Console.WriteLine($"未知命令: {commandName}");
            Console.WriteLine();
            registry.PrintUsage();
            return 1;
        }
    }
}
