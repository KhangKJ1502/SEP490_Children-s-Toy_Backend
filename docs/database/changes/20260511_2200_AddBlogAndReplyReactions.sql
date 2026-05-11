/*
Purpose:
- Add reaction tables for BlogPost and ReviewBlogReply
- Reuse existing ReactionTypes (like/love/haha)
*/

IF OBJECT_ID(N'[dbo].[BlogPostReactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BlogPostReactions]
    (
        [ReactionPostID] INT IDENTITY(1,1) PRIMARY KEY,
        [BlogPostID]     INT NOT NULL,
        [AccountID]      INT NOT NULL,
        [ReactionTypeID] INT NOT NULL,
        [CreatedAt]      DATETIME2(0) NOT NULL CONSTRAINT [DF_BlogPostReactions_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [FK_BlogPostReactions_BlogPosts] FOREIGN KEY ([BlogPostID]) REFERENCES [dbo].[BlogPosts]([BlogPostID]),
        CONSTRAINT [FK_BlogPostReactions_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [dbo].[Accounts]([AccountID]),
        CONSTRAINT [FK_BlogPostReactions_ReactionTypes] FOREIGN KEY ([ReactionTypeID]) REFERENCES [dbo].[ReactionTypes]([ReactionTypeID]),
        CONSTRAINT [UQ_BlogPostReactions_AccountPost] UNIQUE ([AccountID], [BlogPostID])
    );

    CREATE NONCLUSTERED INDEX [IX_BlogPostReactions_Stats]
    ON [dbo].[BlogPostReactions]([BlogPostID], [ReactionTypeID]);
END;
GO

IF OBJECT_ID(N'[dbo].[ReviewBlogReplyReactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReviewBlogReplyReactions]
    (
        [ReactionReplyBlogID] INT IDENTITY(1,1) PRIMARY KEY,
        [ReplyBlogID]         INT NOT NULL,
        [AccountID]           INT NOT NULL,
        [ReactionTypeID]      INT NOT NULL,
        [CreatedAt]           DATETIME2(0) NOT NULL CONSTRAINT [DF_ReviewBlogReplyReactions_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [FK_ReviewBlogReplyReactions_ReviewBlogReplies] FOREIGN KEY ([ReplyBlogID]) REFERENCES [dbo].[ReviewBlogReplies]([ReplyBlogID]),
        CONSTRAINT [FK_ReviewBlogReplyReactions_Accounts] FOREIGN KEY ([AccountID]) REFERENCES [dbo].[Accounts]([AccountID]),
        CONSTRAINT [FK_ReviewBlogReplyReactions_ReactionTypes] FOREIGN KEY ([ReactionTypeID]) REFERENCES [dbo].[ReactionTypes]([ReactionTypeID]),
        CONSTRAINT [UQ_ReviewBlogReplyReactions_AccountReply] UNIQUE ([AccountID], [ReplyBlogID])
    );

    CREATE NONCLUSTERED INDEX [IX_ReviewBlogReplyReactions_Stats]
    ON [dbo].[ReviewBlogReplyReactions]([ReplyBlogID], [ReactionTypeID]);
END;
GO
