using ToyStore.API.Middleware;
using ToyStore.Infrastructure;
using ToyStore.Recommendation;
using ToyStore.Chatbot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "ToyStore API", 
        Version = "v1",
        Description = "API for Children Toy E-Commerce Platform"
    });
});

// Add layers via dependency injection
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRecommendation();
builder.Services.AddChatbot();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ToyStore API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseExceptionHandling();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
