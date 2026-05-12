using ToyStore.Infrastructure;
using ToyStore.Recommendation;
using ToyStore.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpContextAccessor();

// Infrastructure layer (includes Application via AddApplication())
builder.Services.AddInfrastructure(builder.Configuration);

// Recommendation layer
builder.Services.AddRecommendation();

// ── Background workers / hosted services ────────────────────────────────────
// Core system workers
builder.Services.AddHostedService<OrderStatusWorker>();
builder.Services.AddHostedService<PromotionStatusJob>();
builder.Services.AddHostedService<RecommendationWorker>();
builder.Services.AddHostedService<BlogPublishWorker>();

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
builder.Services.AddHostedService<AutoCompleteOrderJob>();

// Campaign sender (uses ICampaignNotificationService — not direct DB bulk-insert)
builder.Services.AddHostedService<CampaignSenderWorker>();

var host = builder.Build();
host.Run();
