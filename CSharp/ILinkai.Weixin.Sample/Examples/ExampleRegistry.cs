using Microsoft.Extensions.Logging;

namespace ILinkai.Weixin.Sample.Examples;

public class ExampleRegistry
{
    private readonly Dictionary<string, IExample> _examples = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILoggerFactory _loggerFactory;

    public ExampleRegistry(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        RegisterDefaults();
    }

    private void RegisterDefaults()
    {
        Register(new LoginExample(_loggerFactory));
        Register(new SendExample(_loggerFactory));
        Register(new MonitorExample(_loggerFactory));
        Register(new AccountExample(_loggerFactory));
        Register(new UploadExample(_loggerFactory));
        Register(new DownloadExample(_loggerFactory));
        Register(new ConfigExample(_loggerFactory));
        Register(new TypingExample(_loggerFactory));
        Register(new FullWorkflowExample(_loggerFactory));
    }

    public void Register(IExample example)
    {
        _examples[example.Name] = example;
    }

    public IExample? GetExample(string name)
    {
        return _examples.TryGetValue(name, out var example) ? example : null;
    }

    public IEnumerable<IExample> GetAllExamples()
    {
        return _examples.Values;
    }

    public void PrintUsage()
    {
        Console.WriteLine("用法: ILinkai.Weixin.Sample <命令> [参数]");
        Console.WriteLine();
        Console.WriteLine("命令:");

        foreach (var example in _examples.Values)
        {
            Console.WriteLine($"  {example.Name.PadRight(18)}- {example.Description}");
        }

        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  ILinkai.Weixin.Sample login");
        Console.WriteLine("  ILinkai.Weixin.Sample send --to user@im.wechat --text \"Hello\" --token xxx --context-token xxx");
        Console.WriteLine("  ILinkai.Weixin.Sample monitor --account-id xxx --token xxx");
        Console.WriteLine("  ILinkai.Weixin.Sample account list");
    }
}
