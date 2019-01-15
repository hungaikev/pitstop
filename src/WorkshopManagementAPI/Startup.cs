using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pitstop.Infrastructure.Messaging;
using Swashbuckle.AspNetCore.Swagger;
using AutoMapper;
using Pitstop.WorkshopManagementAPI.Repositories;
using Pitstop.WorkshopManagementAPI.Commands;
using Pitstop.WorkshopManagementAPI.Events;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.Extensions.HealthChecks;
using Consul;
using Pitstop.Infrastructure.ServiceDiscovery;
using WorkshopManagementAPI.CommandHandlers;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Collections.Generic;
using Pitstop.Application.WorkshopManagementAPI.Behaviours;

namespace Pitstop.WorkshopManagementAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Environment = env;
        }

        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // add repo classes
            var eventStoreConnectionString = Configuration.GetConnectionString("EventStoreCN");
            services.AddTransient<IWorkshopPlanningRepository>((sp) => 
                new SqlServerWorkshopPlanningRepository(eventStoreConnectionString));

            var workshopManagementConnectionString = Configuration.GetConnectionString("WorkshopManagementCN");
            services.AddTransient<IVehicleRepository>((sp) => new SqlServerRefDataRepository(workshopManagementConnectionString));
            services.AddTransient<ICustomerRepository>((sp) => new SqlServerRefDataRepository(workshopManagementConnectionString));

            // add messagepublisher classes
            var configSection = Configuration.GetSection("RabbitMQ");
            string host = configSection["Host"];
            string userName = configSection["UserName"];
            string password = configSection["Password"];
            services.AddTransient<IMessagePublisher>((sp) => new RabbitMQMessagePublisher(host, userName, password, "Pitstop"));

            // add commandhandlers
            services.AddTransient<IRegisterPlanningCommandHandler, RegisterPlanningCommandHandler>();
            services.AddTransient<IPlanMaintenanceJobCommandHandler, PlanMaintenanceJobCommandHandler>();
            services.AddTransient<IFinishMaintenanceJobCommandHandler, FinishMaintenanceJobCommandHandler>();

            // add consul
            services.Configure<ConsulConfig>(Configuration.GetSection("consulConfig"));
            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                var address = Configuration["consulConfig:address"];
                consulConfig.Address = new Uri(address);
            }));

            // prevent from mapping "sub" claim to nameidentifier.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var identityUrl = Configuration.GetValue<string>("IdentityUrl");
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = identityUrl;
                options.RequireHttpsMetadata = false;
                options.Audience = "workshop";
            });

            // Add framework services.
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "WorkshopManagement API", Version = "v1" });
                c.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "implicit",
                    AuthorizationUrl = $"{Configuration.GetValue<string>("IdentityUrl")}/connect/authorize",
                    TokenUrl = $"{Configuration.GetValue<string>("IdentityUrl")}/connect/token",
                    Scopes = new Dictionary<string, string>()
                    {
                        { "workshop", "Workshop Management API Service" }
                    }
                });

                c.OperationFilter<AuthorizeCheckOperationFilter>();
            });

            services.AddHealthChecks(checks =>
            {
                checks.WithDefaultCacheDuration(TimeSpan.FromSeconds(1));
                checks.AddSqlCheck("EventStoreCN", Configuration.GetConnectionString("EventStoreCN"));
                checks.AddSqlCheck("WorkshopManagementCN", Configuration.GetConnectionString("WorkshopManagementCN"));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();
            // Important to register MVC pipeline after Authentication
            app.UseMvc();

            SetupAutoMapper();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwagger()
               .UseSwaggerUI(c =>
               {
                   c.SwaggerEndpoint("/swagger/v1/swagger.json", "WorkshopManagement API - v1");
                   c.OAuthClientId("workshopswaggerui");
                   c.OAuthAppName("Workshop Management API Swagger UI");
               });

            // register service in Consul
            app.RegisterWithConsul(lifetime);
        }

        private void SetupAutoMapper()
        {
            // setup automapper
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<PlanMaintenanceJob, MaintenanceJobPlanned>()
                    .ForCtorParam("messageId", opt => opt.MapFrom(c => Guid.NewGuid()));
                cfg.CreateMap<FinishMaintenanceJob, MaintenanceJobFinished>()
                    .ForCtorParam("messageId", opt => opt.MapFrom(c => Guid.NewGuid()));
            });
        }      
    }
}
