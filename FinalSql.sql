CREATE DATABASE Konnect4;
GO

USE Konnect4;
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    FullName NVARCHAR(100),
    Bio NVARCHAR(500),
    ProfileImageUrl NVARCHAR(500) NULL,
    IsPrivate BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Followers (
    FollowerId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    FollowerUserId INT NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Accepted')),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (FollowerUserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    CONSTRAINT UQ_Follow UNIQUE(UserId, FollowerUserId)
);
GO

CREATE TABLE Posts (
    PostId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Content NVARCHAR(MAX),
    PostImageUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

CREATE TABLE Comments (
    CommentId INT IDENTITY(1,1) PRIMARY KEY,
    PostId INT NOT NULL,
    UserId INT NOT NULL,
    ParentCommentId INT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    FOREIGN KEY (ParentCommentId) REFERENCES Comments(CommentId) ON DELETE NO ACTION
);

CREATE TABLE Likes (
    LikeId INT IDENTITY(1,1) PRIMARY KEY,
    PostId INT NOT NULL,
    UserId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (PostId) REFERENCES Posts(PostId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE No Action,
    CONSTRAINT UQ_Like UNIQUE(PostId, UserId)
);
GO

CREATE TABLE Messages (
    MessageId INT IDENTITY(1,1) PRIMARY KEY,
    SenderId INT NOT NULL,
    ReceiverId INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    SentAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    ReadAt DATETIME2 NULL,
    FOREIGN KEY (SenderId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (ReceiverId) REFERENCES Users(UserId) ON DELETE NO ACTION
);
GO



-- Drop old Messages table
DROP TABLE IF EXISTS Messages;
GO

-- Create Conversations table
CREATE TABLE Conversations (
    ConversationId INT IDENTITY(1,1) PRIMARY KEY,
    LastMessageAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- Create ConversationParticipants table
CREATE TABLE ConversationParticipants (
    ParticipantId INT IDENTITY(1,1) PRIMARY KEY,
    ConversationId INT NOT NULL,
    UserId INT NOT NULL,
    JoinedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    LastReadAt DATETIME2 NULL,
    FOREIGN KEY (ConversationId) REFERENCES Conversations(ConversationId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT UQ_ConversationUser UNIQUE(ConversationId, UserId)
);
GO

-- Create Messages table (new structure)
CREATE TABLE Messages (
    MessageId INT IDENTITY(1,1) PRIMARY KEY,
    ConversationId INT NOT NULL,
    SenderId INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    MessageType NVARCHAR(20) NOT NULL DEFAULT 'Text' CHECK (MessageType IN ('Text', 'Image', 'File')),
    FileUrl NVARCHAR(500) NULL,
    SentAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsEdited BIT NOT NULL DEFAULT 0,
    IsDeleted BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (ConversationId) REFERENCES Conversations(ConversationId) ON DELETE CASCADE,
    FOREIGN KEY (SenderId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- Create MessageReadStatus table
CREATE TABLE MessageReadStatus (
    ReadStatusId INT IDENTITY(1,1) PRIMARY KEY,
    MessageId INT NOT NULL,
    UserId INT NOT NULL,
    ReadAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (MessageId) REFERENCES Messages(MessageId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION,
    CONSTRAINT UQ_MessageRead UNIQUE(MessageId, UserId)
);
GO

-- Create indexes
CREATE INDEX IX_Messages_ConversationId ON Messages(ConversationId);
CREATE INDEX IX_Messages_SenderId ON Messages(SenderId);
CREATE INDEX IX_Messages_SentAt ON Messages(SentAt DESC);
CREATE INDEX IX_ConversationParticipants_UserId ON ConversationParticipants(UserId);
CREATE INDEX IX_ConversationParticipants_ConversationId ON ConversationParticipants(ConversationId);
GO