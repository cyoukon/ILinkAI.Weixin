using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ILinkai.Weixin.Sample;

/// <summary>
/// 大模型对话服务，支持 OpenAI 兼容接口（如 OpenAI、DeepSeek、通义千问等）。
/// </summary>
public class LlmService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly LlmConfig _config;

    public LlmService(LlmConfig config)
    {
        _config = config;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    /// <summary>
    /// 调用大模型对话接口，返回回复文本。
    /// </summary>
    public async Task<string> ChatAsync(string userMessage, CancellationToken ct = default)
    {
        var url = _config.BaseUrl.TrimEnd('/') + "/chat/completions";

        var request = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "system", content = _config.SystemPrompt ?? "你是一个奥特曼。" },
                new { role = "user", content = userMessage },
            },
            max_tokens = _config.MaxTokens,
            temperature = _config.Temperature,
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
        httpReq.Content = new StringContent(
            JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_config.ApiKey}");

        var resp = await _httpClient.SendAsync(httpReq, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return $"[LLM 调用失败: HTTP {(int)resp.StatusCode}] {Truncate(body, 200)}";

        try
        {
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return content ?? "(空回复)";
        }
        catch (Exception ex)
        {
            return $"[LLM 响应解析失败: {ex.Message}]";
        }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "...";

    public void Dispose() => _httpClient.Dispose();
}

/// <summary>
/// 大模型配置。
/// </summary>
public class LlmConfig
{
    /// <summary>API 基础 URL（OpenAI 兼容格式）。</summary>
    public string BaseUrl { get; set; } = "https://open.bigmodel.cn/api/paas/v4";

    /// <summary>API 密钥。</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>模型名称。</summary>
    public string Model { get; set; } = "glm-4.7-flash";

    /// <summary>系统提示词。</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>最大 token 数。</summary>
    public int MaxTokens { get; set; } = 65536;

    /// <summary>温度参数。</summary>
    public double Temperature { get; set; } = 0.7;
}
