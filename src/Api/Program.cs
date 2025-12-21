using Api.Features.Auth;
using Api.Features.Businesses;
using Api.Features.Invites;
using Api.Features.Plans;
using Api.Features.Subscriptions;
using Api.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.UseInfrastructureAsync();

app.MapAuthEndpoints();
app.MapPlanEndpoints();
app.MapSubscriptionEndpoints();
app.MapBusinessEndpoints();
app.MapInviteEndpoints();

app.Run();
