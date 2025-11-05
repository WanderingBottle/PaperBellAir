-- ============================================
-- Serilog 日志数据库创建脚本
-- ============================================
-- 说明：本脚本用于创建专门用于日志存储的数据库和表
-- 推荐使用专门日志数据库，实现性能隔离，确保 EFCore 操作不受影响
-- ============================================

-- 第一步：创建专门的日志数据库
-- 注意：如果数据库已存在，此命令会失败，可以忽略或先删除现有数据库
CREATE DATABASE pbbstore_logs;

-- 连接到日志数据库
-- 注意：在 psql 中执行时，需要先执行 \c pbbstore_logs; 切换到日志数据库
-- 在应用程序中，连接字符串会自动连接到指定的数据库

-- ============================================
-- 第二步：创建日志表
-- ============================================
-- 注意：执行此脚本前，请确保已连接到日志数据库（\c pbbstore_logs;）

-- 创建日志表
CREATE TABLE IF NOT EXISTS "AppLogs" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Timestamp" TIMESTAMP NOT NULL,
    "Level" VARCHAR(50),
    "Message" TEXT,
    "Exception" TEXT,
    "Properties" JSONB,
    "LogEvent" JSONB,
    "MessageHash" VARCHAR(64),
    "FirstOccurrence" TIMESTAMP,
    "LastOccurrence" TIMESTAMP,
    "OccurrenceCount" INTEGER DEFAULT 1,
    "DeduplicationWindowMinutes" INTEGER DEFAULT 5
);

-- ============================================
-- 第三步：创建索引（优化查询性能）
-- ============================================

-- 时间戳索引（用于按时间查询日志）
CREATE INDEX IF NOT EXISTS "IX_AppLogs_Timestamp" ON "AppLogs" ("Timestamp");

-- 日志级别索引（用于按级别查询日志）
CREATE INDEX IF NOT EXISTS "IX_AppLogs_Level" ON "AppLogs" ("Level");

-- 消息哈希索引（用于去重查询）
CREATE INDEX IF NOT EXISTS "IX_AppLogs_MessageHash" ON "AppLogs" ("MessageHash");

-- 复合索引（用于去重查询优化）
CREATE INDEX IF NOT EXISTS "IX_AppLogs_MessageHash_LastOccurrence" ON "AppLogs" ("MessageHash", "LastOccurrence");

-- ============================================
-- 脚本执行完成
-- ============================================
-- 执行步骤：
-- 1. 连接到 PostgreSQL 数据库（使用 postgres 用户或具有创建数据库权限的用户）
-- 2. 执行第一部分创建数据库（CREATE DATABASE pbbstore_logs;）
-- 3. 切换到日志数据库（\c pbbstore_logs;）
-- 4. 执行第二部分创建表和索引
-- ============================================

