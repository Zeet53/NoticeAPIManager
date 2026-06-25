using DataBaseAPI.Services;
using DotNetEnv;
var builder = WebApplication.CreateBuilder(args);

Env.Load();
var envPath = Path.Combine(builder.Environment.ContentRootPath, "..", ".env");
Env.Load(envPath);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI registrations
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IStatusUpdateService, StatusUpdateService>();
builder.Services.AddHostedService<ArchiveBackgroundService>();
builder.Services.AddHostedService<StatusConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
