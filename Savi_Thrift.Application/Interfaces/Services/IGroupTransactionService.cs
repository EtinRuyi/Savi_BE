﻿
using Savi_Thrift.Application.DTO.GroupTransaction;
using Savi_Thrift.Domain;

namespace Savi_Thrift.Application.Interfaces.Services
{
    public interface IGroupTransactionService
    {
		Task<ApiResponse<GroupTransactionResponseDto>> FundGroup(GroupFundDto groupFundDto);
		Task<ApiResponse<List<GroupUserTransactionResponseDto>>> GetGroupTransactions(string groupId);
	}
}
