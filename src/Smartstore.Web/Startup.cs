using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Smartstore.Bootstrapping;
using Smartstore.Core.Data;
using Smartstore.Engine;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace Smartstore.Web
{
    public class Startup
    {
        private IEngineStarter _engineStarter;
        private SmartApplicationContext _appContext;

        public Startup(WebHostBuilderContext hostBuilderContext, MsLogger startupLogger)
        {
            Configuration = hostBuilderContext.Configuration;
            Environment = hostBuilderContext.HostingEnvironment;
            StartupLogger = startupLogger;
        }

        public MsLogger StartupLogger { get; }
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            _appContext = new SmartApplicationContext(Environment, Configuration, StartupLogger);

            _engineStarter = EngineFactory
                .Create(_appContext.AppConfiguration)
                .Start(_appContext);

            _engineStarter.ConfigureServices(services);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            _engineStarter.ConfigureContainer(builder);
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            // Must come very early.
            app.UseContextState();

            // Write streamlined request completion events, instead of the more verbose ones from the framework.
            // To use the default framework request logging instead, remove this line and set the "Microsoft"
            // level in appsettings.json to "Information".
            app.UseSerilogRequestLogging();

            // Executes IApplicationInitializer implementations
            // during the very first request.
            if (_appContext.IsInstalled)
            {
                app.UseApplicationInitializer();
            }
            
            appLifetime.ApplicationStarted.Register(OnStarted, app);
            _engineStarter.ConfigureApplication(app);
        }

        private void OnStarted(object app = null)
        {
            _appContext.Freeze();
            
            _engineStarter.Dispose();
            _engineStarter = null;
        }
    }
}
