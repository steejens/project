using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using AutoWrapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Project.Core.Extensions;
using Project.Core.Models;
using Project.Data;
using Project.DataAccess.Repository;
using Project.Domain.Entities.Identity;
using Project.Infrastructure.Services;

namespace Project.Application
{
    public class DependencyInjectionOptions
    {
        public Assembly[]? AutoMapperAssemblies { get; set; }
        public Assembly SwaggerAssembly { get; set; }
    }

    public static class DependencyInjection
    {
        private static readonly bool IsDevelopment = EnvironmentExtension.IsDevelopment;
        private static readonly bool IsStaging = EnvironmentExtension.IsStaging;
        private static readonly bool IsProduction = EnvironmentExtension.IsPreProduction;
        private static readonly bool IsPreProduction = EnvironmentExtension.IsPreProduction;
        public static IServiceCollection ConfigureDependencyInjections(this IServiceCollection services,
            IConfiguration configuration, DependencyInjectionOptions options)
        {
            //var contentRoot = Environment.Con;

            services.AddMemoryCache();
            services
                .ConfigureIdentity(configuration)
                .ConfigureAuthentication(configuration)
                //.ConfigureAuthorization()
                .ConfigureDatabase(configuration)
                .AddHttpClient()
                .AddRepositories()
                .ConfigureAutoMapper(options.AutoMapperAssemblies)
                .ConfigureServices()
                .ConfigureCors()
                .ConfigureRequestBodyLimit()
                .AddSwagger(options.SwaggerAssembly)
                .AddMediator();
            return services;
        }

        public static IServiceCollection ConfigureRequestBodyLimit(this IServiceCollection services)
        {
            const int maxBodyLimit = 5000000;//5mb
            const int maxFileSizeLimit = 40000000; //40mb
            const int maxLimit = maxBodyLimit + maxFileSizeLimit;
            const long multipartBodyLengthLimit = maxLimit;
            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = multipartBodyLengthLimit;

            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = multipartBodyLengthLimit; // if don't set default value is: 30 MB  41MB
            });

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = maxLimit;
                x.MultipartBodyLengthLimit = multipartBodyLengthLimit; // if don't set default value is: 128 MB
                x.MultipartHeadersLengthLimit = maxLimit;
            });
            return services;
        }

        public static IApplicationBuilder ConfigureAutoWrapperMiddleware(this IApplicationBuilder builder)
        {
            builder.UseApiResponseAndExceptionWrapper(new AutoWrapperOptions
            {
                //UseApiProblemDetailsException = true,
                IgnoreNullValue = false,
                ShowStatusCode = true,
                ShowIsErrorFlagForSuccessfulResponse = true,
                IsDebug = IsDevelopment || IsStaging,
                EnableExceptionLogging = false,
                EnableResponseLogging = false,
                LogRequestDataOnException = false,
                ShouldLogRequestData = false,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                UseCustomExceptionFormat = false,
            });

            return builder;
        }


        public static IServiceCollection AddMediator(this IServiceCollection services)
        {

            services
                .AddMediatR(Assembly.GetExecutingAssembly());



            return services;
        }

        public static IServiceCollection AddSwagger(this IServiceCollection services, Assembly assembly)
        {
            services.AddSwaggerGen(swagger =>
            {
                //This is to generate the Default UI of Swagger Documentation  
                swagger.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "AIH.ERP.Analytic.API",
                    Version = "v1",
                    Description = "Vis Api"
                });
                // To Enable authorization using Swagger (JWT)  
                swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                });
                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}

                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                swagger.IncludeXmlComments(xmlPath);

            });
            services.AddSwaggerGenNewtonsoftSupport();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // i assume your service interfaces inherit from IRepositoryBase<>
            Assembly ass = typeof(IRepositoryIdentifier).GetTypeInfo().Assembly;

            // get all concrete types which implements IRepositoryIdentifier
            var allRepositories = ass.GetTypes().Where(t =>
                t.GetTypeInfo().IsClass &&
                !t.IsGenericType &&
                !t.GetTypeInfo().IsAbstract &&
                typeof(IRepositoryIdentifier).IsAssignableFrom(t));

            foreach (var type in allRepositories)
            {
                var allInterfaces = type.GetInterfaces();
                var mainInterfaces = allInterfaces.Where(t => typeof(IRepositoryIdentifier) != t && (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(IRepository<>)));
                foreach (var itype in mainInterfaces)
                {
                    if (allRepositories.Any(x => x != type && itype.IsAssignableFrom(x)))
                    {
                        throw new Exception("The " + itype.Name + " type has more than one implementations, please change your filter");
                    }
                    services.AddScoped(itype, type);
                }
            }
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
        private static IServiceCollection ConfigureAutoMapper(this IServiceCollection services, Assembly[] assemblies)
        {
            services.AddAutoMapper(assemblies);
            return services;
        }
        private static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            //services.AddScoped<AuthService>();
            services.AddScoped<TokenService>();

            return services;
        }

        private static IServiceCollection ConfigureDatabase(this IServiceCollection services,
            IConfiguration configuration)
        {

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseNpgsql(configuration.GetConnectionString("DefaultPostgres")));
            return services;
        }

        public static IServiceCollection ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:4200");
                    });
            });
            return services;
        }
        private static IServiceCollection ConfigureIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            var appSettingsSection = configuration.GetSection("authSettings");
            services.Configure<AuthSettings>(appSettingsSection);

            var authSettings = appSettingsSection.Get<AuthSettings>();

            services.AddIdentity<User, Role>(
                options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireDigit = false;
                    options.Password.RequiredUniqueChars = 0;
                    options.Lockout.AllowedForNewUsers = true;
                    //options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(authSettings.LockoutExpiry);
                   // options.Lockout.MaxFailedAccessAttempts = authSettings.MaxFailedAccessAttempts;
                }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            return services;
        }
        private static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var appSettingsSection = configuration.GetSection("TokenSettings");
            services.Configure<TokenSettings>(appSettingsSection);
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var appSettings = appSettingsSection.Get<TokenSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.JwtKey);
            var issuer = appSettings.JwtIssuer;

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(x =>
            {

                x.IncludeErrorDetails = true;
                x.RequireHttpsMetadata = false;
                x.SaveToken = false;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = issuer,
                    ValidAudience = issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}
