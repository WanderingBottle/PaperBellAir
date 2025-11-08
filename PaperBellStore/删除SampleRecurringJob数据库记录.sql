-- =============================================
-- 删除 SampleRecurringJob 相关的数据库记录
-- =============================================
-- 执行前请确保：
-- 1. 已停止应用程序（避免任务继续执行）
-- 2. 已备份数据库（可选但推荐）
-- 3. 已删除代码中的注册逻辑
-- =============================================

-- 1. 删除定时任务记录
DELETE FROM hangfire.recurringjob 
WHERE id = 'sample-job-daily';

-- 2. 验证删除结果（应该返回空）
SELECT * FROM hangfire.recurringjob 
WHERE id = 'sample-job-daily';

-- 3. 可选：删除相关的任务执行历史记录（如果有）
-- 注意：这些记录可能很多，删除前请确认
-- DELETE FROM hangfire.job 
-- WHERE job->>'Type' LIKE '%SampleRecurringJob%';

-- 4. 可选：查看所有 Hangfire 定时任务（用于验证）
SELECT 
    id,
    cron,
    timezoneid,
    nextexecution,
    lastexecution,
    createdat
FROM hangfire.recurringjob
ORDER BY createdat DESC;

