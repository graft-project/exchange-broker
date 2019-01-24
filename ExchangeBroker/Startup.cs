using ExchangeBroker.Data;
using ExchangeBroker.Middleware;
using ExchangeBroker.Services;
using Graft.Infrastructure;
using Graft.Infrastructure.Rate;
using Graft.Infrastructure.Watcher;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReflectionIT.Mvc.Paging;
using System;

namespace ExchangeBroker
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }
        public string ConnectionString { get; private set; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConnectionString = ConfigUtils.GetConnectionString(Configuration, "EB");

            Console.WriteLine(ConnectionString);

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSingleton<IRateCache, RateCache>();
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<IBitcoinService, BitcoinService>();
            services.AddSingleton<IGraftWalletService, GraftWalletService>();
            services.AddSingleton<IEthereumService, EthereumService>();
            services.AddSingleton<ICryptoProviderService, CryptoProviderService>();

            services.AddScoped<IExchangeService, ExchangeService>();
            services.AddScoped<IPaymentService, PaymentService>();

            services.AddHttpContextAccessor();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(ConnectionString));

            services.AddMvc()
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
            .AddRazorPagesOptions(options =>
            {
                options.AllowAreas = true;
                options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
            });

            services.AddPaging(options =>
            {
                options.ViewName = "Bootstrap4";
                options.HtmlIndicatorDown = " <span>&darr;</span>";
                options.HtmlIndicatorUp = " <span>&uarr;</span>";
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info { Title = "GRAFT Exchange Broker API", Version = "v1" });
            });

            services.AddWatcher(options =>
            {
                options.CheckPeriodMs = Convert.ToInt32(Configuration["Watcher:CheckPeriod"] ?? "60000");
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ApplicationDbContext context,
            ILoggerFactory loggerFactory, WatcherService watcher, IEmailSender emailSender, IRateCache rateCache,
            IBitcoinService bitcoinService, IGraftWalletService graftWalletService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "GRAFT Exchange Broker API V1");
            });

            app.UseMiddleware();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            watcher.Add(rateCache);
            watcher.Add(emailSender);
            watcher.Add(bitcoinService);
            watcher.Add(graftWalletService);

            // comment this line if you want to apply the migrations as a separate process
            context.Database.Migrate();

            BitcoinService.Init(options =>
            {
                options.IsTestNetwork = Configuration["BitcoinService:NetworkType"] != "MainNet";
                options.TryCount = 2;
                options.DelayMs = 2000;
                options.LoggerFactory = loggerFactory;
                options.DbContext = context;
                options.BitcoinExtPubKeyString = Configuration["BitcoinService:BitcoinExtPubKeyString"];
            });

            EthereumService.Init(
                ConnectionString,
                loggerFactory,
                Configuration["EthereumService:NetworkType"] != "MainNet",
                Configuration["EthereumService:EthereumPoolWalletPassword"],
                Configuration["EthereumService:EthereumGethNodeUrl"],
                Configuration["EthereumService:EthereumBrokerWallet"],
                Convert.ToDecimal(Configuration["EthereumService:EthereumPoolDrainLimit"]));

            bitcoinService.Ping();

            GraftWalletService.Init(context,
                Configuration["GraftWalletService:RpcUrl"],
                ConnectionString,
                loggerFactory);


        }
    }
}
