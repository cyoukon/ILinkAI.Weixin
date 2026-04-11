namespace ILinkai.Weixin.Cli.Commands;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    Task<int> ExecuteAsync(string[] args);
}
