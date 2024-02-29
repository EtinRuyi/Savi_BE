﻿using Savi_Thrift.Application.DTO.Saving;
using Savi_Thrift.Application.DTO.Wallet;
using Savi_Thrift.Domain;
using Savi_Thrift.Domain.Entities;

namespace Savi_Thrift.Application.Interfaces.Services
{
    public interface ISavingService
	{
		Task<ApiResponse<GoalResponseDto>> CreateGoal(CreateGoalDto createGoalDto);
		Task<ApiResponse<List<GoalResponseDto>>> ViewGoals();
		Task<ApiResponse<List<Saving>>> GetListOfAllUserGoals(string walletNumber);
		Task<ApiResponse<decimal>> GetTotalSavingBalance(string walletNumber);
		Task<ApiResponse<SavingsResponseDto>> CreditPersonalSavings(CreditSavingsDto creditDto);
        Task<ApiResponse<SavingsResponseDto>> WithdrawFundsFromGoalToWallet(CreditWalletFromGoalDto creditDto);
        Task<ApiResponse<DebitResponseDto>> FundsPersonalGoal(FundsPersonalGoalDto personalGoalDto);
        Task<ApiResponse<GetPersonalSavingsDTO>> GetPersonalSavings(string savingsId);
		Task<ApiResponse<decimal>> GetAllUsersSavingBalance();
		Task<ApiResponse<decimal>> GetMonthlySavingBalancePercentage();
		Task<ApiResponse<decimal>> GetMonthlyGroupCreationPercentage();
		Task<ApiResponse<DebitResponseDto>> AutoFundPersonalGoal(string goalId);
	}
}
