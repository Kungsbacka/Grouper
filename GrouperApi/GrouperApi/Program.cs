using GrouperLib.Backend;
using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Language;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;

namespace GrouperApi;

[SupportedOSPlatform("windows")]
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.Configure<GrouperConfiguration>(builder.Configuration.GetSection("Grouper"));

        builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            .AddNegotiate();
        builder.Services.AddAuthorization(options =>
        {

            var roleMappings = builder.Configuration.GetSection("RoleMapping")
                .Get<Dictionary<string, string>>();
            if (roleMappings  != null)
            {
                foreach (var roleMapping in roleMappings)
                {
                    options.AddPolicy(roleMapping.Key, policy => policy.RequireRole(roleMapping.Value));
                }
                options.AddPolicy("All", policy =>
                    policy.RequireAssertion(context =>
                        roleMappings.Values.Any(group =>
                            context.User.IsInRole(group)
                        )
                    )
                );
            }
            options.FallbackPolicy = options.DefaultPolicy;
        });

        builder.Services.AddSingleton<IStringResourceHelper, StringResourceHelper>();

        builder.Services.AddSingleton((_) =>
        {
            GrouperConfiguration config = new();
            ConfigurationBinder.Bind(builder.Configuration.GetSection("Grouper"), config);
            return Grouper.CreateFromConfig(config);
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Grouper API", Version = "v1" });
            c.UseAllOfToExtendReferenceSchemas();
            c.MapType<GroupMemberDiff>(() => new OpenApiSchema { Type = "object" });
            c.MapType<GrouperDocument>(() => new OpenApiSchema { Type = "object" });
        });

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Grouper API V1");

            });
        }
        else
        {
            app.UseExceptionHandler("/error");
        }
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}