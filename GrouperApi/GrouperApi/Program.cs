using GrouperLib.Backend;
using GrouperLib.Config;
using GrouperLib.Language;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Newtonsoft.Json.Converters;

[assembly: CLSCompliant(false)]
namespace GrouperApi
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen()
                .AddSwaggerGenNewtonsoftSupport();

            builder.Services.Configure<GrouperConfiguration>(builder.Configuration.GetSection("Grouper"));

            builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = options.DefaultPolicy;
            });

            builder.Services.AddSingleton<IStringResourceHelper, StringResourceHelper>();

            builder.Services.AddSingleton((_) =>
                {
                GrouperConfiguration config = new();
                ConfigurationBinder.Bind(builder.Configuration.GetSection("Grouper"), config);
                return Grouper.CreateFromConfig(config);
                });

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
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
}