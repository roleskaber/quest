using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReQuest_backend.Server;
using ReQuest_backend.Server.Auth;
using ReQuest_backend.Server.Database.Quest;
using ReQuest_backend.Server.QuestSession;
using ReQuest_backend.Server.Translation;
using ReQuest_backend.Server.TriviaAPI;

var builder = WebApplication.CreateBuilder(args);
var databaseUri = Environment.GetEnvironmentVariable("DATABASE_URI");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? databaseUri;

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DATABASE_URI or ConnectionStrings:DefaultConnection must be configured.");
}

builder.Services.AddOpenApi();
builder.Services.AddDataProtection();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5094", "https://localhost:7244")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<TriviaApiService>();
builder.Services.AddHttpClient<QuestionTranslationService>();
builder.Services.AddDbContext<QuestContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<GameSessionStore>();
builder.Services.AddSingleton<AuthTokenService>();
builder.Services.AddScoped<QuestRepository>();
builder.Services.AddScoped<QuestService>();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        var builtInFactory = options.InvalidModelStateResponseFactory;
        options.InvalidModelStateResponseFactory = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            return builtInFactory(context);
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuestContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseCors("frontend");
app.MapControllers();

app.Run();