using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;
using SecOpsSteward.Data;
using SecOpsSteward.Integrations.Azure;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.NonceTracking;
using SecOpsSteward.Shared.Packaging;
using SecOpsSteward.Shared.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.UI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Instance = this;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static Startup Instance { get; private set; }
        public bool RunDemoMode => Configuration.GetValue("RunDemoMode", false);
        public bool HasAuthConfiguration => !RunDemoMode && Configuration.GetSection("AzureAd").Exists();
        public bool UseDummyServices => RunDemoMode || Configuration.GetValue<bool>("UseDummyServices");
        public static bool LockDiscovery => Instance.RunDemoMode || Instance.Configuration.GetValue<bool>("DisableDiscovery", false);
        private void RegisterAuthXServices(IServiceCollection services)
        {
            if (HasAuthConfiguration)
            {
                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                    .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddDownstreamWebApi("AzureRM", co => { co.BaseUrl = "https://management.azure.com/"; co.Scopes = "https://management.azure.com//user_impersonation"; })
                    .AddDownstreamWebApi("Graph", co => { co.BaseUrl = "https://graph.windows.net/"; co.Scopes = "https://graph.windows.net//user.read"; })
                    .AddDownstreamWebApi("KeyVault", co => { co.BaseUrl = "https://vault.azure.net/"; co.Scopes = "https://vault.azure.net//user_impersonation"; })
                    .AddDownstreamWebApi("ServiceBus", co => { co.BaseUrl = "https://servicebus.azure.net/"; co.Scopes = "https://servicebus.azure.net//user_impersonation"; })
                    .AddInMemoryTokenCaches();
                services.AddControllersWithViews()
                    .AddMicrosoftIdentityUI();

                services.AddAuthorization(options =>
                {
                    // By default, all incoming requests will be authorized according to the default policy
                    options.FallbackPolicy = options.DefaultPolicy;
                });

                services.AddServerSideBlazor()
                        .AddMicrosoftIdentityConsentHandler();
            }
            else
            {
                services.AddServerSideBlazor();
            }

            // Register TokenOwner for user information
            services.AddScoped<TokenOwner>(s => TokenOwner.Create(
                s.GetRequiredService<AuthenticationStateProvider>().GetAuthenticationStateAsync().Result,
                HasAuthConfiguration));
        }
        private ChimeraServiceConfigurator RegisterChimeraServices(IServiceCollection services)
        {
            var config = new ChimeraServiceConfigurator(new Dictionary<string, string>()
            {
                { "SubscriptionId", Configuration.GetSection("Chimera")["SubscriptionId"] },
                { "ResourceGroup", Configuration.GetSection("Chimera")["ResourceGroup"] },
                { "VaultName", Configuration.GetSection("Chimera")["VaultName"] },
                { "PackageRepoAccount", Configuration.GetSection("Chimera")["PackageRepoAccount"] },
                { "PackageRepoContainer", Configuration.GetSection("Chimera")["PackageRepoContainer"] },
                { "NonceAccount", Configuration.GetSection("Chimera")["NonceAccount"] },
                { "NonceContainer", Configuration.GetSection("Chimera")["NonceContainer"] },
                { "ServiceBusNamespace", Configuration.GetSection("Chimera")["ServiceBusNamespace"] },
                { "SignDecryptRole", Configuration.GetSection("Chimera")["SignDecryptRole"] },
                { "VerifyEncryptRole", Configuration.GetSection("Chimera")["VerifyEncryptRole"] },
            });


            if (!UseDummyServices)
                services.AddAzurePlatformIntegrations();
            else
                services.AddChimeraDummyIntegrations(true);


            // If running locally, this factory is registered to use managed identity (provided by the environment)
            services.RegisterCurrentCredentialFactory(Configuration.GetSection("AzureAd")["TenantId"], config["SubscriptionId"], false, UseDummyServices);

            // This is for executions local to the _web server_ ... don't bother with this (for now)
            services.AddScoped<INonceTrackingService, NoNonceTrackingService>();

            // Chimera core
            services.AddChimera(config);

            // Bindings to Chimera API and data repo
            services.AddTransient<DataBoundApi>();

            // Message processing (as user)
            services.AddScoped<WorkflowMessageProcessorService>();

            // Public package repo
            services.AddSingleton<PublicPackageRepository>(s => new PublicPackageRepository(Configuration.GetConnectionString("PublicPackages")));

            return config;
        }
        private void RegisterDatabaseServices(IServiceCollection services)
        {
            services.AddDbContextFactory<SecOpsStewardDbContext>(options =>
            {
                options.EnableDetailedErrors(true);
                if (UseDummyServices)
                    options.UseSqlite("Data Source=sos.db")
                           .EnableSensitiveDataLogging(true);

                else if (Configuration.GetConnectionString("Database") != null)
                    options.UseSqlServer(Configuration.GetConnectionString("Database"));
                else if (Environment.GetEnvironmentVariable("SQLAZURECONNSTR_Database") != null)
                    options.UseSqlServer(Environment.GetEnvironmentVariable("SQLAZURECONNSTR_Database"));

                else throw new Exception("No database configuration specified!");
            });

            services.AddTransient<SecOpsStewardDbContext>(p => p.GetRequiredService<IDbContextFactory<SecOpsStewardDbContext>>().CreateDbContext());
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Register logger
            services.AddLogging(c => { c.AddConsole().AddAnsiConsoleFormatter(); });

            // Register AuthX services
            RegisterAuthXServices(services);

            // Database
            RegisterDatabaseServices(services);

            // Register Chimera services
            var chimeraConfig = RegisterChimeraServices(services);

            // Set up Razor components
            services.AddRazorPages();
            services.AddMudServices();
            services.AddBlazoredSessionStorage();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            if (HasAuthConfiguration)
            {
                app.UseAuthentication();
                app.UseAuthorization();
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            // --- Dummy mode ---
            if (UseDummyServices)
            {
                Task.Delay(1000).Wait();
                using (var cxt = serviceProvider.GetRequiredService<SecOpsStewardDbContext>())
                {
                    var api = serviceProvider.GetRequiredService<DataBoundApi>();

                    if (!cxt.Agents.Any())
                    {
                        Task.WhenAll(
                            api.AddAgent("Sample A", Guid.NewGuid()),
                            api.AddAgent("Sample B", Guid.NewGuid())).GetAwaiter().GetResult();
                    }

                    if (!cxt.Users.Any())
                    {
                        Task.WhenAll(
                            api.AddUser(TokenOwner.Create(null, false)),
                            api.AddUser("bob"),
                            api.AddUser("jane")).GetAwaiter().GetResult();
                    }
                }
            }
        }
    }
}
