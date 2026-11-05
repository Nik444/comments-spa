using Microsoft.EntityFrameworkCore;

namespace Comments.Api.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    private const int MaxAttempts = 30;

    // Для Docker: при первом запуске база может подниматься дольше приложения, поэтому подключение повторяется.
    // Включается настройкой Database:MigrateOnStart, на обычной разработке миграции применяются командой EF.
    public static async Task MigrateDatabaseIfRequestedAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStart"))
            return;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

                app.Logger.LogInformation("Миграции базы применены.");
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts)
            {
                app.Logger.LogWarning("База недоступна (попытка {Attempt} из {Max}): {Message}", attempt, MaxAttempts, exception.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
