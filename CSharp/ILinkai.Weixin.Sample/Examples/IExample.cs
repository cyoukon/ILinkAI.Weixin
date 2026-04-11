namespace ILinkai.Weixin.Sample.Examples;

public interface IExample
{
    string Name { get; }
    string Description { get; }
    string Usage { get; }
    Task<int> ExecuteAsync(string[] args);
}
