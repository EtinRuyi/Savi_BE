﻿using Microsoft.AspNetCore.Http;
using Savi_Thrift.Application.DTO.Group;
using Savi_Thrift.Domain;

namespace Savi_Thrift.Application.Interfaces.Services
{
    public interface IGroupSavingsService
    {
        Task<ApiResponse<GroupResponseDto>> CreateGroupAsync(GroupCreationDto groupCreationDto, string userId);

        Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetAllPublicGroupsAsync();
        Task<ApiResponse<GroupResponseDto>> ExploreGroupSavingDetailsAsync(string id);

    }
}
