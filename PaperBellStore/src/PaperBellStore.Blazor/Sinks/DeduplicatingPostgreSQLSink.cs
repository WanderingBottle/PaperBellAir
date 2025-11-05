using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.Blazor.Sinks
{
    /// <summary>
    /// 支持去重的 PostgreSQL Sink
    /// 在时间窗口内相同的日志会被合并，记录首次和最后出现时间以及出现次数
    /// </summary>
    public class DeduplicatingPostgreSQLSink : PeriodicBatchingSink, ITransientDependency
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly bool _enableDeduplication;
        private readonly int _deduplicationWindowMinutes;
        private readonly LogEventLevel _minimumLevel;
        private readonly List<string> _excludedKeywords;
        private readonly List<Regex> _excludedPatterns;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // 性能优化：内存缓存，存储最近写入的日志哈希和对应的记录ID
        // Key: MessageHash, Value: (LogId, LastOccurrence)
        private readonly ConcurrentDictionary<string, (Guid LogId, DateTime LastOccurrence)> _hashCache;
        private readonly Timer _cacheCleanupTimer;
        private readonly int _cacheExpirationMinutes;

        public DeduplicatingPostgreSQLSink(
            string connectionString,
            string tableName = "AppLogs",
            bool enableDeduplication = false,
            int deduplicationWindowMinutes = 5,
            LogEventLevel minimumLevel = LogEventLevel.Verbose,
            IEnumerable<string>? excludedKeywords = null,
            IEnumerable<string>? excludedPatterns = null,
            int batchPostingLimit = 100,
            TimeSpan? period = null,
            int cacheExpirationMinutes = 10)
            : base(batchPostingLimit, period ?? TimeSpan.FromSeconds(5))
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _enableDeduplication = enableDeduplication;
            _deduplicationWindowMinutes = deduplicationWindowMinutes;
            _minimumLevel = minimumLevel;
            _excludedKeywords = excludedKeywords?.ToList() ?? new List<string>();
            _excludedPatterns = excludedPatterns?
                .Select(p => new Regex(p, RegexOptions.IgnoreCase))
                .ToList() ?? new List<Regex>();

            // 性能优化：初始化内存缓存
            _hashCache = new ConcurrentDictionary<string, (Guid, DateTime)>();
            _cacheExpirationMinutes = cacheExpirationMinutes;

            // 定期清理过期的缓存项（每5分钟清理一次）
            _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            await _semaphore.WaitAsync();
            try
            {
                // 性能优化：先过滤和预处理日志
                var validEvents = events
                    .Where(e => e.Level >= _minimumLevel && !ShouldExcludeLog(e))
                    .ToList();

                if (!validEvents.Any())
                {
                    return;
                }

                // 性能优化：使用连接池（Npgsql 默认使用连接池）
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 性能优化：批量去重处理
                if (_enableDeduplication)
                {
                    await ProcessBatchWithDeduplicationAsync(connection, validEvents);
                }
                else
                {
                    await ProcessBatchWithoutDeduplicationAsync(connection, validEvents);
                }
            }
            catch (Exception ex)
            {
                // 写入失败时记录到控制台（避免循环日志）
                System.Diagnostics.Debug.WriteLine($"Failed to write log to database: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 性能优化：批量去重处理（在内存中先去重，再批量写入数据库）
        /// </summary>
        private async Task ProcessBatchWithDeduplicationAsync(NpgsqlConnection connection, List<LogEvent> events)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-_deduplicationWindowMinutes);
            var cacheExpirationTime = now.AddMinutes(-_cacheExpirationMinutes);

            // 性能优化：批量计算哈希值
            var hashToEvents = new Dictionary<string, List<(LogEvent Event, DateTime Timestamp)>>();
            var hashToUpdate = new Dictionary<string, (Guid LogId, DateTime LastOccurrence, int Count)>();
            var newLogs = new List<(LogEvent Event, string MessageHash, DateTime Timestamp)>();

            foreach (var logEvent in events)
            {
                var messageHash = CalculateMessageHash(logEvent);

                // 性能优化：先检查内存缓存
                if (_hashCache.TryGetValue(messageHash, out var cached))
                {
                    // 检查缓存是否在时间窗口内
                    if (cached.LastOccurrence >= windowStart)
                    {
                        // 在内存中合并，减少数据库操作
                        if (hashToUpdate.TryGetValue(messageHash, out var existing))
                        {
                            hashToUpdate[messageHash] = (existing.LogId, now, existing.Count + 1);
                        }
                        else
                        {
                            hashToUpdate[messageHash] = (cached.LogId, now, 1);
                        }

                        // 更新缓存
                        _hashCache[messageHash] = (cached.LogId, now);
                        continue;
                    }
                }

                // 缓存未命中或过期，需要查询数据库
                if (!hashToEvents.ContainsKey(messageHash))
                {
                    hashToEvents[messageHash] = new List<(LogEvent, DateTime)>();
                }
                hashToEvents[messageHash].Add((logEvent, now));
            }

            // 性能优化：批量查询数据库中未缓存的哈希
            if (hashToEvents.Any())
            {
                var hashesToQuery = hashToEvents.Keys.ToList();
                var dbResults = await FindDuplicateLogsBatchAsync(connection, hashesToQuery, windowStart);

                foreach (var hash in hashesToQuery)
                {
                    if (dbResults.TryGetValue(hash, out var dbResult))
                    {
                        // 数据库中找到重复记录，更新
                        var (logEvent, timestamp) = hashToEvents[hash].First();
                        hashToUpdate[hash] = (dbResult.LogId, timestamp, hashToEvents[hash].Count);

                        // 更新缓存
                        _hashCache[hash] = (dbResult.LogId, timestamp);
                    }
                    else
                    {
                        // 数据库中没有找到，作为新记录插入
                        foreach (var (logEvent, timestamp) in hashToEvents[hash])
                        {
                            var messageHash = CalculateMessageHash(logEvent);
                            newLogs.Add((logEvent, messageHash, timestamp));
                        }
                    }
                }
            }

            // 性能优化：批量更新数据库
            if (hashToUpdate.Any())
            {
                await UpdateLogsBatchAsync(connection, hashToUpdate);
            }

            // 性能优化：批量插入新记录
            if (newLogs.Any())
            {
                await InsertLogsBatchAsync(connection, newLogs, now);
            }
        }

        /// <summary>
        /// 性能优化：批量处理（不去重）
        /// </summary>
        private async Task ProcessBatchWithoutDeduplicationAsync(NpgsqlConnection connection, List<LogEvent> events)
        {
            var newLogs = events.Select(e => (e, string.Empty, DateTime.UtcNow)).ToList();
            await InsertLogsBatchAsync(connection, newLogs, DateTime.UtcNow);
        }

        /// <summary>
        /// 性能优化：批量查询重复日志
        /// </summary>
        private async Task<Dictionary<string, (Guid LogId, DateTime LastOccurrence)>> FindDuplicateLogsBatchAsync(
            NpgsqlConnection connection,
            List<string> messageHashes,
            DateTime windowStart)
        {
            var results = new Dictionary<string, (Guid, DateTime)>();

            if (!messageHashes.Any())
            {
                return results;
            }

            // 性能优化：使用 IN 子句批量查询
            var sql = $@"
                SELECT DISTINCT ON (""MessageHash"")
                       ""MessageHash"", ""Id"", ""LastOccurrence""
                FROM ""{_tableName}""
                WHERE ""MessageHash"" = ANY(@messageHashes)
                  AND ""LastOccurrence"" >= @windowStart
                ORDER BY ""MessageHash"", ""LastOccurrence"" DESC";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("messageHashes", messageHashes.ToArray());
            command.Parameters.AddWithValue("windowStart", windowStart);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var hash = reader.GetString(0);
                var logId = reader.GetGuid(1);
                var lastOccurrence = reader.GetDateTime(2);
                results[hash] = (logId, lastOccurrence);
            }

            return results;
        }

        /// <summary>
        /// 性能优化：批量更新日志记录（使用事务批量更新）
        /// </summary>
        private async Task UpdateLogsBatchAsync(
            NpgsqlConnection connection,
            Dictionary<string, (Guid LogId, DateTime LastOccurrence, int Count)> updates)
        {
            if (!updates.Any())
            {
                return;
            }

            // 性能优化：使用事务批量更新，提升性能
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var kvp in updates)
                {
                    var sql = $@"
                        UPDATE ""{_tableName}""
                        SET ""LastOccurrence"" = @lastOccurrence,
                            ""OccurrenceCount"" = ""OccurrenceCount"" + @count,
                            ""Timestamp"" = @lastOccurrence
                        WHERE ""Id"" = @id";

                    await using var command = new NpgsqlCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("id", kvp.Value.LogId);
                    command.Parameters.AddWithValue("lastOccurrence", kvp.Value.LastOccurrence);
                    command.Parameters.AddWithValue("count", kvp.Value.Count);
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 性能优化：批量插入日志记录（使用参数化批量插入）
        /// </summary>
        private async Task InsertLogsBatchAsync(
            NpgsqlConnection connection,
            List<(LogEvent Event, string? MessageHash, DateTime Timestamp)> logs,
            DateTime now)
        {
            if (!logs.Any())
            {
                return;
            }

            // 性能优化：使用事务批量插入，提升性能
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                if (_enableDeduplication)
                {
                    // 启用去重：批量插入所有字段包括去重相关字段
                    foreach (var (logEvent, messageHash, timestamp) in logs)
                    {
                        var logId = Guid.NewGuid();
                        var level = logEvent.Level.ToString();
                        var message = logEvent.RenderMessage();
                        var exception = logEvent.Exception?.ToString();
                        var properties = SerializeProperties(logEvent);
                        var logEventJson = SerializeLogEvent(logEvent);

                        var sql = $@"
                            INSERT INTO ""{_tableName}""
                            (""Id"", ""Timestamp"", ""Level"", ""Message"", ""Exception"", ""Properties"", ""LogEvent"",
                             ""MessageHash"", ""FirstOccurrence"", ""LastOccurrence"", ""OccurrenceCount"", ""DeduplicationWindowMinutes"")
                            VALUES
                            (@id, @timestamp, @level, @message, @exception, @properties, @logEvent,
                             @messageHash, @firstOccurrence, @lastOccurrence, @occurrenceCount, @windowMinutes)";

                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        command.Parameters.AddWithValue("id", logId);
                        command.Parameters.AddWithValue("timestamp", timestamp);
                        command.Parameters.AddWithValue("level", level);
                        command.Parameters.AddWithValue("message", (object)message ?? DBNull.Value);
                        command.Parameters.AddWithValue("exception", (object)exception ?? DBNull.Value);
                        command.Parameters.AddWithValue("properties", (object)properties ?? DBNull.Value);
                        command.Parameters.AddWithValue("logEvent", (object)logEventJson ?? DBNull.Value);
                        command.Parameters.AddWithValue("messageHash", messageHash ?? string.Empty);
                        command.Parameters.AddWithValue("firstOccurrence", timestamp);
                        command.Parameters.AddWithValue("lastOccurrence", timestamp);
                        command.Parameters.AddWithValue("occurrenceCount", 1);
                        command.Parameters.AddWithValue("windowMinutes", _deduplicationWindowMinutes);

                        await command.ExecuteNonQueryAsync();

                        // 更新缓存
                        _hashCache[messageHash] = (logId, timestamp);
                    }
                }
                else
                {
                    // 禁用去重：批量插入基本字段
                    foreach (var (logEvent, _, timestamp) in logs)
                    {
                        var logId = Guid.NewGuid();
                        var level = logEvent.Level.ToString();
                        var message = logEvent.RenderMessage();
                        var exception = logEvent.Exception?.ToString();
                        var properties = SerializeProperties(logEvent);
                        var logEventJson = SerializeLogEvent(logEvent);

                        var sql = $@"
                            INSERT INTO ""{_tableName}""
                            (""Id"", ""Timestamp"", ""Level"", ""Message"", ""Exception"", ""Properties"", ""LogEvent"")
                            VALUES
                            (@id, @timestamp, @level, @message, @exception, @properties, @logEvent)";

                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        command.Parameters.AddWithValue("id", logId);
                        command.Parameters.AddWithValue("timestamp", timestamp);
                        command.Parameters.AddWithValue("level", level);
                        command.Parameters.AddWithValue("message", (object)message ?? DBNull.Value);
                        command.Parameters.AddWithValue("exception", (object)exception ?? DBNull.Value);
                        command.Parameters.AddWithValue("properties", (object)properties ?? DBNull.Value);
                        command.Parameters.AddWithValue("logEvent", (object)logEventJson ?? DBNull.Value);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 性能优化：清理过期的缓存项
        /// </summary>
        private void CleanupExpiredCache(object? state)
        {
            try
            {
                var expirationTime = DateTime.UtcNow.AddMinutes(-_cacheExpirationMinutes);
                var expiredKeys = _hashCache
                    .Where(kvp => kvp.Value.LastOccurrence < expirationTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _hashCache.TryRemove(key, out _);
                }
            }
            catch
            {
                // 忽略清理错误，避免影响主流程
            }
        }

        /// <summary>
        /// 检查日志是否应该被屏蔽
        /// </summary>
        private bool ShouldExcludeLog(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            var exception = logEvent.Exception?.ToString() ?? "";

            // 检查关键词屏蔽
            if (_excludedKeywords.Any(keyword =>
                message.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                exception.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // 检查正则表达式屏蔽
            if (_excludedPatterns.Any(pattern =>
                pattern.IsMatch(message) || pattern.IsMatch(exception)))
            {
                return true;
            }

            return false;
        }

        private string CalculateMessageHash(LogEvent logEvent)
        {
            // 基于 Level + Message + Exception 计算哈希
            var content = $"{logEvent.Level}|{logEvent.RenderMessage()}|{logEvent.Exception?.ToString() ?? ""}";
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private string? SerializeProperties(LogEvent logEvent)
        {
            try
            {
                var properties = new Dictionary<string, object>();
                foreach (var property in logEvent.Properties)
                {
                    properties[property.Key] = property.Value.ToString();
                }
                return System.Text.Json.JsonSerializer.Serialize(properties);
            }
            catch
            {
                return null;
            }
        }

        private string? SerializeLogEvent(LogEvent logEvent)
        {
            try
            {
                var logEventData = new
                {
                    MessageTemplate = logEvent.MessageTemplate.ToString(),
                    Level = logEvent.Level.ToString(),
                    Timestamp = logEvent.Timestamp
                };
                return System.Text.Json.JsonSerializer.Serialize(logEventData);
            }
            catch
            {
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _cacheCleanupTimer?.Dispose();
                _semaphore?.Dispose();
                _hashCache?.Clear();
            }
        }
    }
}
