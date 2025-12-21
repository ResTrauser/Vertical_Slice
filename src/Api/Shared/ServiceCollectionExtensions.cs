using Api.Data;
using Api.Data.Seeds;
using Api.Features.Auth;
using Api.Features.Subscriptions;
using Api.Shared.Behaviors;
using Api.Shared.Email;
using Api.Shared.Exceptions;
using Api.Shared.Auth;
using Api.Shared.Mapping;
using Api.Shared.Options;
using Api.Shared.Security;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Api.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<SubscriptionOptions>(configuration.GetSection("Subscription"));
        services.Configure<InviteOptions>(configuration.GetSection("Invite"));
        services.Configure<AdminSeedOptions>(configuration.GetSection("AdminSeed"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<PasswordHasher>();
        services.AddScoped<TokenHasher>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IDevEmailSender, DevEmailSender>();
        services.AddScoped<IAuthorizationHandler, AdminOnlyHandler>();
        services.AddScoped<IAuthorizationHandler, BusinessMemberHandler>();
        services.AddScoped<IAuthorizationHandler, BusinessOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, BusinessAdminOrOwnerHandler>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        var config = TypeAdapterConfig.GlobalSettings;
        MapsterConfig.RegisterMappings(config);
        services.AddSingleton(config);
        services.AddMapster();

        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddProblemDetails();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, policy => policy.Requirements.Add(new AdminOnlyRequirement()));
            options.AddPolicy(Policies.BusinessMember, policy => policy.Requirements.Add(new BusinessMemberRequirement()));
            options.AddPolicy(Policies.BusinessOwner, policy => policy.Requirements.Add(new BusinessOwnerRequirement()));
            options.AddPolicy(Policies.BusinessAdminOrOwner, policy => policy.Requirements.Add(new BusinessAdminOrOwnerRequirement()));
        });

        services.AddEndpointsApiExplorer();
        services.AddOpenApi();

        return services;
    }
}
