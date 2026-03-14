using RedRat3ControllerWebServer.Services;
using RedRat3ControllerWebServer.WebSocketHandlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register hardware services as singletons
builder.Services.AddSingleton<RedRatService>();
builder.Services.AddSingleton<SerialPortService>();
builder.Services.AddSingleton<CameraService>();
builder.Services.AddSingleton<AudioStreamingService>();

// Register WebSocket handlers
builder.Services.AddSingleton<StatusWebSocketHandler>();
builder.Services.AddSingleton<AudioWebSocketHandler>();

// Add CORS
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
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.UseWebSockets();

app.MapControllers();

// Enable static files for web frontend
app.UseStaticFiles();

// Serve index.html at root
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

// Initialize RedRat on startup
var redRatService = app.Services.GetRequiredService<RedRatService>();
Task.Run(() => redRatService.Initialize());

app.Run();