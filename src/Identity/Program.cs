using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Pitstop.Infrastructure.WebHost.Customization;
using Pitstop.Identity.Data;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Pitstop.Identity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build()
                .MigrateDbContext<PersistedGrantDbContext>((_, __) => { })
                .MigrateDbContext<ApplicationDbContext>((_, __) => { })
                .MigrateDbContext<ConfigurationDbContext>((context, services) =>
                {
                    var configuration = services.GetService<IConfiguration>();

                    new ConfigurationDbContextSeed()
                        .SeedAsync(context, configuration)
                        .Wait();
                }).Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
