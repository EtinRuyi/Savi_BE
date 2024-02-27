﻿using Microsoft.AspNetCore.Http;
using Savi_Thrift.Application.DTO.Group;
using Savi_Thrift.Application.DTO.UserTransaction;
using Savi_Thrift.Domain;

namespace Savi_Thrift.Application.Interfaces.Services
{
    public interface IGroupSavingsService
    {
        Task<ApiResponse<GroupResponseDto>> CreateGroupAsync(GroupCreationDto groupCreationDto, string userId);

        Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetAllPublicGroupsAsync();
		Task<ApiResponse<IEnumerable<GroupResponseDto>>> ListOngoingGroupSavingsAccountsAsync();
        Task<ApiResponse<IEnumerable<GroupResponseDto>>> GetGroupsByUserId(string userId);
		Task<ApiResponse<GroupResponseDto>> GetGroupDetailByIdAsync(string groupId);
        Task<ApiResponse<GroupResponseDto>> ExploreGroupSavingDetailsAsync(string id);
        Task<ApiResponse<List<GroupResponseDto>>> GetRecentGroup();
        Task<ApiResponse<List<GroupResponseDto>>> GetAllGroups();

        Task<ApiResponse<decimal>> TotalSavingsGroup(string userId);


    }
}
