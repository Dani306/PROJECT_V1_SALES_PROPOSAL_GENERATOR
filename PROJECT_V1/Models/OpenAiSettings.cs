namespace PROJECT_V1.Models;

public class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiBase { get; set; } = "https://api.openai.com/v1/";

    public string ChatEndpoint { get; set; } = "chat/completions";

    public string Model { get; set; } = "gpt-4o-mini";

    public int MaxRetries { get; set; } = 2;

    public int TimeoutSeconds { get; set; } = 60;
}
