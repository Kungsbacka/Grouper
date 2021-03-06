using GrouperLib.Backend;
using GrouperLib.Config;
using GrouperLib.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GrouperApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson();
            services.Configure<GrouperConfiguration>(Configuration.GetSection("Grouper"));
            GrouperConfiguration config = new GrouperConfiguration();
            ConfigurationBinder.Bind(Configuration.GetSection("Grouper"), config);
            services.AddSingleton<IStringResourceHelper, StringResourceHelper>();
            services.AddSingleton((_) =>
            {
                return Grouper.CreateFromConfig(config);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/error");
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
