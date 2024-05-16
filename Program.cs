using System.Reflection;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using TNG.Shared.Lib;
using TNG.Shared.Lib.Communications.Email;
using TNG.Shared.Lib.Mongo.Base;
using TNG.Shared.Lib.Mongo.Master;
using TNG.Shared.Lib.Settings;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Facility auth service",
                Description = "Handles authentication service of app.",
            });
            // Set the comments path for the Swagger JSON and UI.    
            
        });

        configureServices(builder);

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseCors(builder => builder

                    .AllowAnyHeader()

                    .AllowAnyMethod()

                    .SetIsOriginAllowed((host) => true)

                    .AllowCredentials()

                );
        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

    /// <summary>
    /// Configuer all the required services
    /// </summary>
    /// <param name="builder"></param>
    private static void configureServices(WebApplicationBuilder builder)
    {

        builder.Services.AddSingleton<IMongoClient, MongoClient>(
            _ => new MongoClient(builder.Configuration.GetConnectionString("DefaultConnection"))
        );
        builder.Services.AddScoped<IMongoConfigurationService, MongoConfigurationService>(
            _ => new MongoConfigurationService(builder.Configuration.GetConnectionString("Database"), MongoOperationsMode.UNRESTRICTED)
        );
        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IMongoLayer, MongoLayer>();

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.ITNGUtiltityLib, TNGUtilityLib>();

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.ILogger, TNG.Shared.Lib.Logger>();

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IRestLayer, TNG.Shared.Lib.RestLayer>();

        //For retieving IP
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IEmailer, Emailer>(
      _ => new TNG.Shared.Lib.Communications.Email.Emailer(builder.Configuration.GetSection("ConnectionEmail").Get<EMailSettings>()));

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IAuthenticationService, AuthenticationService>(
        _ => new TNG.Shared.Lib.AuthenticationService(builder.Configuration.GetSection("Cryptography").Get<CryptoSettings>()));

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IS3Layer, S3Layer>(

       _ => new TNG.Shared.Lib.S3Layer(builder.Configuration.GetSection("ConnectionsS3").Get<S3LayerSettings>()));




    }
}

