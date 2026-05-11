IF DB_ID(N'EnglishTeacher') IS NULL
BEGIN
    CREATE DATABASE EnglishTeacher;
END
GO

USE EnglishTeacher;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Usuario NVARCHAR(100) NOT NULL,
        SenhaHash NVARCHAR(255) NOT NULL,
        Cargo NVARCHAR(100) NOT NULL CONSTRAINT DF_Users_Cargo DEFAULT N'User',
        Tipo INT NOT NULL CONSTRAINT DF_Users_Tipo DEFAULT 2,
        Email NVARCHAR(255) NULL,
        Nome NVARCHAR(200) NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_Users_Ativo DEFAULT 1,
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2(0) NULL
    );

    CREATE UNIQUE INDEX UX_Users_Usuario ON dbo.Users (Usuario);
END
GO

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
        Login NVARCHAR(100) NOT NULL,
        Remetente NVARCHAR(30) NOT NULL,
        Mensagem NVARCHAR(MAX) NOT NULL,
        ThreadId NVARCHAR(100) NOT NULL,
        TipoAssistente NVARCHAR(80) NOT NULL,
        DataHoraRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_ChatMessages_DataHoraRegistro DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_ChatMessages_Login_TipoAssistente_Data
        ON dbo.ChatMessages (Login, TipoAssistente, DataHoraRegistro)
        INCLUDE (Remetente, ThreadId);
END
GO

IF OBJECT_ID(N'dbo.UserVocabulary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserVocabulary
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserVocabulary PRIMARY KEY,
        Login       NVARCHAR(100)        NOT NULL,
        Word        NVARCHAR(200)        NOT NULL,
        Translation NVARCHAR(200)        NOT NULL,
        Level       NVARCHAR(80)         NOT NULL,
        RegisteredAt DATETIME2(0)        NOT NULL CONSTRAINT DF_UserVocabulary_RegisteredAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_UserVocabulary_Login_Level ON dbo.UserVocabulary (Login, Level, RegisteredAt);
END
GO

IF OBJECT_ID(N'dbo.UserErrors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserErrors
    (
        Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserErrors PRIMARY KEY,
        Login            NVARCHAR(100)        NOT NULL,
        ErrorDescription NVARCHAR(500)        NOT NULL,
        CorrectedVersion NVARCHAR(500)        NOT NULL,
        Level            NVARCHAR(80)         NOT NULL,
        RegisteredAt     DATETIME2(0)         NOT NULL CONSTRAINT DF_UserErrors_RegisteredAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_UserErrors_Login_Level ON dbo.UserErrors (Login, Level, RegisteredAt);
END
GO

IF OBJECT_ID(N'dbo.UserTopics', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserTopics
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserTopics PRIMARY KEY,
        Login        NVARCHAR(100)        NOT NULL,
        Topic        NVARCHAR(200)        NOT NULL,
        Level        NVARCHAR(80)         NOT NULL,
        RegisteredAt DATETIME2(0)         NOT NULL CONSTRAINT DF_UserTopics_RegisteredAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_UserTopics_Login_Level ON dbo.UserTopics (Login, Level, RegisteredAt);
END
GO

IF OBJECT_ID(N'dbo.UserExercises', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserExercises
    (
        Id            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserExercises PRIMARY KEY,
        Login         NVARCHAR(100)        NOT NULL,
        Question      NVARCHAR(MAX)        NOT NULL,
        CorrectAnswer NVARCHAR(500)        NOT NULL,
        ExerciseType  NVARCHAR(100)        NOT NULL,
        Level         NVARCHAR(80)         NOT NULL,
        WasAnswered   BIT                  NOT NULL CONSTRAINT DF_UserExercises_WasAnswered DEFAULT 0,
        WasCorrect    BIT                  NULL,
        CreatedAt     DATETIME2(0)         NOT NULL CONSTRAINT DF_UserExercises_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_UserExercises_Login_Level ON dbo.UserExercises (Login, Level, WasAnswered, CreatedAt);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Usuario = N'admin')
BEGIN
    INSERT INTO dbo.Users (Usuario, SenhaHash, Cargo, Tipo, Email, Nome)
    VALUES
    (
        N'admin',
        N'$2a$11$uD4LMGoxqlSF/3.6EvesreKLCs722c8PPujDd1sNXkzchRfeS8kKC',
        N'Admin',
        2,
        N'admin@local',
        N'Administrador'
    );
END
GO
