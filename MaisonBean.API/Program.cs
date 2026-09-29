using MaisonBean.API.Middleware;
using MaisonBean.Application.AI.Interfaces;
using MaisonBean.Application.Common;
using MaisonBean.Application.Interfaces;
using MaisonBean.Application.Orders.Commands;
using MaisonBean.Application.Payments.Interfaces;
using MaisonBean.Domain.Entities;
using MaisonBean.Domain.Enums;
using MaisonBean.Infrastructure;
using MaisonBean.Infrastructure.AI.Conversations;
using MaisonBean.Infrastructure.AI.LocalAI;
using MaisonBean.Infrastructure.AI.OpenAI;
using MaisonBean.Infrastructure.AI.Prompting;
using MaisonBean.Infrastructure.AI.VectorDatabase;
using MaisonBean.Infrastructure.Payments;
using MaisonBean.Infrastructure.Persistence;
using MaisonBean.Infrastructure.Persistence.Repositories;
using MaisonBean.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MaisonBean.Application.Interfaces;
using MaisonBean.Infrastructure.Configurations;
using MaisonBean.Infrastructure.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddInfrastructure(builder.Configuration);

// INFRASTRUCTURE
builder.Services.AddInfrastructure(builder.Configuration );
// MEDIATR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(PlaceOrderHandler).Assembly) );
// OPENAI
builder.Services.Configure<OpenAIOptions>(
    builder.Configuration.GetSection("OpenAI"));
builder.Services.AddHttpClient();
// CLOUDINARY
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection( "CloudinarySettings") );
builder.Services.AddScoped<IImageService, CloudinaryService>();
// JWT SETTINGS
var jwtSettings = builder.Configuration .GetSection("JwtSettings").Get<JwtSettings>()!;
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
// AUTHENTICATION
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                RoleClaimType = "ROLE",
                NameClaimType = "id",
                ClockSkew = TimeSpan.Zero
            };

        options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["accessToken"];
                        return Task.CompletedTask;
                    },

                OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    },

                OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    }
            };
    });

// AUTHORIZATION
builder.Services.AddAuthorization();


// RATE LIMITING
builder.Services.AddRateLimiter(options =>
{
    // GLOBAL LIMITER
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey: ip,

                        factory: _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 100,
                                Window = TimeSpan.FromMinutes(1),
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 2
                            });
            });

    // LOGIN POLICY
    options.AddFixedWindowLimiter("login", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });

     // CART POLICY
    options.AddFixedWindowLimiter("cart", limiterOptions =>
        {
            limiterOptions.PermitLimit = 30;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });

     // WISHLIST POLICY
    options.AddFixedWindowLimiter("wishlist", limiterOptions =>
        {
            limiterOptions.PermitLimit = 20;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });

     // CHECKOUT POLICY
    options.AddFixedWindowLimiter("checkout",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });

    // AI POLICY
    options.AddFixedWindowLimiter("ai",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 15;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });

    // RESPONSE
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(
                new
                {
                    success = false,
                    message = "Too many requests. Please try again later."
                },
                cancellationToken: token
            );
    };

}); 

// ======================================================
// COOKIE POLICY
// ======================================================

//builder.Services.Configure<
//    CookiePolicyOptions>(options =>
//    {
//        options.HttpOnly =
//            HttpOnlyPolicy.Always;

//        options.MinimumSameSitePolicy =
//            SameSiteMode.Unspecified;

//        options.Secure =
//            builder.Environment.IsDevelopment()
//                ? CookieSecurePolicy.None
//                : CookieSecurePolicy.Always;
//    });


builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.HttpOnly = HttpOnlyPolicy.Always;
        options.MinimumSameSitePolicy = SameSiteMode.None;
        options.Secure = CookieSecurePolicy.Always;
    });


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => {
        policy
        .WithOrigins("http://localhost:5173",
        "https://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// CONTROLLERS
builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options
            .JsonSerializerOptions
            .Converters
            .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        options
            .JsonSerializerOptions
            .ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
// REPOSITORIES
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
// AI SERVICES
builder.Services.AddHttpClient<IAIChatService, OllamaChatService>();
builder.Services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IPromptService, PromptBuilderService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// PAYMENT
builder.Services.AddScoped<IPaymentService, RazorpayService>();
// SWAGGER
builder.Services.AddSwaggerGen(c =>
{
    c.UseInlineDefinitionsForEnums();
    c.MapType<OrderStatus>(() =>
        new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "string",
            Enum = Enum.GetNames(typeof(OrderStatus))
                .Select(x => (Microsoft.OpenApi.Any.IOpenApiAny)
                    new Microsoft.OpenApi.Any.OpenApiString(x)).ToList()
        });
});

// BUILD APP
var app = builder.Build();

// SEED ADMIN
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
    await DbSeeder.SeedAdminAsync(userManager, roleManager);
}

// CREATE ROLES
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<int>>>();

    string[] roles = { "ADMIN", "CUSTOMER" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }
}

// MIDDLEWARE
app.UseHttpsRedirection();
app.UseMiddleware<IpWhitelistMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MaisonBean API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseCookiePolicy();
app.UseAuthentication();
app.UseMiddleware<BlockedUserMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
// RUN
app.Run();