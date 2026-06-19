using ToyStore.Infrastructure;
using ToyStore.Recommendation;
using ToyStore.Recommendation.Jobs;
using ToyStore.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpContextAccessor();

// Infrastructure layer (includes Application via AddApplication())
builder.Services.AddInfrastructure(builder.Configuration);

// Recommendation layer (DI: MongoDb, Tracking, RecommendationService, ...)
builder.Services.AddRecommendation(builder.Configuration);

// Recommendation background jobs
builder.Services.AddHostedService<FlushEventsJob>();
builder.Services.AddHostedService<ComputeScoresJob>();
builder.Services.AddHostedService<ComputeSimilarityJob>();
builder.Services.AddHostedService<ComputeTrendingJob>();
builder.Services.AddHostedService<UpdateUserProfilesJob>();

// Core system workers
builder.Services.AddHostedService<OrderStatusWorker>();
builder.Services.AddHostedService<PromotionStatusJob>();
builder.Services.AddHostedService<BlogPublishWorker>();
builder.Services.AddHostedService<BlogCommentModerationPollJob>();
builder.Services.AddHostedService<BlogCommentManualReviewTimeoutJob>();
builder.Services.AddHostedService<BlogCommentPermissionUnlockJob>();

// Notification pipeline
builder.Services.AddHostedService<OutboxProcessorJob>();
builder.Services.AddHostedService<EmailDispatchJob>();

// Scheduled notification jobs
builder.Services.AddHostedService<BirthdayNotificationJob>();
builder.Services.AddHostedService<VoucherExpiryReminderJob>();
builder.Services.AddHostedService<FlashSaleActivationJob>();
builder.Services.AddHostedService<LowStockScanJob>();
builder.Services.AddHostedService<PaymentOverdueJob>();
builder.Services.AddHostedService<BackInStockJob>();
builder.Services.AddHostedService<CampaignSchedulerJob>();
builder.Services.AddHostedService<CampaignApprovedExpireJob>();
builder.Services.AddHostedService<CampaignReferenceRevalidationJob>();
builder.Services.AddHostedService<CampaignStaleLockRecoveryJob>();
builder.Services.AddHostedService<ShiftLifecycleJob>();
builder.Services.AddHostedService<OrderQueueRetryJob>();
builder.Services.AddHostedService<OrderAssignmentReconciliationJob>();
builder.Services.AddHostedService<OrderQueuedDigestJob>(); // Digest email mỗi 10 phút thay vì spam từng email
builder.Services.AddHostedService<CustomerDeliveryAbuseScanJob>();

// Checkout flow workers
builder.Services.AddHostedService<SePayExpiryJob>();
builder.Services.AddHostedService<GhnShippingRetryJob>();

// Withdrawal workers
builder.Services.AddHostedService<WithdrawalTimeoutJob>();
builder.Services.AddHostedService<WithdrawalPayoutPollJob>();

var host = builder.Build();
host.Run();
