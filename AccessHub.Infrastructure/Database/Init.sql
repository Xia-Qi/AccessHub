-- 1. User表

CREATE TABLE [dbo].[Users] (
    -- Primary Key
    [Id]                UNIQUEIDENTIFIER    NOT NULL,
    
    -- User Properties
    [Name]              NVARCHAR(50)        NOT NULL,
    [Email]             NVARCHAR(100)       NOT NULL,
    [PasswordHash]      NVARCHAR(200)       NOT NULL,
    [IsActive]          BIT                 NOT NULL,
    
    -- Soft Delete
    [IsDeleted]         BIT                 NOT NULL,
    
    -- Audit Properties
    [CreatedBy]         NVARCHAR(50)        NOT NULL,
    [CreatedAt]         DATETIME            NOT NULL, -- 使用DDD，在代码层面控制默认值
    [LastModifiedBy]    NVARCHAR(50)        NULL,
    [LastModifiedAt]    DATETIME            NULL,
    
    -- Concurrency Version
    [Version]           ROWVERSION          NOT NULL,
    
    CONSTRAINT [PK_Users_Id] 
        PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- 唯一聚集索引
-- CREATE UNIQUE CLUSTERED INDEX [PK_Users_Id] ON [dbo].[Users] ([Id] ASC)

-- Filtered Unique Indexes instead of Unique Constraints（有过滤条件的唯一索引代替唯一约束）
CREATE UNIQUE NONCLUSTERED INDEX [UK_Users_Email] 
ON [dbo].[Users] ([Email])
WHERE [IsDeleted] = 0;

CREATE UNIQUE NONCLUSTERED INDEX [UK_Users_Name] 
ON [dbo].[Users] ([Name])
WHERE [IsDeleted] = 0;

-- Indexes 非聚集索引包含列，适用于“覆盖查询”
CREATE NONCLUSTERED INDEX [IX_Users_Email] 
ON [dbo].[Users] ([Email]) 
INCLUDE ([Name], [IsActive], [IsDeleted]);

CREATE NONCLUSTERED INDEX [IX_Users_Name] 
ON [dbo].[Users] ([Name]) 
INCLUDE ([Email], [IsActive], [IsDeleted]);


-- 2. Role表

CREATE TABLE [dbo].[Roles] (
    -- Primary Key
    [Id]                UNIQUEIDENTIFIER    NOT NULL,
    
    -- Role Properties
    [Name]              NVARCHAR(50)        NOT NULL,
    
    -- Audit Properties
    [CreatedBy]         NVARCHAR(50)        NOT NULL,
    [CreatedAt]         DATETIME            NOT NULL,
    [LastModifiedBy]    NVARCHAR(50)        NULL,
    [LastModifiedAt]    DATETIME            NULL,
    
    -- Concurrency Version
    [Version]           ROWVERSION          NOT NULL,
    
    CONSTRAINT [PK_Roles_Id] 
        PRIMARY KEY CLUSTERED ([Id] ASC),
);

-- 3. UserRole 表

CREATE TABLE [dbo].[UserRole] (
    -- Composite Foreign Keys
    [UserId]       UNIQUEIDENTIFIER        NOT NULL,
    [RoleId]       UNIQUEIDENTIFIER        NOT NULL,
    
    -- Audit Properties
    [CreatedBy]     NVARCHAR(50)           NOT NULL,
    [CreatedAt]     DATETIME               NOT NULL,
    [LastModifiedBy] NVARCHAR(50)          NULL,
    [LastModifiedAt] DATETIME              NULL,
    
    CONSTRAINT [PK_UserRole_UserId_RoleId] 
        PRIMARY KEY CLUSTERED ([UserId], [RoleId]),
    
    -- 外键约束
    CONSTRAINT [FK_UserRole_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[Users] ([Id])
        ON DELETE CASCADE, -- 允许级联删除
        
    CONSTRAINT [FK_UserRole_RoleId] 
        FOREIGN KEY ([RoleId]) 
        REFERENCES [dbo].[Roles] ([Id])
        ON DELETE CASCADE
);

-- Indexes
CREATE NONCLUSTERED INDEX [IX_UserRole_UsersId] 
ON [dbo].[UserRole] ([UserId]);

CREATE NONCLUSTERED INDEX [IX_UserRole_RolesId] 
ON [dbo].[UserRole] ([RoleId]);