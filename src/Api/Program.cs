using Api.Features.Auth;
using Api.Features.Businesses;
using Api.Features.Invites;
using Api.Features.Plans;
using Api.Features.Subscriptions;
using Api.Shared;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.UseInfrastructureAsync();

app.MapAuthEndpoints();
app.MapPlanEndpoints();
app.MapSubscriptionEndpoints();
app.MapBusinessEndpoints();
app.MapInviteEndpoints();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }),
            duration = report.TotalDuration.TotalMilliseconds
        };
        await ctx.Response.WriteAsJsonAsync(payload);
    }
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = reg => reg.Name == "db"
});

app.Run();
