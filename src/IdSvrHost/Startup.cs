using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using IdSvrHost.Configuration;
using IdSvrHost.Extensions;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace IdSvrHost
{
    public class Startup
    {
        private readonly IApplicationEnvironment _environment;

        public Startup(IHostingEnvironment env, IApplicationEnvironment environment)
        {
            _environment = environment;
            var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
                
            if (env.IsDevelopment())
            {
                builder.AddUserSecrets();                
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();   
        }
        
        public IConfigurationRoot Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var cert = new X509Certificate2(Path.Combine(_environment.ApplicationBasePath, "idsrv4test.pfx"), "idsrv3test");

            var builder = services.AddIdentityServer(options =>
            {
                options.SigningCertificate = cert;                                
            });

            builder.AddInMemoryClients(Clients.Get());
            builder.AddInMemoryScopes(Scopes.Get());
            builder.AddInMemoryUsers(Users.Get());

            builder.AddCustomGrantValidator<CustomGrantValidator>();            

            // for the UI
            services
                .AddMvc()
                .AddRazorOptions(razor =>
                {
                    razor.ViewLocationExpanders.Add(new UI.CustomViewLocationExpander());
                });
            services.AddTransient<UI.Login.LoginService>();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Verbose);
            loggerFactory.AddDebug(LogLevel.Verbose);

            app.UseDeveloperExceptionPage();
            app.UseIISPlatformHandler();

//            app.UseGoogleAuthentication(options =>
//            {
//                options.AuthenticationScheme = "Google";
//                options.SignInScheme = IdentityServer4.Core.Constants.PrimaryAuthenticationType;
//
//                options.ClientId = Configuration["GoogleIdentityProvider:ClientId"];
//                options.ClientSecret = Configuration["GoogleIdentityProvider:ClientSecret"];
////                options.CallbackPath = new PathString("");
//            });

            app.UseIdentityServer();

            app.UseCookieAuthentication(options =>
            {
                options.AuthenticationScheme = "External";
            });

            app.UseGoogleAuthentication(options =>
            {
                options.AuthenticationScheme = "Google";
                options.SignInScheme = "External";

                options.ClientId = Configuration["GoogleIdentityProvider:ClientId"];
                options.ClientSecret = Configuration["GoogleIdentityProvider:ClientSecret"];
                options.CallbackPath = new PathString("/login/googlecallback");
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = t =>
                    {
                        return Task.FromResult(0);
                    },
                    OnRemoteError = e =>
                    {
                        return Task.FromResult(0);
                    },
                    OnTicketReceived = t =>
                    {
                        return Task.FromResult(0);
                    }
                };
            });

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
