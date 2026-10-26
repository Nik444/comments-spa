using Comments.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Api.Infrastructure.Persistence.Configurations;

/// <summary>Как комментарий хранится в базе: размеры полей, связи, ограничения и индексы.</summary>
public class CommentConfiguration : IEntityTypeConfiguration<CommentEntity>
{
    public void Configure(EntityTypeBuilder<CommentEntity> builder)
    {
        // Корень: оба поля пустые. Ответ: оба заполнены. Другие сочетания база не пропустит.
        builder.ToTable("Comments", table => table.HasCheckConstraint(
            "CK_Comments_ParentId_RootId",
            "([ParentId] IS NULL AND [RootId] IS NULL) OR ([ParentId] IS NOT NULL AND [RootId] IS NOT NULL)"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserName).HasMaxLength(CommentLimits.UserNameMax);
        builder.Property(c => c.Email).HasMaxLength(CommentLimits.EmailMax);
        builder.Property(c => c.HomePage).HasMaxLength(CommentLimits.HomePageMax);
        builder.Property(c => c.Text).HasMaxLength(CommentLimits.TextMax);

        // Внешние ключи на ту же таблицу. Каскада нет: комментарии не удаляются.
        builder.HasOne<CommentEntity>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<CommentEntity>()
            .WithMany()
            .HasForeignKey(c => c.RootId)
            .OnDelete(DeleteBehavior.NoAction);

        // Вложение лежит в тех же колонках таблицы. Нет файла: все четыре колонки NULL.
        builder.OwnsOne(c => c.Attachment, attachment =>
        {
            attachment.Property(a => a.Kind).HasColumnName("AttachmentKind");
            attachment.Property(a => a.Key).HasColumnName("AttachmentKey").HasMaxLength(CommentLimits.AttachmentKeyMax);
            attachment.Property(a => a.OriginalName).HasColumnName("AttachmentName").HasMaxLength(CommentLimits.AttachmentNameMax);
            attachment.Property(a => a.SizeBytes).HasColumnName("AttachmentSize");
        });

        // Индексы под сортировку списка корней. Фильтр оставляет в индексе только корни,
        // поэтому ответы его не раздувают. Id вторым ключом делает порядок однозначным.
        // Один индекс обслуживает обе стороны сортировки (прямой и обратный просмотр).
        builder.HasIndex(c => new { c.CreatedAt, c.Id })
            .IsDescending(true, true)
            .HasFilter("[ParentId] IS NULL")
            .HasDatabaseName("IX_Comments_Roots_CreatedAt");

        builder.HasIndex(c => new { c.UserName, c.Id })
            .HasFilter("[ParentId] IS NULL")
            .HasDatabaseName("IX_Comments_Roots_UserName");

        builder.HasIndex(c => new { c.Email, c.Id })
            .HasFilter("[ParentId] IS NULL")
            .HasDatabaseName("IX_Comments_Roots_Email");

        // Все ответы набора корней одним запросом: WHERE RootId IN (...) ORDER BY RootId, CreatedAt.
        builder.HasIndex(c => new { c.RootId, c.CreatedAt })
            .HasDatabaseName("IX_Comments_RootId_CreatedAt");
    }
}
