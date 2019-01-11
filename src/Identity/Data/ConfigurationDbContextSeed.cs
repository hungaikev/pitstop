using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pitstop.Identity.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pitstop.Identity.Data
{
    public class ConfigurationDbContextSeed
    {
        public async Task SeedAsync(ConfigurationDbContext context, IConfiguration configuration)
        {

            //callbacks urls from config:
            var clientUrls = new Dictionary<string, string>();

            clientUrls.Add("Mvc", configuration.GetValue<string>("WebAppClient"));
            clientUrls.Add("Workshop", configuration.GetValue<string>("WorkshopManagementApi"));
            clientUrls.Add("Customer", configuration.GetValue<string>("CustomerManagementApi"));
            clientUrls.Add("Vehicle", configuration.GetValue<string>("VehicleManagementApi"));


            if (!context.Clients.Any())
            {
                foreach (var client in Config.GetClients(clientUrls))
                {
                    context.Clients.Add(client.ToEntity());
                }
                await context.SaveChangesAsync();
            }
            
            if (!context.IdentityResources.Any())
            {
                foreach (var resource in Config.GetResources())
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }

            if (!context.ApiResources.Any())
            {
                foreach (var api in Config.GetApis())
                {
                    context.ApiResources.Add(api.ToEntity());
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
