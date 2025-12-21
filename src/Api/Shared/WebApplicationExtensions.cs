using Api.Data.Seeds;

namespace Api.Shared;

public static class WebApplicationExtensions
{
    public static async Task UseInfrastructureAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}
