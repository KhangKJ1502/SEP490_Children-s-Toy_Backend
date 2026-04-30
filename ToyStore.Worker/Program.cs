using ToyStore.Infrastructure;
using ToyStore.Recommendation;
using ToyStore.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Add Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);

// Add Recommendation layer
builder.Services.AddRecommendation();

// Add hosted services (workers)
builder.Services.AddHostedService<OrderStatusWorker>();
builder.Services.AddHostedService<RecommendationWorker>();
builder.Services.AddHostedService<BlogPublishWorker>();

var host = builder.Build();
host.Run();
