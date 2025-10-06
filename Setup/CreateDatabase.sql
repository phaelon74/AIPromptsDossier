-- ===================================================
-- AI Dungeon Prompts Database Setup Script
-- ===================================================
-- This script creates the database, user, and all required tables
-- for the AI Dungeon Prompts application
-- ===================================================

-- Create the database (run as postgres superuser)
CREATE DATABASE aidungeonprompts
    WITH 
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TEMPLATE = template0;

-- Connect to the new database
\c aidungeonprompts

-- Create the application user
CREATE USER aidungeonprompts_user WITH PASSWORD 'CHANGE_THIS_PASSWORD';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE aidungeonprompts TO aidungeonprompts_user;
GRANT ALL ON SCHEMA public TO aidungeonprompts_user;

-- ===================================================
-- Table: Users
-- ===================================================
CREATE TABLE "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" TEXT NOT NULL,
    "Password" TEXT,
    "Role" INTEGER NOT NULL DEFAULT 0,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE
);

CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");

-- ===================================================
-- Table: Prompts
-- ===================================================
CREATE TABLE "Prompts" (
    "Id" SERIAL PRIMARY KEY,
    "Title" TEXT NOT NULL,
    "PromptContent" TEXT NOT NULL,
    "Description" TEXT,
    "Memory" TEXT,
    "AuthorsNote" TEXT,
    "Quests" TEXT,
    "Nsfw" BOOLEAN NOT NULL DEFAULT FALSE,
    "Upvote" INTEGER NOT NULL DEFAULT 0,
    "Views" INTEGER NOT NULL DEFAULT 0,
    "IsDraft" BOOLEAN NOT NULL DEFAULT FALSE,
    "PublishDate" TIMESTAMP WITH TIME ZONE,
    "OwnerId" INTEGER,
    "ParentId" INTEGER,
    "ScriptZip" BYTEA,
    "NovelAiScenario" JSONB,
    "HoloAiScenario" TEXT,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_Prompts_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_Prompts_Prompts_ParentId" FOREIGN KEY ("ParentId") REFERENCES "Prompts" ("Id")
);

CREATE INDEX "IX_Prompts_OwnerId" ON "Prompts" ("OwnerId");
CREATE INDEX "IX_Prompts_ParentId" ON "Prompts" ("ParentId");
CREATE INDEX "IX_Prompts_Title" ON "Prompts" ("Title");

-- Add constraint for ScriptZip max length
ALTER TABLE "Prompts" ADD CONSTRAINT "CK_Prompts_ScriptZip_MaxLength" 
    CHECK (octet_length("ScriptZip") <= 5000000);

-- ===================================================
-- Table: Tags
-- ===================================================
CREATE TABLE "Tags" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "SearchVector" TSVECTOR NOT NULL,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE
);

-- Create GIN index for full-text search
CREATE INDEX "IX_Tags_SearchVector" ON "Tags" USING GIN ("SearchVector");

-- Create trigger to automatically update SearchVector
CREATE OR REPLACE FUNCTION tags_search_vector_update() RETURNS trigger AS $$
BEGIN
    NEW."SearchVector" := to_tsvector('english', COALESCE(NEW."Name", ''));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tags_search_vector_trigger
    BEFORE INSERT OR UPDATE ON "Tags"
    FOR EACH ROW
    EXECUTE FUNCTION tags_search_vector_update();

