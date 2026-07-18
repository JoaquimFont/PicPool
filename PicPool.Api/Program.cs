using Microsoft.EntityFrameworkCore;
using PicPool.Api.Hubs;
using PicPool.Infrastructure.Data;
using PicPool.Infrastructure.Services;
using PicPool.Api.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddDbContext<PicPoolDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ServeiUsuaris>();
builder.Services.AddScoped<ServeiSala>();
builder.Services.AddScoped<ServeiNotificacions>();
builder.Services.AddScoped<CloudflareR2Service>();
builder.Services.AddSingleton<SalaOperacioCua>();
builder.Services.AddScoped<ServeiMantenimentSales>();
builder.Services.AddHostedService<MantenimentSalesBackgroundService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.MapHub<SalaHub>("/hubs/sala");

app.UseHttpsRedirection();

app.UseCors("ReactApp");

app.MapControllers();

app.Run();