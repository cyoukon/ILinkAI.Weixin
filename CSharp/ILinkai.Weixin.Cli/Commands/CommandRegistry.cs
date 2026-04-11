namespace ILinkai.Weixin.Cli.Commands;

public class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public CommandRegistry()
    {
        RegisterDefaults();
    }

    private void RegisterDefaults()
    {
        Register(new InstallCommand());
        Register(new LoginCommand());
        Register(new SendCommand());
        Register(new MonitorCommand());
        Register(new AccountCommand());
    }

    public void Register(ICommand command)
    {
        _commands[command.Name] = command;
    }

    public ICommand? GetCommand(string name)
    {
        return _commands.TryGetValue(name, out var command) ? command : null;
    }

    public IEnumerable<ICommand> GetAllCommands()
    {
        return _commands.Values;
    }

    public void PrintUsage()
    {
        Console.WriteLine("ILinkai Weixin CLI - 微信ILinkai SDK命令行工具");
        Console.WriteLine();
        Console.WriteLine("用法: ilinkai-weixin <命令> [选项]");
        Console.WriteLine();
        Console.WriteLine("命令:");

        foreach (var command in _commands.Values)
        {
            Console.WriteLine($"  {command.Name.PadRight(12)}- {command.Description}");
        }

        Console.WriteLine();
        Console.WriteLine("使用 'ilinkai-weixin <命令> --help' 获取更多命令详情");
    }
}
