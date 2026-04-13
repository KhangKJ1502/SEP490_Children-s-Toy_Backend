namespace ToyStore.Chatbot.Templates;

/// <summary>
/// Prompt templates for chatbot interactions.
/// Uses template placeholders that can be dynamically replaced.
/// </summary>
public static class PromptTemplates
{
    /// <summary>
    /// System prompt for the chatbot's persona.
    /// </summary>
    public const string SystemPrompt = @"
You are ToyBot, a friendly and knowledgeable toy shopping assistant for ToyStore, 
an online children's toy store. Your goal is to help parents find the perfect toys 
for their children.

Guidelines:
- Be warm, friendly, and helpful
- Ask clarifying questions when needed
- Consider child's age, interests, and budget
- Recommend safe, age-appropriate toys
- Explain why each recommendation is suitable
- Be concise but informative
";

    /// <summary>
    /// Template for greeting new users.
    /// </summary>
    public const string GreetingTemplate = @"
Hello! 👋 Welcome to ToyStore! I'm ToyBot, your personal toy shopping assistant.

I can help you find the perfect toy based on:
🎂 Your child's age
🎯 Their interests and hobbies
💰 Your budget

How can I help you today?
";

    /// <summary>
    /// Template for asking about child's age.
    /// Placeholder: None
    /// </summary>
    public const string AskAgeTemplate = @"
To recommend the best toys, I'd love to know more about the child you're shopping for.
How old is the child? (You can say something like ""5 years old"" or ""toddler"")
";

    /// <summary>
    /// Template for asking about interests.
    /// Placeholder: {age}
    /// </summary>
    public const string AskInterestsTemplate = @"
Great! Shopping for a {age} year old is so much fun! 🎉

What are they interested in? For example:
- 🦸 Superheroes and action figures
- 🏗️ Building and construction
- 🎨 Arts and crafts
- 🧩 Puzzles and problem-solving
- 🏃 Outdoor activities
- 🎵 Music and instruments
- 🤖 Science and technology
";

    /// <summary>
    /// Template for asking about budget.
    /// Placeholder: {interests}
    /// </summary>
    public const string AskBudgetTemplate = @"
{interests} - wonderful choices! 

Do you have a budget in mind? You can tell me:
- A specific amount (e.g., ""around 500,000 VND"")
- A range (e.g., ""between 200,000 and 400,000 VND"")
- Or just say ""no limit"" if you're flexible
";

    /// <summary>
    /// Template for providing recommendations.
    /// Placeholders: {age}, {interests}, {budget}, {recommendations}
    /// </summary>
    public const string RecommendationTemplate = @"
Based on what you've told me about your {age} year old who loves {interests}, 
here are my top recommendations{budget_text}:

{recommendations}

Would you like more details about any of these, or should I suggest different options?
";

    /// <summary>
    /// Template for a single product recommendation.
    /// Placeholders: {name}, {price}, {age_range}, {reason}
    /// </summary>
    public const string ProductRecommendationTemplate = @"
🎁 **{name}**
   💰 {price}
   👶 Ages: {age_range}
   ✨ {reason}
";

    /// <summary>
    /// Template for no results found.
    /// Placeholders: {criteria}
    /// </summary>
    public const string NoResultsTemplate = @"
I couldn't find exact matches for {criteria}, but here are some alternatives 
that your child might love:

{alternatives}

Would you like me to search with different criteria?
";

    /// <summary>
    /// Template for unable to understand.
    /// </summary>
    public const string ConfusedTemplate = @"
I'm not quite sure I understood that. Could you please tell me:
- The age of the child
- What they're interested in
- Your budget (optional)

Or you can just describe what kind of toy you're looking for!
";

    /// <summary>
    /// Template for goodbye.
    /// </summary>
    public const string GoodbyeTemplate = @"
Thank you for shopping with ToyStore! 🎉

If you have any more questions, I'm always here to help.
Happy playing! 🧸
";
}
