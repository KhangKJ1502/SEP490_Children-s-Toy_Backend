using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ChatMessage
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public string SenderType { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Payload { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;
}
