using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Bootstrapping
{
    public static class DbServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a scoped <see cref="DbQuerySettings" /> factory.
        /// </summary>
        public static IServiceCollection AddDbQuerySettings(this IServiceCollection services)
        {
            services.TryAddScoped<DbQuerySettings>(c => 
            {
                var storeContext = c.GetService<IStoreContext>();
                var aclService = c.GetService<IAclService>();

                return new DbQuerySettings(
                    aclService != null && !aclService.HasActiveAcl(),
                    storeContext?.IsSingleStoreMode() ?? false);
            });

            return services;
        }

        /// <summary>
        /// Registers the open generic <see cref="DbMigrator{TContext}" /> as transient dependency.
        /// </summary>
        public static IServiceCollection AddDbMigrator(this IServiceCollection services)
        {
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
            services.AddTransient(typeof(DbMigrator<>));
            return services;
        }

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Support for multi-provider pooled factory")]
        public static IServiceCollection AddPooledDbContextFactory<TContext>(
            this IServiceCollection services,
            Type contextImplType,
            int poolSize = 128,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsBuilder = null)
            where TContext : HookingDbContext
        {
            // INFO: TContextImpl cannot be a type parameter because type is defined in an assembly that is not referenced.
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(contextImplType, nameof(contextImplType));

            var addPoolingOptionsMethod = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethod("AddPoolingOptions", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(contextImplType);

            // --> Call AddPoolingOptions<TContextImplementation>(services, optionsAction, poolSize)
            addPoolingOptionsMethod.Invoke(null, new object[] { services, optionsBuilder, poolSize });

            // --> Call services.TryAddSingleton<IDbContextPool<TContextImpl>, DbContextPool<TContextImpl>>()
            var contextPoolServiceType = typeof(IDbContextPool<>).MakeGenericType(contextImplType);
            var contextPoolImplType = typeof(DbContextPool<>).MakeGenericType(contextImplType);
            services.TryAddSingleton(contextPoolServiceType, contextPoolImplType);

            // --> Register provider-aware IDbContextFactory<TContext>
            services.TryAddSingleton(c =>
            {
                var pool = c.GetRequiredService(contextPoolServiceType);
                var pooledFactoryType = typeof(PooledApplicationDbContextFactory<,>).MakeGenericType(typeof(TContext), contextImplType);

                var instance = Activator.CreateInstance(pooledFactoryType, new object[] { pool });
                return (IDbContextFactory<TContext>)instance;
            });

            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            DbMigrationManager.Instance.RegisterDbContext(typeof(TContext));

            return services;
        }
    }
}