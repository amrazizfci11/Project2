using System.Text;
using System.Text.Json;

namespace Backend.Services;

public class ClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(IConfiguration configuration, ILogger<ClaudeService> logger)
    {
        _apiKey = configuration["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude API key not configured");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _logger = logger;
    }

    public async Task<string> AnalyzeDocumentAsync(string documentText)
    {
        var prompt = $@"Analyze the following project document and extract the following information in JSON format:

1. Project Name
2. Duration of the project
3. Hierarchy of human resources needed for the project
4. Stages of the project
5. Special conditions of the project
6. Boundaries of implementing the project (ITIL, governance, cyber security)

Document Content:
{documentText}

Please provide your response in the following JSON format:
{{
  ""projectName"": ""..."",
  ""projectDuration"": ""..."",
  ""humanResourcesHierarchy"": ""..."",
  ""projectStages"": ""..."",
  ""specialConditions"": ""..."",
  ""implementationBoundaries"": ""...""
}}";

        var requestBody = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 4096,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);

            var textContent = jsonResponse.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return textContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude API");
            throw;
        }
    }
}
