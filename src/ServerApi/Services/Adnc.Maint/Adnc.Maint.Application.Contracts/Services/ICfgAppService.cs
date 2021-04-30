﻿using System.Threading.Tasks;
using Adnc.Maint.Application.Contracts.Dtos;
using Adnc.Application.Shared.Interceptors;
using Adnc.Application.Shared.Services;
using Adnc.Application.Shared.Dtos;
using Adnc.Maint.Application.Contracts.Consts;
using Adnc.Infra.Caching.Interceptor;

namespace Adnc.Maint.Application.Contracts.Services
{
    public interface ICfgAppService : IAppService
    {
        Task<AppSrvResult<PageModelDto<CfgDto>>> GetPagedAsync(CfgSearchPagedDto search);

        [OpsLog(LogName = "新增参数")]
        [CachingEvict(CacheKey = EasyCachingConsts.CfgListCacheKey)]
        Task<AppSrvResult<long>> CreateAsync(CfgCreationDto input);

        [OpsLog(LogName = "修改参数")]
        [CachingEvict(CacheKey = EasyCachingConsts.CfgListCacheKey)]
        Task<AppSrvResult> UpdateAsync(long id, CfgUpdationDto input);

        [OpsLog(LogName = "删除参数")]
        [CachingEvict(CacheKey = EasyCachingConsts.CfgListCacheKey)]
        Task<AppSrvResult> DeleteAsync(long id);

        Task<AppSrvResult<CfgDto>> GetAsync(long id);
    }
}
