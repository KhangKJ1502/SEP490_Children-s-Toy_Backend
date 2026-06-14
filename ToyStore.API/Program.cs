using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using ToyStore.API.Hubs;
using ToyStore.API.Middleware;
using ToyStore.Application.Interfaces.Notifications;
using ToyStore.Infrastructure;
using ToyStore.Recommendation;

var builder = WebApplication.CreateBuilder(args);

// Allow multipart file uploads up to 10 MB (Cloudinary service validates ≤ 5 MB internally)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Accept "HH:mm" and "HH:mm:ss" for TimeSpan fields (e.g. ShiftTemplate start/end times)
        opts.JsonSerializerOptions.Converters.Add(new ToyStore.API.Extensions.FlexibleTimeSpanConverter());
        opts.JsonSerializerOptions.Converters.Add(new ToyStore.API.Extensions.FlexibleNullableTimeSpanConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "ToyStore API",
        Version = "v1",
        Description = "API for Children Toy E-Commerce Platform"
    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Enter JWT token only (without Bearer prefix).",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRecommendation(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, AccountIdProvider>();

// Override NoOp hub service (registered by Infrastructure) with real SignalR implementation
builder.Services.AddScoped<INotificationHubService, NotificationHubService>();
builder.Services.AddScoped<ToyStore.Application.Interfaces.Services.ICartRealtimeService, CartRealtimeService>();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed for {Path}: {Message}", 
                    context.HttpContext.Request.Path, context.Exception.Message);
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    (path.StartsWithSegments("/hubs/cart") || path.StartsWithSegments("/hubs/notifications")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Orders.Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Orders.Operational", policy => policy.RequireRole("Staff", "Merchandise", "Admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

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
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAccountStatusGuard();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CartHub>("/hubs/cart");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
