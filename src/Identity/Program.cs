using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Pitstop.Infrastructure.WebHost.Customization;
using Pitstop.Identity.Data;

namespace Pitstop.Identity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build()
                .MigrateDbContext<ApplicationDbContext>((_, __) => { })
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
