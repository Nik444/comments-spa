namespace Comments.Api.Application.Models;

// RootId пуст, если родитель сам корневой.
public sealed record ParentRef(long Id, long? RootId);
