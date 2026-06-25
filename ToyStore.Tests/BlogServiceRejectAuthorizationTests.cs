using System.Reflection;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using ToyStore.Application.DTOs.Blogs;
using ToyStore.Application.DTOs.Notifications;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Blogs;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Services;
using Xunit;

namespace ToyStore.Tests;

public class BlogServiceRejectAuthorizationTests
{
    private const byte RejectReasonId = 1;

    [Theory]
    [InlineData("Customer")]
    [InlineData("Staff")]
    [InlineData("Admin")]
    public async Task Admin_CanRejectEveryBlogReview(string reviewAuthorRole)
    {
        var review = CreateReview(reviewAuthorRole);
        var repository = new BlogRepositoryStub(review: review);
        var service = CreateService("Admin", repository);

        var result = await service.UpdateBlogReviewStatusAsync(
            review.ReviewBlogId,
            new UpdateBlogReviewStatusDto { ModerationStatus = "Rejected", BanReasonId = RejectReasonId });

        Assert.True(result.IsSuccess);
        Assert.Equal("Rejected", review.ModerationStatus);
        Assert.Contains(repository.Logs, x => x.TargetType == "Comment" && x.Action == "Rejected");
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Staff")]
    [InlineData("Admin")]
    public async Task Admin_CanRejectEveryBlogReviewReply(string replyAuthorRole)
    {
        var reply = CreateReply(replyAuthorRole);
        var repository = new BlogRepositoryStub(reply: reply);
        var service = CreateService("Admin", repository);

        var result = await service.UpdateBlogReplyStatusAsync(
            reply.ReplyBlogId,
            new UpdateBlogReviewStatusDto { ModerationStatus = "Rejected", BanReasonId = RejectReasonId });

        Assert.True(result.IsSuccess);
        Assert.Equal("Rejected", reply.ModerationStatus);
        Assert.Contains(repository.Logs, x => x.TargetType == "Reply" && x.Action == "Rejected");
    }

    [Fact]
    public async Task Staff_CanRejectCustomerBlogReview()
    {
        var review = CreateReview("Customer");
        var repository = new BlogRepositoryStub(review: review);
        var service = CreateService("Staff", repository);

        var result = await service.UpdateBlogReviewStatusAsync(
            review.ReviewBlogId,
            new UpdateBlogReviewStatusDto { ModerationStatus = "Rejected", BanReasonId = RejectReasonId });

        Assert.True(result.IsSuccess);
        Assert.Equal("Rejected", review.ModerationStatus);
        Assert.Contains(repository.Logs, x => x.TargetType == "Comment" && x.Action == "Rejected");
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Staff")]
    [InlineData("Admin")]
    public async Task Staff_CannotRejectAnyBlogReviewReply(string replyAuthorRole)
    {
        var reply = CreateReply(replyAuthorRole);
        var repository = new BlogRepositoryStub(reply: reply);
        var service = CreateService("Staff", repository);

        var result = await service.UpdateBlogReplyStatusAsync(
            reply.ReplyBlogId,
            new UpdateBlogReviewStatusDto { ModerationStatus = "Rejected", BanReasonId = RejectReasonId });

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
        Assert.Equal("You do not have permission to reject blog review replies.", result.ErrorMessage);
        Assert.Equal("ManualReview", reply.ModerationStatus);
        Assert.Empty(repository.Logs);
    }

    [Theory]
    [InlineData("Staff")]
    [InlineData("Admin")]
    public async Task Staff_GetsForbidden_WhenRejectingNonCustomerBlogReview(string reviewAuthorRole)
    {
        var review = CreateReview(reviewAuthorRole);
        var repository = new BlogRepositoryStub(review: review);
        var service = CreateService("Staff", repository);

        var result = await service.UpdateBlogReviewStatusAsync(
            review.ReviewBlogId,
            new UpdateBlogReviewStatusDto { ModerationStatus = "Rejected", BanReasonId = RejectReasonId });

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
        Assert.Equal("You do not have permission to reject this blog review.", result.ErrorMessage);
        Assert.Equal("ManualReview", review.ModerationStatus);
        Assert.Empty(repository.Logs);
    }

    private static BlogService CreateService(string roleName, BlogRepositoryStub repository)
    {
        var unitOfWork = ProxyFactory.Create<IUnitOfWork>(new Dictionary<string, Func<object?[]?, object?>>
        {
            ["get_Blogs"] = _ => repository.Proxy,
            ["SaveChangesAsync"] = _ => Task.FromResult(0),
            ["BeginTransactionAsync"] = _ => Task.CompletedTask,
            ["CommitTransactionAsync"] = _ => Task.CompletedTask,
            ["RollbackTransactionAsync"] = _ => Task.CompletedTask,
            ["Detach"] = _ => null,
            ["Dispose"] = _ => null
        });

        return new BlogService(
            unitOfWork,
            new StubCurrentUserService(roleName),
            new NoopDomainEventPublisher(),
            null!,
            new NoopNotificationDispatcher(),
            new UpdateBlogReviewPermissionValidator(),
            NullLogger<BlogService>.Instance,
            new FixedTimeProvider(),
            new NoopBlogCommentModerationGateway());
    }

    private static ReviewBlog CreateReview(string authorRole)
    {
        return new ReviewBlog
        {
            ReviewBlogId = 100,
            BlogPostId = 200,
            AccountId = 300,
            Account = CreateAccount(300, authorRole),
            BlogPost = new BlogPost { BlogPostId = 200, BlogTitle = "Safety tips", Status = "Published" },
            Comment = "Needs review",
            ModerationStatus = "ManualReview",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ReviewBlogReply CreateReply(string authorRole)
    {
        return new ReviewBlogReply
        {
            ReplyBlogId = 101,
            ReviewBlogId = 100,
            AccountId = 301,
            Account = CreateAccount(301, authorRole),
            ReviewBlog = CreateReview("Customer"),
            Comment = "Reply under review",
            ModerationStatus = "ManualReview",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Account CreateAccount(int accountId, string roleName)
    {
        return new Account
        {
            AccountId = accountId,
            AccountName = $"{roleName} User",
            Email = $"{roleName.ToLowerInvariant()}@example.test",
            Role = new Role { RoleName = roleName }
        };
    }

    private sealed class BlogRepositoryStub
    {
        private readonly BlogCommentViolationCount _violationState = new();
        private readonly List<BlogCommentBanReason> _banReasons =
        [
            new BlogCommentBanReason { BanReasonId = RejectReasonId, Content = "Policy violation", CreatedAt = DateTime.UtcNow }
        ];

        public BlogRepositoryStub(ReviewBlog? review = null, ReviewBlogReply? reply = null)
        {
            Proxy = ProxyFactory.Create<IBlogRepository>(new Dictionary<string, Func<object?[]?, object?>>
            {
                ["GetReviewByIdAsync"] = _ => Task.FromResult(review),
                ["GetReplyByIdAsync"] = _ => Task.FromResult(reply),
                ["GetBlogCommentBanReasonsAsync"] = _ => Task.FromResult(_banReasons),
                ["UpdateReviewAsync"] = args =>
                {
                    UpdatedReview = (ReviewBlog)args![0]!;
                    return Task.CompletedTask;
                },
                ["UpdateReplyAsync"] = args =>
                {
                    UpdatedReply = (ReviewBlogReply)args![0]!;
                    return Task.CompletedTask;
                },
                ["AddCommentModerationLogAsync"] = args =>
                {
                    Logs.Add((BlogCommentModerationLog)args![0]!);
                    return Task.CompletedTask;
                },
                ["GetLatestRejectedCommentLogAsync"] = _ => Task.FromResult<BlogCommentModerationLog?>(LatestRejectedLog()),
                ["GetLatestRejectedReplyLogAsync"] = _ => Task.FromResult<BlogCommentModerationLog?>(LatestRejectedLog()),
                ["CheckAndRefreshCommentLockAsync"] = _ => Task.FromResult((false, (DateTime?)null)),
                ["GetCommentPermissionStateAsync"] = _ => Task.FromResult<BlogCommentViolationCount?>(_violationState),
                ["UpdateCommentPermissionStateAsync"] = _ => Task.CompletedTask
            });
        }

        public IBlogRepository Proxy { get; }
        public ReviewBlog? UpdatedReview { get; private set; }
        public ReviewBlogReply? UpdatedReply { get; private set; }
        public List<BlogCommentModerationLog> Logs { get; } = [];

        private BlogCommentModerationLog LatestRejectedLog()
        {
            return new BlogCommentModerationLog
            {
                BanReasonId = RejectReasonId,
                BanReason = _banReasons.Single()
            };
        }
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public StubCurrentUserService(string roleName)
        {
            RoleName = roleName;
            RoleId = roleName == "Admin" ? (byte)2 : (byte)3;
        }

        public int AccountId => 999;
        public byte RoleId { get; }
        public string Email => "moderator@example.test";
        public string RoleName { get; }
        public string? Jti => null;
        public DateTime? TokenExpiry => DateTime.UtcNow.AddHours(1);
        public bool IsAuthenticated => true;
    }

    private sealed class NoopNotificationDispatcher : INotificationDispatcher
    {
        public Task DispatchAsync(NotificationContext context, CancellationToken ct = default) => Task.CompletedTask;
        public Task DispatchBulkAsync(IEnumerable<NotificationContext> contexts, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync(string aggregateType, string aggregateId, string eventType, object payload, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoopBlogCommentModerationGateway : IBlogCommentModerationGateway
    {
        public Task<bool> ModerateCommentAsync(int reviewBlogId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ModerateReplyAsync(int replyBlogId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class FixedTimeProvider : ITimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 6, 25, 12, 0, 0, DateTimeKind.Utc);
        public DateTime VnNow => UtcNow.AddHours(7);
        public DateTime TodayVn => VnNow.Date;
        public DateTime ToVnTime(DateTime utcTime) => utcTime.AddHours(7);
        public DateTime ToUtc(DateTime vnTime) => vnTime.AddHours(-7);
    }

    private class ProxyFactory<T> : DispatchProxy where T : class
    {
        private Dictionary<string, Func<object?[]?, object?>> _handlers = new();

        public static T Create(Dictionary<string, Func<object?[]?, object?>> handlers)
        {
            var proxy = DispatchProxy.Create<T, ProxyFactory<T>>();
            ((ProxyFactory<T>)(object)proxy)._handlers = handlers;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null)
            {
                throw new InvalidOperationException("Proxy method is missing.");
            }

            if (_handlers.TryGetValue(targetMethod.Name, out var handler))
            {
                return handler(args);
            }

            throw new NotImplementedException($"{typeof(T).Name}.{targetMethod.Name} was not expected in this test.");
        }
    }

    private static class ProxyFactory
    {
        public static T Create<T>(Dictionary<string, Func<object?[]?, object?>> handlers) where T : class
        {
            return ProxyFactory<T>.Create(handlers);
        }
    }
}
