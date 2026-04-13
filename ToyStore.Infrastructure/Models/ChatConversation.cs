using System;
using System.Collections.Generic;

namespace ToyStore.Infrastructure.Models;

public partial class ChatConversation
{
    public int ConversationId { get; set; }

    public int? AccountId { get; set; }

    public string? SessionId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
