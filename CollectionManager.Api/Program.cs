using CollectionManager.Api.Auth;
using CollectionManager.Api.Data;
using CollectionManager.Api.Services;
using CollectionManager.Api;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<PostgresConnectionFactory>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CollectionService>();
builder.Services.AddScoped<TradeService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddAuthentication("Bearer").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, JwtAuthenticationHandler>("Bearer", _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Collector", p => p.RequireRole("Collector"));
    options.AddPolicy("Evaluator", p => p.RequireRole("Evaluator"));
    options.AddPolicy("Admin", p => p.RequireRole("Admin"));
});
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "КОЛЕКЦИОНЕРСКИ СВЯТ — API", Version = "v1", Description = "API за колекции, размени, търгове и съобщения." });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Въведи JWT токена, получен от /api/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("CollectionDb")))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PostgresConnectionFactory>();
    await DatabaseInitializer.InitializeAsync(database);
}
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (DomainException ex) when (!context.Response.HasStarted)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message, status = ex.StatusCode });
    }
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService service) => Results.Ok(await service.Login(request)));
app.MapPost("/api/auth/register-collector", async (RegisterRequest request, AuthService service) => Results.Created("/api/auth/login", await service.Register(request, "Collector")));
app.MapPost("/api/auth/register-evaluator", async (RegisterRequest request, AuthService service) => Results.Created("/api/auth/login", await service.Register(request, "Evaluator")));
app.MapGet("/api/profile/me", async (HttpContext c, ProfileService s) => Results.Ok(await s.Mine(c.User))).RequireAuthorization();
app.MapPut("/api/profile/me", async (ProfileInput input, HttpContext c, ProfileService s) => Results.Ok(await s.Update(c.User, input))).RequireAuthorization();
app.MapGet("/api/users/{id:int}/profile", async (int id, ProfileService s) => Results.Ok(await s.Get(id))).RequireAuthorization();
app.MapGet("/api/users/search", async (string? query, HttpContext c, ProfileService s) => Results.Ok(await s.SearchPeople(query, c.User))).RequireAuthorization("Collector");
app.MapPost("/api/media/upload", async (HttpRequest request, IWebHostEnvironment env) =>
{
    const long maxBytes = 5 * 1024 * 1024;
    if (!request.HasFormContentType) return Results.BadRequest(new { error = "Избери файл за качване." });
    var form = await request.ReadFormAsync(); var file = form.Files.GetFile("file");
    if (file is null || file.Length is 0 or > maxBytes) return Results.BadRequest(new { error = "Снимката трябва да е до 5 MB." });
    var extension = file.ContentType.ToLowerInvariant() switch { "image/jpeg" => ".jpg", "image/png" => ".png", "image/webp" => ".webp", _ => null };
    if (extension is null) return Results.BadRequest(new { error = "Поддържат се JPG, PNG и WebP снимки." });
    var imageBytes = new byte[(int)file.Length];
    await using (var input = file.OpenReadStream()) await input.ReadExactlyAsync(imageBytes);
    var validImage = extension switch
    {
        ".jpg" => imageBytes.Length >= 3 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF,
        ".png" => imageBytes.Length >= 8 && imageBytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        ".webp" => imageBytes.Length >= 12 && imageBytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && imageBytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
    if (!validImage) return Results.BadRequest(new { error = "Файлът не е валидна JPG, PNG или WebP снимка." });
    var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    var folder = Path.Combine(webRoot, "uploads"); Directory.CreateDirectory(folder);
    var fileName = $"{Guid.NewGuid():N}{extension}"; var path = Path.Combine(folder, fileName);
    await File.WriteAllBytesAsync(path, imageBytes);
    return Results.Ok(new { url = $"/uploads/{fileName}" });
}).RequireAuthorization().DisableAntiforgery();

app.MapGet("/api/items", async (HttpContext c, CollectionService s, string? status, int? categoryId, string? search) =>
    Results.Ok(await s.List(c.User, status, categoryId, search))).RequireAuthorization();
app.MapGet("/api/items/{id:int}", async (int id, HttpContext c, CollectionService s) =>
    Results.Ok(await s.Get(id, c.User))).RequireAuthorization();
app.MapPost("/api/items", async (ItemInput input, HttpContext c, CollectionService s) =>
    Results.Created("/api/items", await s.Create(input, c.User))).RequireAuthorization("Collector");
app.MapPut("/api/items/{id:int}", async (int id, ItemInput input, HttpContext c, CollectionService s) =>
    Results.Ok(await s.Update(id, input, c.User))).RequireAuthorization("Collector");
app.MapDelete("/api/items/{id:int}", async (int id, HttpContext c, CollectionService s) =>
    { await s.Delete(id, c.User); return Results.NoContent(); }).RequireAuthorization("Collector");
app.MapPost("/api/items/{id:int}/offer-for-trade", async (int id, HttpContext c, CollectionService s) =>
    Results.Ok(await s.ListForTrade(id, c.User))).RequireAuthorization("Collector");
app.MapPost("/api/items/{id:int}/request-auction", async (int id, HttpContext c, CollectionService s) =>
    Results.Ok(await s.RequestAuction(id, c.User))).RequireAuthorization("Collector");
app.MapPost("/api/items/{id:int}/withdraw-trade", async (int id, HttpContext c, CollectionService s) =>
    Results.Ok(await s.WithdrawTradeListing(id, c.User))).RequireAuthorization("Collector");
app.MapPost("/api/items/{id:int}/withdraw-auction", async (int id, HttpContext c, CollectionService s) =>
    Results.Ok(await s.WithdrawAuctionRequest(id, c.User))).RequireAuthorization("Collector");
app.MapPost("/api/items/{id:int}/approval", async (int id, ApprovalInput input, HttpContext c, CollectionService s) =>
    Results.Ok(await s.ReviewAuction(id, input.Approve, c.User))).RequireAuthorization("Evaluator");
app.MapPost("/api/items/{id:int}/bids", async (int id, BidInput input, HttpContext c, TradeService s) =>
    Results.Ok(await s.PlaceBid(id, input.Amount, c.User))).RequireAuthorization("Collector");
app.MapPost("/api/trades", async (TradeInput input, HttpContext c, TradeService s) =>
    Results.Created("/api/trades", await s.CreateTrade(input, c.User))).RequireAuthorization("Collector");
app.MapGet("/api/trades", async (HttpContext c, TradeService s) => Results.Ok(await s.MyTrades(c.User))).RequireAuthorization("Collector");
app.MapGet("/api/items/{id:int}/activity", async (int id, HttpContext c, TradeService s) => Results.Ok(await s.ItemActivity(id, c.User))).RequireAuthorization();
app.MapPost("/api/trades/{id:int}/accept", async (int id, HttpContext c, TradeService s) =>
    Results.Ok(await s.AcceptTrade(id, c.User))).RequireAuthorization("Collector");
app.MapPost("/api/auctions/close-due", async (HttpContext c, TradeService s) =>
    Results.Ok(new { closed = await s.CloseDueAuctions() })).RequireAuthorization("Admin");

app.MapGet("/api/notifications", async (HttpContext c, NotificationService s) => Results.Ok(await s.List(c.User))).RequireAuthorization();
app.MapGet("/api/notifications/unread-count", async (HttpContext c, NotificationService s) => Results.Ok(await s.UnreadCount(c.User))).RequireAuthorization();
app.MapPost("/api/notifications/{id:int}/read", async (int id, HttpContext c, NotificationService s) => { await s.MarkRead(id, c.User); return Results.NoContent(); }).RequireAuthorization();
app.MapPost("/api/notifications/read-all", async (HttpContext c, NotificationService s) => { await s.MarkAllRead(c.User); return Results.NoContent(); }).RequireAuthorization();

app.MapGet("/api/categories", async (AdminService s) => Results.Ok(await s.Categories()));
app.MapPost("/api/admin/categories", async (CategoryInput input, AdminService s) => Results.Created("/api/categories", await s.AddCategory(input.Name))).RequireAuthorization("Admin");
app.MapDelete("/api/admin/categories/{id:int}", async (int id, AdminService s) => { await s.DeleteCategory(id); return Results.NoContent(); }).RequireAuthorization("Admin");

app.MapGet("/api/messages/{otherUserId:int}", async (int otherUserId, HttpContext c, MessageService s) => Results.Ok(await s.Conversation(otherUserId, c.User))).RequireAuthorization("Collector");
app.MapGet("/api/messages/contacts", async (HttpContext c, MessageService s) => Results.Ok(await s.Contacts(c.User))).RequireAuthorization("Collector");
app.MapPost("/api/messages", async (MessageInput input, HttpContext c, MessageService s) => Results.Created("/api/messages", await s.Send(input, c.User))).RequireAuthorization();

app.MapGet("/api/admin/summary", async (AdminService s) => Results.Ok(await s.Summary())).RequireAuthorization("Admin");
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program { }
