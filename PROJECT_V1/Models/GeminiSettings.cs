namespace PROJECT_V1.Models;

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiBase { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/";

    public string Model { get; set; } = "gemini-2.5-flash";

    public int MaxRetries { get; set; } = 2;

    public int TimeoutSeconds { get; set; } = 60;
}