-- ===================================================
-- Table: PromptTags (Junction Table)
-- ===================================================
CREATE TABLE "PromptTags" (
    "Id" SERIAL PRIMARY KEY,
    "PromptId" INTEGER NOT NULL,
    "TagId" INTEGER NOT NULL,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_PromptTags_Prompts_PromptId" FOREIGN KEY ("PromptId") REFERENCES "Prompts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PromptTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PromptTags_PromptId" ON "PromptTags" ("PromptId");
CREATE INDEX "IX_PromptTags_TagId" ON "PromptTags" ("TagId");

-- ===================================================
-- Table: WorldInfos
-- ===================================================
CREATE TABLE "WorldInfos" (
    "Id" SERIAL PRIMARY KEY,
    "Keys" TEXT NOT NULL,
    "Entry" TEXT NOT NULL,
    "PromptId" INTEGER NOT NULL,
    "PromptId1" INTEGER,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_WorldInfos_Prompts_PromptId" FOREIGN KEY ("PromptId") REFERENCES "Prompts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WorldInfos_Prompts_PromptId1" FOREIGN KEY ("PromptId1") REFERENCES "Prompts" ("Id")
);

CREATE INDEX "IX_WorldInfos_PromptId" ON "WorldInfos" ("PromptId");
CREATE INDEX "IX_WorldInfos_PromptId1" ON "WorldInfos" ("PromptId1");

-- ===================================================
-- Table: Reports
-- ===================================================
CREATE TABLE "Reports" (
    "Id" SERIAL PRIMARY KEY,
    "PromptId" INTEGER NOT NULL,
    "ReportReason" INTEGER NOT NULL,
    "ExtraDetails" TEXT,
    "Cleared" BOOLEAN NOT NULL DEFAULT FALSE,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_Reports_Prompts_PromptId" FOREIGN KEY ("PromptId") REFERENCES "Prompts" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Reports_PromptId" ON "Reports" ("PromptId");

-- ===================================================
-- Table: ApplicationLogs (Serilog)
-- ===================================================
CREATE TABLE "ApplicationLogs" (
    "Id" SERIAL PRIMARY KEY,
    "Message" TEXT NOT NULL,
    "RenderedMessage" TEXT NOT NULL,
    "Level" TEXT NOT NULL,
    "TimeStamp" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Exception" TEXT,
    "Properties" JSONB NOT NULL,
    "LogEvent" JSONB NOT NULL
);

-- ===================================================
-- Table: AuditPrompts (Audit Trail)
-- ===================================================
CREATE TABLE "AuditPrompts" (
    "Id" SERIAL PRIMARY KEY,
    "PromptId" INTEGER NOT NULL,
    "AuditScopeId" UUID NOT NULL,
    "Entry" JSONB NOT NULL,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE
);

-- ===================================================
-- Table: ServerFlags
-- ===================================================
CREATE TABLE "ServerFlags" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Enabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AdditionalMessage" TEXT,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE
);

-- ===================================================
-- Table: DataProtectionKeys (ASP.NET Core)
-- ===================================================
CREATE TABLE "DataProtectionKeys" (
    "Id" SERIAL PRIMARY KEY,
    "FriendlyName" TEXT,
    "Xml" TEXT
);

-- ===================================================
-- Table: SystemSettings
-- ===================================================
CREATE TABLE "SystemSettings" (
    "Id" SERIAL PRIMARY KEY,
    "Key" VARCHAR(255) NOT NULL UNIQUE,
    "Value" TEXT NOT NULL,
    "Description" TEXT,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE
);

-- ===================================================
-- Table: LoginAttempts
-- ===================================================
CREATE TABLE "LoginAttempts" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Success" BOOLEAN NOT NULL,
    "IpAddress" VARCHAR(45),
    "AttemptDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_LoginAttempts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_LoginAttempts_UserId" ON "LoginAttempts" ("UserId");
CREATE INDEX "IX_LoginAttempts_AttemptDate" ON "LoginAttempts" ("AttemptDate");

-- ===================================================
-- Table: AccountLockouts
-- ===================================================
CREATE TABLE "AccountLockouts" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "LockoutStart" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LockoutEnd" TIMESTAMP WITH TIME ZONE,
    "FailedAttempts" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "LockedByAdmin" TEXT,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_AccountLockouts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AccountLockouts_UserId" ON "AccountLockouts" ("UserId");
CREATE INDEX "IX_AccountLockouts_UserId_IsActive" ON "AccountLockouts" ("UserId", "IsActive");

-- ===================================================
-- View: NonDraftPrompts
-- ===================================================
CREATE OR REPLACE VIEW "NonDraftPrompts" AS
SELECT "Id"
FROM "Prompts"
WHERE "IsDraft" = FALSE;

-- ===================================================
-- Grant permissions to application user
-- ===================================================
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO aidungeonprompts_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO aidungeonprompts_user;
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO aidungeonprompts_user;

-- Ensure future objects also get permissions
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO aidungeonprompts_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO aidungeonprompts_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO aidungeonprompts_user;

-- ===================================================
-- Insert default data
-- ===================================================

-- Insert default ServerFlag for create disabled
INSERT INTO "ServerFlags" ("Name", "Enabled", "AdditionalMessage", "DateCreated")
VALUES ('CreateDisabled', FALSE, NULL, NOW());

-- Insert default SystemSettings
INSERT INTO "SystemSettings" ("Key", "Value", "Description", "DateCreated")
VALUES 
    ('UserRegistrationEnabled', 'true', 'Controls whether new user registration is allowed', NOW()),
    ('MaxPageSize', '100', 'Maximum number of results per page in search queries', NOW());

-- ===================================================
-- Database setup complete!
-- ===================================================
-- 
-- IMPORTANT: 
-- 1. Change the password 'CHANGE_THIS_PASSWORD' above to a strong password
-- 2. Use the same password in your Docker secrets configuration
-- 3. Run this script as the postgres superuser: psql -U postgres -f CreateDatabase.sql
--
-- ===================================================
