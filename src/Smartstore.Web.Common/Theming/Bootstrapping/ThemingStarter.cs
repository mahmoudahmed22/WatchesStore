using Autofac;
using Smartstore.Core.Bootstrapping;
using Smartstore.Core.Theming;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Razor;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Bootstrapping
{
    internal sealed class ThemingStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterModule(new ThemesModule());

            if (appContext.IsInstalled)
            {
                builder.RegisterType<ThemeVariableRepository>().AsSelf().InstancePerLifetimeScope();
                builder.RegisterType<DefaultThemeVariableService>().As<IThemeVariableService>().InstancePerLifetimeScope();
                builder.RegisterType<DefaultThemeContext>().As<IThemeContext>().InstancePerLifetimeScope();
                builder.RegisterType<RazorViewInvoker>().As<IRazorViewInvoker>().InstancePerLifetimeScope();
            }
        }
    }
}