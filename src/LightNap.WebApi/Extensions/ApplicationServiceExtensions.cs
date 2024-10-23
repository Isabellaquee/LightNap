﻿using LightNap.Core.Data;
using LightNap.Core.Extensions;
using LightNap.Core.Identity;
using LightNap.Core.Services;
using LightNap.DataProviders.Sqlite.Extensions;
using LightNap.DataProviders.SqlServer.Extensions;
using LightNap.WebApi.Configuration;
using LightNap.WebApi.Interfaces;
using LightNap.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LightNap.WebApi.Extensions
{
    /// <summary>
    /// Extension methods for configuring application services.
    /// </summary>
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            string databaseProvider = configuration.GetRequiredSetting("DatabaseProvider");
            switch (databaseProvider)
            {
                case "Sqlite":
                    services.AddLightNapSqlite(configuration);
                    break;
                case "SqlServer":
                    services.AddLightNapSqlServer(configuration);
                    break;
                default: throw new ArgumentException($"Unsupported 'DatabaseProvider' setting: '{databaseProvider}'");
            }
            return services;
        }

        public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
        {
            string emailProvider = configuration.GetRequiredSetting("EmailProvider");
            switch (emailProvider)
            {
                case "LogToConsole":
                    services.AddLogToConsoleEmailer();
                    break;
                case "Smtp":
                    services.AddSmtpEmailer();
                    break;
                default: throw new ArgumentException($"Unsupported 'EmailProvider' setting: '{emailProvider}'");
            }
            return services;
        }

        /// <summary>
        /// Adds application services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddCors();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAdministratorService, AdministratorService>();
            services.AddScoped<IDeviceService, DeviceService>();

            return services;
        }

        /// <summary>
        /// Adds identity services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentity<ApplicationUser, ApplicationRole>(
                (options) =>
                {
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration.GetRequiredSetting("Jwt:Issuer"),
                    ValidAudience = configuration.GetRequiredSetting("Jwt:Audience"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetRequiredSetting("Jwt:Key")))
                };
            });

            services.AddAuthorizationBuilder()
                .AddPolicy(Policies.RequireAdministratorRole, policy => policy.RequireRole(ApplicationRoles.Administrator.Name!));

            return services;
        }
    }
}