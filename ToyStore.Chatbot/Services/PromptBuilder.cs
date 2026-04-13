namespace ToyStore.Chatbot.Services;

// TODO: Implement khi feature Chatbot được assign
public class PromptBuilder { }

public class ChatAnalysis
{
    public string OriginalMessage { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public int? ExtractedAge { get; set; }
    public decimal? ExtractedBudget { get; set; }
    public List<string> ExtractedInterests { get; set; } = new();
    public double Confidence { get; set; }
}
