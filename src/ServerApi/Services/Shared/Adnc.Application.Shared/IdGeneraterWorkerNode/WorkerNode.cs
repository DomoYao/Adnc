﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Adnc.Infra.Caching;
using Adnc.Infra.Common.Extensions;
using Adnc.Infra.Common.Helper.IdGeneraterInternal;

namespace Adnc.Application.Shared.IdGeneraterWorkerNode
{
    public class WorkerNode
    {
        private readonly ILogger<WorkerNode> _logger;
        private readonly IRedisProvider _redisProvider;

        public WorkerNode(ILogger<WorkerNode> logger
           , IRedisProvider redisProvider)
        {
            _redisProvider = redisProvider;
            _logger = logger;
        }

        public async Task InitWorkerNodes(string serviceName)
        {
            var workerIdSortedSetCacheKey = string.Format(BaseEasyCachingConsts.WorkerIdSortedSetCacheKey, serviceName);

            if (!_redisProvider.KeyExists(workerIdSortedSetCacheKey))
            {
                _logger.LogInformation("Starting InitWorkerNodes:{0}", workerIdSortedSetCacheKey);

                var lockKey = $"{workerIdSortedSetCacheKey}_lock";
                var lockValue = DateTime.Now.GetTotalMilliseconds().ToString();
                var flag = await _redisProvider.StringSetAsync(lockKey, lockValue, TimeSpan.FromMilliseconds(5000), "nx");

                if (!flag)
                {
                    await Task.Delay(300);
                    await InitWorkerNodes(serviceName);
                }

                var set = new Dictionary<long, double>();
                for (long index = 0; index <= YitterSnowFlake.MaxWorkerId; index++)
                {
                    set.Add(index, DateTime.Now.GetTotalMicroseconds());
                }
                var count = await _redisProvider.ZAddAsync(workerIdSortedSetCacheKey, set);

                await _redisProvider.KeyDelAsync(lockKey);

                _logger.LogInformation("Finlished InitWorkerNodes:{0}:{1}", workerIdSortedSetCacheKey, count);
            }
            else
                _logger.LogInformation("Exists WorkerNodes:{0}", workerIdSortedSetCacheKey);
        }

        public long GetWorkerId(string serviceName)
        {
            var workerIdSortedSetCacheKey = string.Format(BaseEasyCachingConsts.WorkerIdSortedSetCacheKey, serviceName);

            var scirpt = @"local workerids = redis.call('ZRANGE', @key, @start,@stop)
                                    redis.call('ZADD',@key,@score,workerids[1])
                                    return workerids[1]";
            var parameters = new { key = workerIdSortedSetCacheKey, start = 0, stop = 0, score = DateTime.Now.GetTotalMicroseconds() };
            var luaResult = (byte[])_redisProvider.ScriptEvaluate(scirpt, parameters);
            var workerId = _redisProvider.Serializer.Deserialize<long>(luaResult);

            _logger.LogInformation("Get WorkerNodes:{0}", workerId);

            return workerId;
        }

        public async Task RefreshWorkerIdScore(string serviceName, long workerId)
        {
            if (workerId < 0 || workerId > YitterSnowFlake.MaxWorkerId)
                throw new Exception(string.Format("worker Id can't be greater than {0} or less than 0", YitterSnowFlake.MaxWorkerId));

            var workerIdSortedSetCacheKey = string.Format(BaseEasyCachingConsts.WorkerIdSortedSetCacheKey, serviceName);

            var score = DateTime.Now.GetTotalMicroseconds();
            await _redisProvider.ZAddAsync(workerIdSortedSetCacheKey, new Dictionary<long, double> { { workerId, score } });
            _logger.LogInformation("Refresh WorkerNodes:{0}:{1}", workerId, score);
        }
    }
}