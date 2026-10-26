namespace Comments.Api.Domain;

/// <summary>Вид вложения. Значения хранятся в БД числом, менять их нельзя.</summary>
public enum AttachmentKind
{
    Image = 1,
    Text = 2
}
