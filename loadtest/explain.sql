-- Диагностика медленных запросов на странице списка. Запускать в SSMS на заполненной базе (seed.sql).
-- Перед запуском включи "Включить действительный план выполнения" (Ctrl+M).
-- Нужны: вкладка "Сообщения" (время и логические чтения) и, если получится, скриншот плана.

USE CommentsDb;

SELECT SERVERPROPERTY('Edition') AS Edition, (SELECT cpu_count FROM sys.dm_os_sys_info) AS Cpus,
       (SELECT COUNT(*) FROM dbo.Comments) AS Total;

SET STATISTICS IO ON;
SET STATISTICS TIME ON;

PRINT '=== 1. Середина списка, дата по убыванию, пропустить 50000 ===';
SELECT [c].[Id], [c].[CreatedAt], [c].[Email], [c].[HomePage], [c].[ParentId], [c].[RootId], [c].[Text], [c].[UserName], [c].[AttachmentKey], [c].[AttachmentKind], [c].[AttachmentName], [c].[AttachmentSize]
FROM [Comments] AS [c]
WHERE [c].[ParentId] IS NULL
ORDER BY [c].[CreatedAt] DESC, [c].[Id] DESC
OFFSET 50000 ROWS FETCH NEXT 25 ROWS ONLY;

PRINT '=== 2. Середина списка, имя по возрастанию, пропустить 50000 ===';
SELECT [c].[Id], [c].[CreatedAt], [c].[Email], [c].[HomePage], [c].[ParentId], [c].[RootId], [c].[Text], [c].[UserName], [c].[AttachmentKey], [c].[AttachmentKind], [c].[AttachmentName], [c].[AttachmentSize]
FROM [Comments] AS [c]
WHERE [c].[ParentId] IS NULL
ORDER BY [c].[UserName], [c].[Id]
OFFSET 50000 ROWS FETCH NEXT 25 ROWS ONLY;

PRINT '=== 3. Последняя страница, дата по убыванию, пропустить 99975 ===';
SELECT [c].[Id], [c].[CreatedAt], [c].[Email], [c].[HomePage], [c].[ParentId], [c].[RootId], [c].[Text], [c].[UserName], [c].[AttachmentKey], [c].[AttachmentKind], [c].[AttachmentName], [c].[AttachmentSize]
FROM [Comments] AS [c]
WHERE [c].[ParentId] IS NULL
ORDER BY [c].[CreatedAt] DESC, [c].[Id] DESC
OFFSET 99975 ROWS FETCH NEXT 25 ROWS ONLY;

PRINT '=== 4. Последняя страница, e-mail по убыванию, пропустить 99975 ===';
SELECT [c].[Id], [c].[CreatedAt], [c].[Email], [c].[HomePage], [c].[ParentId], [c].[RootId], [c].[Text], [c].[UserName], [c].[AttachmentKey], [c].[AttachmentKind], [c].[AttachmentName], [c].[AttachmentSize]
FROM [Comments] AS [c]
WHERE [c].[ParentId] IS NULL
ORDER BY [c].[Email] DESC, [c].[Id] DESC
OFFSET 99975 ROWS FETCH NEXT 25 ROWS ONLY;

PRINT '=== 5. Для сравнения: только Id корней (без остальных колонок), как пункт 2 ===';
SELECT [c].[Id]
FROM [Comments] AS [c]
WHERE [c].[ParentId] IS NULL
ORDER BY [c].[UserName], [c].[Id]
OFFSET 50000 ROWS FETCH NEXT 25 ROWS ONLY;

PRINT '=== 6. Ответы 25 корней из середины списка ===';
SELECT [c].[Id], [c].[CreatedAt], [c].[Email], [c].[HomePage], [c].[ParentId], [c].[RootId], [c].[Text], [c].[UserName]
FROM [Comments] AS [c]
WHERE [c].[RootId] IN (
    SELECT [r].[Id] FROM [Comments] AS [r]
    WHERE [r].[ParentId] IS NULL
    ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
    OFFSET 50000 ROWS FETCH NEXT 25 ROWS ONLY)
ORDER BY [c].[RootId], [c].[CreatedAt], [c].[Id];

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
