-- Схема базы данных проекта «Комментарии».
-- Рабочая база проекта это SQL Server (структуру создают миграции EF Core), этот файл
-- описывает ту же схему на диалекте MySQL, чтобы её можно было открыть в MySQL Workbench:
--   File > Open SQL Script, затем выполнить, затем Database > Reverse Engineer для диаграммы (EER).
-- Различия с SQL Server: в MySQL нет фильтрованных индексов (в SQL Server корневые индексы
-- построены с условием ParentId IS NULL), поэтому здесь индексы обычные.

CREATE DATABASE IF NOT EXISTS CommentsDb
  CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE CommentsDb;

CREATE TABLE Comments (
  Id             BIGINT        NOT NULL AUTO_INCREMENT,
  -- Комментарий, на который отвечают. NULL у корневого.
  ParentId       BIGINT        NULL,
  -- Корень ветки. NULL у корневого. Позволяет читать всю ветку одним запросом без рекурсии.
  RootId         BIGINT        NULL,
  UserName       VARCHAR(50)   NOT NULL,   -- латинские буквы и цифры
  Email          VARCHAR(254)  NOT NULL,
  HomePage       VARCHAR(2048) NULL,
  Text           VARCHAR(4000) NOT NULL,   -- только <a href title>, <code>, <i>, <strong>
  CreatedAt      DATETIME(6)   NOT NULL,   -- всегда UTC
  -- Вложение лежит в тех же колонках. Нет файла: все четыре колонки NULL.
  AttachmentKind INT           NULL,       -- 1 картинка, 2 текст (enum AttachmentKind в коде)
  AttachmentKey  VARCHAR(100)  NULL,       -- ключ в хранилище (случайный), не путь и не URL
  AttachmentName VARCHAR(255)  NULL,       -- имя файла от пользователя, только для показа
  AttachmentSize BIGINT        NULL,       -- байт

  PRIMARY KEY (Id),

  -- Корень: оба поля пустые. Ответ: оба заполнены.
  CONSTRAINT CK_Comments_ParentId_RootId CHECK (
    (ParentId IS NULL AND RootId IS NULL) OR (ParentId IS NOT NULL AND RootId IS NOT NULL)),

  -- Каскада нет: комментарии не удаляются.
  CONSTRAINT FK_Comments_Parent FOREIGN KEY (ParentId) REFERENCES Comments (Id) ON DELETE NO ACTION,
  CONSTRAINT FK_Comments_Root   FOREIGN KEY (RootId)   REFERENCES Comments (Id) ON DELETE NO ACTION,

  -- Сортировка корней по дате (по умолчанию новые сверху), по имени и по e-mail. Id вторым ключом
  -- делает порядок однозначным.
  INDEX IX_Comments_Roots_CreatedAt (CreatedAt DESC, Id DESC),
  INDEX IX_Comments_Roots_UserName (UserName, Id),
  INDEX IX_Comments_Roots_Email (Email, Id),

  -- Все ответы набора корней одним запросом: WHERE RootId IN (...) ORDER BY RootId, CreatedAt.
  INDEX IX_Comments_RootId_CreatedAt (RootId, CreatedAt)
) ENGINE = InnoDB;
