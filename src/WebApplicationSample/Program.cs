using System;
using WebApplicationSample.Handlers;
using WebApplicationSample.Messages;

var builder = WebApplication.CreateBuilder(args);
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", true, reloadOnChange: true);
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddNebulaBus(options =>
{
    options.UseRabbitmq(rabbitmq =>
    {
        rabbitmq.HostName = configuration!.GetValue<string>("RabbitMq:HostName");
        rabbitmq.UserName = configuration!.GetValue<string>("RabbitMq:UserName");
        rabbitmq.Password = configuration!.GetValue<string>("RabbitMq:Password");
        rabbitmq.VirtualHost = configuration!.GetValue<string>("RabbitMq:VirtualHost");
    });
});
builder.Services.AddNebulaBusHandler<TestHandler, TestMessage>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
