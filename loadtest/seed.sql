-- Заполняет таблицу Comments тестовыми данными для нагрузочных тестов: 1 млн строк, из них 10% корневых.
-- Корни разнесены по времени, у ответов два уровня вложенности, у двух последних корней по 500+ ответов.
-- ВНИМАНИЕ: сначала удаляет все существующие комментарии. Запускать только на тестовой базе.

USE CommentsDb;

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Roots  int = 100000;
DECLARE @Level1 int = 600000;   -- ответы на корни
DECLARE @Level2 int = 300000;   -- ответы на ответы
DECLARE @Now datetime2 = SYSUTCDATETIME();

DELETE FROM dbo.Comments;
DBCC CHECKIDENT ('dbo.Comments', RESEED, 0) WITH NO_INFOMSGS;

-- Числа 1..600000.
SELECT TOP (600000) CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS int) AS n
INTO #N
FROM sys.all_columns a CROSS JOIN sys.all_columns b;
CREATE UNIQUE CLUSTERED INDEX IX_N ON #N (n);

-- Корни: старые первыми, новые последними, шаг 5 минут.
INSERT INTO dbo.Comments (ParentId, RootId, UserName, Email, HomePage, Text, CreatedAt)
SELECT NULL, NULL,
       u.UserName,
       LOWER(u.UserName) + '@example.com',
       CASE WHEN n.n % 5 = 0 THEN 'https://example.com/' + CAST(n.n AS varchar(10)) END,
       N'Корневой комментарий ' + CAST(n.n AS nvarchar(10)) + N'. <strong>Тест</strong> нагрузки.',
       DATEADD(SECOND, -(@Roots - n.n) * 300, @Now)
FROM #N n
CROSS APPLY (SELECT 'User' + CAST(ABS(CHECKSUM(n.n)) % 5000 AS varchar(10)) AS UserName) u
WHERE n.n <= @Roots
ORDER BY n.n;

SELECT Id, CreatedAt, CAST(ROW_NUMBER() OVER (ORDER BY Id) AS int) AS Idx
INTO #RootIds
FROM dbo.Comments
WHERE ParentId IS NULL;
CREATE UNIQUE CLUSTERED INDEX IX_RootIds ON #RootIds (Idx);

-- Ответы первого уровня. Первые 1000 уходят в 2 последних корня (по 500 штук), остальные равномерно.
INSERT INTO dbo.Comments (ParentId, RootId, UserName, Email, HomePage, Text, CreatedAt)
SELECT r.Id, r.Id,
       u.UserName,
       LOWER(u.UserName) + '@example.com',
       NULL,
       N'Ответ ' + CAST(n.n AS nvarchar(10)) + N' на корневой комментарий. <i>Тест</i>.',
       CASE WHEN d.D > @Now THEN @Now ELSE d.D END
FROM #N n
JOIN #RootIds r
  ON r.Idx = CASE WHEN n.n <= 1000
                  THEN @Roots - ((n.n - 1) % 2)
                  ELSE CAST(CAST(n.n AS bigint) * 7919 % @Roots + 1 AS int) END
CROSS APPLY (SELECT 'User' + CAST(ABS(CHECKSUM(n.n, 1)) % 5000 AS varchar(10)) AS UserName) u
CROSS APPLY (SELECT DATEADD(MINUTE, 1 + n.n % 5000, r.CreatedAt) AS D) d
WHERE n.n <= @Level1
ORDER BY n.n;

SELECT Id, RootId, CreatedAt, CAST(ROW_NUMBER() OVER (ORDER BY Id) AS int) AS Idx
INTO #Level1
FROM dbo.Comments
WHERE ParentId IS NOT NULL;
CREATE UNIQUE CLUSTERED INDEX IX_Level1 ON #Level1 (Idx);

-- Ответы второго уровня: родитель случайный ответ первого уровня, корень тот же.
INSERT INTO dbo.Comments (ParentId, RootId, UserName, Email, HomePage, Text, CreatedAt)
SELECT p.Id, p.RootId,
       u.UserName,
       LOWER(u.UserName) + '@example.com',
       NULL,
       N'Ответ ' + CAST(n.n AS nvarchar(10)) + N' в ветке. <code>Тест</code>.',
       CASE WHEN d.D > @Now THEN @Now ELSE d.D END
FROM #N n
JOIN #Level1 p
  ON p.Idx = CAST(CAST(n.n AS bigint) * 7919 % @Level1 + 1 AS int)
CROSS APPLY (SELECT 'User' + CAST(ABS(CHECKSUM(n.n, 2)) % 5000 AS varchar(10)) AS UserName) u
CROSS APPLY (SELECT DATEADD(MINUTE, 1 + n.n % 5000, p.CreatedAt) AS D) d
WHERE n.n <= @Level2
ORDER BY n.n;

UPDATE STATISTICS dbo.Comments;

SELECT COUNT(*) AS Total,
       SUM(CASE WHEN ParentId IS NULL THEN 1 ELSE 0 END) AS Roots
FROM dbo.Comments;

SELECT TOP (5) RootId, COUNT(*) AS Replies
FROM dbo.Comments
WHERE RootId IS NOT NULL
GROUP BY RootId
ORDER BY COUNT(*) DESC;
