using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class ChatConversation
{
    public int ConversationId { get; set; }

    public int AccountId { get; set; }

    public string? SessionId { get; set; }

    public string Status { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
