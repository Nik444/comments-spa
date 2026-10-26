using Comments.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Comments.Api.Infrastructure.Persistence;

/// <summary>
/// Единственная точка доступа к базе данных. Знает, какие таблицы есть,
/// а как именно они устроены (длины, индексы, ключи), описано отдельно в *Configuration-классах.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CommentEntity> Comments => Set<CommentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Подхватывает все IEntityTypeConfiguration<T> из этой сборки автоматически:
        // новая конфигурация заработает без правок в этом файле.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
