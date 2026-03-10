namespace PROJECT_V1.Models;

public class LlmOptions
{
    public const string SectionName = "Llm";

    public string Provider { get; set; } = "Gemini";

    public OpenAiSettings OpenAI { get; set; } = new();

    public GeminiSettings Gemini { get; set; } = new();
}
