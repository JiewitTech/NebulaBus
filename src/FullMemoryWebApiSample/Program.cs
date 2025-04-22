using LogicSamples;
using LogicSamples.Handlers;
using NebulaBus.Store.Memory;

namespace FullMemoryWebApiSample;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://*:55891");
        builder.Logging.AddConsole();
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
            options.ExecuteThreadCount = 1;
            options.UseMemoryTransport();
            options.UserMemoryStore();
        });
        builder.Services.AddNebulaBusHandler(typeof(TestHandlerV1).Assembly);
        //Add Global Handler Filter
        builder.Services.AddNebulaBusFilter<GlobalHandlerFilter>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment() || environment == "Local")
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}