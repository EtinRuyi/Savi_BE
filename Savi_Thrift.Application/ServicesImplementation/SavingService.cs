﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Savi_Thrift.Application.DTO.Saving;
using Savi_Thrift.Application.Interfaces.Repositories;
using Savi_Thrift.Application.Interfaces.Services;
using Savi_Thrift.Domain.Entities;
using Savi_Thrift.Domain;
using Savi_Thrift.Application.DTO.Wallet;
using Hangfire;
using Savi_Thrift.Domain.Enums;

namespace Savi_Thrift.Application.ServicesImplementation
{
	public class SavingService : ISavingService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IWalletService _walletService;
		private readonly ICloudinaryServices<SavingService> _cloudinaryServices;
		private readonly IBackgroundJobClient _backgroundJobClient;

		public SavingService(IUnitOfWork unitOfWork, IMapper mapper, IWalletService walletService, ICloudinaryServices<SavingService> cloudinaryServices, IBackgroundJobClient backgroundJobClient)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_walletService = walletService;
			_cloudinaryServices = cloudinaryServices;
			_backgroundJobClient = backgroundJobClient;
		}

		public async Task<ApiResponse<GoalResponseDto>> CreateGoal(CreateGoalDto createGoalDto)
		{

			var existingBoard = await _unitOfWork.SavingRepository.FindAsync(x => x.Title == createGoalDto.Title);
			if (existingBoard.Count > 0)
			{
				return ApiResponse<GoalResponseDto>.Failed("Goal already exists with this title", 400, new List<string>());
			}
			var validWallet = await _unitOfWork.WalletRepository.FindAsync(x => x.WalletNumber == createGoalDto.WalletNumber);
			if (validWallet.Count < 1)
			{
				return ApiResponse<GoalResponseDto>.Failed("Wallet number not valid", 400, new List<string>());
			}

			try
			{
				var avatar = await _cloudinaryServices.UploadImage(createGoalDto.Avatar);
				if (avatar == null)
				{
					return ApiResponse<GoalResponseDto>.Failed("Failed to upload image to cloudinary",
						StatusCodes.Status500InternalServerError, new List<string>());
				}

				var saving = _mapper.Map<Saving>(createGoalDto);
				saving.Avatar = avatar.Url;
				await _unitOfWork.SavingRepository.AddAsync(saving);
				await _unitOfWork.SaveChangesAsync();
				if (createGoalDto.IsAutomatic)
				{

					if (createGoalDto.Frequency == SavingFrequency.Daily)
					{
						RecurringJob.AddOrUpdate<IRecurringGroupJobs>(saving.Id, (job) => job.AutoFundPersonalSavings(saving.Id), Cron.Daily);
					}
					else if (createGoalDto.Frequency == SavingFrequency.Weekly)
					{
						RecurringJob.AddOrUpdate<IRecurringGroupJobs>(saving.Id, (job) => job.AutoFundPersonalSavings(saving.Id), Cron.Weekly);
					}
					else if (createGoalDto.Frequency == SavingFrequency.Monthly)
					{
						RecurringJob.AddOrUpdate<IRecurringGroupJobs>(saving.Id, (job) => job.AutoFundPersonalSavings(saving.Id), Cron.Monthly);
					}
				}

				var reponseDto = _mapper.Map<GoalResponseDto>(saving);
				return ApiResponse<GoalResponseDto>.Success(reponseDto, "Goal Created Successfully", StatusCodes.Status200OK);

			}
			catch (Exception ex)
			{
				return ApiResponse<GoalResponseDto>.Failed("Error occurred while creating a goal", StatusCodes.Status500InternalServerError, new List<string> { ex.InnerException.ToString() });
			}
		}

		public async Task<ApiResponse<List<Saving>>> GetListOfAllUserGoals(string walletNumber)
		{

			try
			{
				var listOfTargets = await _unitOfWork.SavingRepository.FindAsync(u => u.WalletNumber == walletNumber);
				if (listOfTargets.Any())
				{
					return ApiResponse<List<Saving>>.Success(listOfTargets, " Goals Retrieved Successfully", StatusCodes.Status200OK);

				}
				return ApiResponse<List<Saving>>.Failed("Goal not found", StatusCodes.Status400BadRequest, new List<string>());

			}

			catch (Exception ex)
			{
				return ApiResponse<List<Saving>>.Failed("Error occurred while creating a goal", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });

			}
		}

		public async Task<ApiResponse<List<GoalResponseDto>>> ViewGoals()
		{
			var wallets = await _unitOfWork.SavingRepository.GetAllAsync();

			List<GoalResponseDto> result = new();
			foreach (var wallet in wallets)
			{
				var reponseDto = _mapper.Map<GoalResponseDto>(wallet);
				result.Add(reponseDto);
			}
			return ApiResponse<List<GoalResponseDto>>.Success(result, "Goals retrieved successfully", StatusCodes.Status200OK);
		}


		public async Task<ApiResponse<SavingsResponseDto>> CreditPersonalSavings(CreditSavingsDto creditDto)
		{
			try
			{
				// Assuming you have a method to get personal savings by user ID
				var personalSavings = await _unitOfWork.SavingRepository.GetByIdAsync(creditDto.UserId);

				if (personalSavings == null)
				{

					var savingEntity = _mapper.Map<Saving>(creditDto);

					await _unitOfWork.SavingRepository.AddAsync(savingEntity);
					await _unitOfWork.SaveChangesAsync();
				}

				var response = await _walletService.GetWalletByNumber(creditDto.WalletNumber);

				if (!response.Succeeded)
				{
					return ApiResponse<SavingsResponseDto>.Failed(response.Message, response.StatusCode, response.Errors);
				}

				var wallet = response.Data;

				if (wallet == null)
				{
					//wallet is not found
					return ApiResponse<SavingsResponseDto>.Failed("Wallet not found", StatusCodes.Status404NotFound, new List<string>());
				}

				if (wallet.Balance < creditDto.CreditAmount)
				{

					return ApiResponse<SavingsResponseDto>.Failed("Insufficient funds in the wallet.", StatusCodes.Status200OK, new List<string>());
				}

				// Debit the wallet
				decimal newWalletBalance = wallet.Balance - creditDto.CreditAmount;
				wallet.Balance = newWalletBalance;
				_unitOfWork.WalletRepository.Update(wallet);
				await _unitOfWork.SaveChangesAsync();

				// Credit personal savings
				personalSavings.Balance += creditDto.CreditAmount;
				_unitOfWork.SavingRepository.Update(personalSavings);
				await _unitOfWork.SaveChangesAsync();

				var responseDto = new SavingsResponseDto
				{
					UserId = creditDto.UserId,
					Balance = personalSavings.Balance,
					Message = "Personal savings credited successfully.",
				};

				return ApiResponse<SavingsResponseDto>.Success(responseDto, "Personal savings credited successfully", StatusCodes.Status200OK);
			}
			catch (Exception e)
			{
				return ApiResponse<SavingsResponseDto>.Failed("Failed to credit personal savings. ", StatusCodes.Status500InternalServerError, new List<string> { e.InnerException.ToString() });
			}

		}


		public async Task<ApiResponse<GetPersonalSavingsDTO>> GetPersonalSavings(string savingsId)
		{

			try
			{
				var listOfSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.Id == savingsId);
				if (!listOfSavings.Any())
				{
					return ApiResponse<GetPersonalSavingsDTO>.Failed("Saving Not Found", StatusCodes.Status404NotFound, new List<string>());

				}
				var Savings = listOfSavings.First();

				var SavingsDTO = _mapper.Map<GetPersonalSavingsDTO>(Savings);



				return ApiResponse<GetPersonalSavingsDTO>.Success(SavingsDTO, "Saving Retrieved Successfully", StatusCodes.Status200OK);

			}

			catch (Exception ex)
			{
				return ApiResponse<GetPersonalSavingsDTO>.Failed("Error occurred while retrieving savings", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });

			}
		}

		public async Task<ApiResponse<decimal>> GetTotalSavingBalance(string walletNumber)
		{

			try
			{
				var listOfSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.WalletNumber == walletNumber);
				if (!listOfSavings.Any())
				{
					return ApiResponse<decimal>.Failed("No Savings Found", StatusCodes.Status404NotFound, new List<string>());

				}
				decimal total = 0;
				foreach (var savings in listOfSavings)
				{
					total += savings.Balance;
				}
				return ApiResponse<decimal>.Success(total, "TotalSavingBalance Retrieved Successfully", StatusCodes.Status200OK);
			}

			catch (Exception ex)
			{
				return ApiResponse<decimal>.Failed("Error occurred while retrieving savings balance", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });

			}
		}


		public async Task<ApiResponse<decimal>> GetAllUsersSavingBalance()
		{

			try
			{
				var listOfSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.IsDeleted == false);
				if (!listOfSavings.Any())
				{
					return ApiResponse<decimal>.Failed("No Savings Found", StatusCodes.Status404NotFound, new List<string>());

				}
				decimal total = 0;
				foreach (var savings in listOfSavings)
				{
					total += savings.Balance;
				}
				return ApiResponse<decimal>.Success(total, "TotalSavingBalance Retrieved Successfully", StatusCodes.Status200OK);
			}

			catch (Exception ex)
			{
				return ApiResponse<decimal>.Failed("Error occurred while retrieving savings balance", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });

			}
		}

		public async Task<ApiResponse<decimal>> GetMonthlySavingBalancePercentage()
		{
			try
			{
				DateTime now = DateTime.Now;
				DateTime thisMonth = new DateTime(now.Year, now.Month, 1);
				DateTime nextMonth = thisMonth.AddMonths(1);

				var listOfSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.IsDeleted == false && u.CreatedAt >= thisMonth && u.CreatedAt < nextMonth);

				// Calculate total savings for the month
				decimal thismonth = listOfSavings.Sum(s => s.Balance);

				DateTime today = DateTime.Today;
				DateTime firstDayOfLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
				DateTime lastDayOfLastMonth = firstDayOfLastMonth.AddMonths(1).AddDays(-1);

				var listSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.IsDeleted == false && u.CreatedAt >= firstDayOfLastMonth && u.CreatedAt <= lastDayOfLastMonth);

				decimal lastmonth = listSavings.Sum(s => s.Balance);
				decimal val = 0;
				if (thismonth != 0 & lastmonth != 0)
				{
					val = (thismonth - lastmonth / lastmonth) * 100;
				}
				if (lastmonth == 0 && thismonth != 0)
				{
					val = 100;
				}
				if (lastmonth > thismonth)
				{
					if (val != 0)
					{
						var pl = "-" + val;
						val = Decimal.Parse(pl);
					}
				}


				return ApiResponse<decimal>.Success(val, "TotalSavingBalance Retrieved Successfully", StatusCodes.Status200OK);
			}

			catch (Exception ex)
			{
				return ApiResponse<decimal>.Failed("Error occurred while retrieving savings balance", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });

			}
		}


		public async Task<ApiResponse<decimal>> GetMonthlySavingPercentage()
		{
			try
			{
				DateTime now = DateTime.Now;
				DateTime thisMonth = new DateTime(now.Year, now.Month, 1);
				DateTime nextMonth = thisMonth.AddMonths(1);

				var listOfSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.IsDeleted == false && u.CreatedAt >= thisMonth && u.CreatedAt < nextMonth);

				// Calculate total savings for the month
				decimal thismonth = listOfSavings.Sum(s => s.Balance);

				DateTime today = DateTime.Today;
				DateTime firstDayOfLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
				DateTime lastDayOfLastMonth = firstDayOfLastMonth.AddMonths(1).AddDays(-1);

				var listSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.IsDeleted == false && u.CreatedAt >= firstDayOfLastMonth && u.CreatedAt <= lastDayOfLastMonth);

				decimal lastmonth = listSavings.Sum(s => s.Balance);
				decimal val = 0;
				if (thismonth != 0 & lastmonth != 0)
				{
					val = (thismonth - lastmonth / lastmonth) * 100;
				}
				if (lastmonth == 0 && thismonth != 0)
				{
					val = 100;
				}
				if (lastmonth > thismonth)
				{
					if (val != 0)
					{
						var pl = "-" + val;
						val = Decimal.Parse(pl);
					}
				}
				return ApiResponse<decimal>.Success(val, "TotalSavingBalance Retrieved Successfully", StatusCodes.Status200OK);
			}
			catch (Exception ex)
			{
				return ApiResponse<decimal>.Failed("Error occurred while retrieving savings balance", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
			}
		}

		public async Task<ApiResponse<decimal>> GetMonthlyGroupCreationPercentage()
		{
			try
			{
				DateTime now = DateTime.Now;
				DateTime thisMonth = new DateTime(now.Year, now.Month, 1);
				DateTime nextMonth = thisMonth.AddMonths(1);

				var listOfSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.IsDeleted == false && u.CreatedAt >= thisMonth && u.CreatedAt < nextMonth);

				// Calculate total savings for the month
				decimal thismonth = listOfSavings.Count;

				DateTime today = DateTime.Today;
				DateTime firstDayOfLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
				DateTime lastDayOfLastMonth = firstDayOfLastMonth.AddMonths(1).AddDays(-1);

				var listSavings = await _unitOfWork.SavingRepository.FindAsync(u => u.IsDeleted == false && u.CreatedAt >= firstDayOfLastMonth && u.CreatedAt <= lastDayOfLastMonth);

				decimal lastmonth = listSavings.Count;
				decimal val = 0;
				if (thismonth != 0 & lastmonth != 0)
				{
					val = (thismonth - lastmonth / lastmonth) * 100;
				}
				if (lastmonth == 0 && thismonth != 0)
				{
					val = 100;
				}
				if (lastmonth > thismonth)
				{
					if (val != 0)
					{
						var pl = "-" + val;
						val = Decimal.Parse(pl);
					}
				}
				return ApiResponse<decimal>.Success(val, "TotalSavingBalance Retrieved Successfully", StatusCodes.Status200OK);
			}
			catch (Exception ex)
			{
				return ApiResponse<decimal>.Failed("Error occurred while retrieving savings balance", StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
			}
		}


		public async Task<ApiResponse<SavingsResponseDto>> WithdrawFundsFromGoalToWallet(CreditWalletFromGoalDto creditDto)
		{
			try
			{
				var goalsSaving = await _unitOfWork.SavingRepository.FindAsync(u => u.WalletNumber == creditDto.WalletNumber);
				var goalsSavings = goalsSaving.FirstOrDefault();

				if (goalsSavings == null)
				{
					return ApiResponse<SavingsResponseDto>.Failed("Goal savings is null.", StatusCodes.Status400BadRequest, new List<string>());
				}

				// Check if there are insufficient funds in the savings goal
				if (goalsSavings.Balance < creditDto.GoalAmount)
				{
					return ApiResponse<SavingsResponseDto>.Failed("Insufficient funds in the savings goal.", StatusCodes.Status400BadRequest, new List<string>());
				}

				// Calculate new balance and update
				decimal newBalance = goalsSavings.Balance - creditDto.GoalAmount;
				goalsSavings.Balance = newBalance;
				_unitOfWork.SavingRepository.Update(goalsSavings);
				await _unitOfWork.SaveChangesAsync();

				// Check if the user already has a wallet
				var existingWalletDetails = await _unitOfWork.WalletRepository.FindAsync(w => w.WalletNumber == creditDto.WalletNumber);
				var existingWallet = existingWalletDetails.FirstOrDefault();

				if (existingWallet != null)
				{
					// Update existing wallet balance
					existingWallet.Balance += creditDto.GoalAmount;
					_unitOfWork.WalletRepository.Update(existingWallet);
				}
				else
				{
					// Create a new wallet entry
					var wallet = new Wallet
					{
						UserId = creditDto.WalletNumber,
						Balance = creditDto.GoalAmount
					};
					await _unitOfWork.WalletRepository.AddAsync(wallet);
				}

				await _unitOfWork.SaveChangesAsync();

				// Create a user transaction
				var userFund = new UserTransaction
				{
					ModifiedAt = DateTime.Now,
					Amount = creditDto.GoalAmount,
					ActionId = 2,
					UserId = creditDto.WalletNumber // Verify if this should be set to creditDto.WalletId
				};
				await _unitOfWork.UserTransactionRepository.AddAsync(userFund);
				await _unitOfWork.SaveChangesAsync();

				// Prepare and return the response
				var responseDto = new SavingsResponseDto
				{
					UserId = creditDto.WalletNumber,
					Balance = goalsSavings.Balance,
					Message = "Funds successfully withdrawn from savings goal to wallet.",
				};

				return ApiResponse<SavingsResponseDto>.Success(responseDto, "Funds withdrawn successfully", StatusCodes.Status200OK);
			}
			catch (Exception e)
			{
				// Handle exceptions and return an error response
				return ApiResponse<SavingsResponseDto>.Failed("Failed to withdraw funds.", StatusCodes.Status500InternalServerError, new List<string> { e.Message });
			}
		}

		public async Task<ApiResponse<DebitResponseDto>> AutoFundPersonalGoal(string goalId)
		{
			try
			{
				var goals = await _unitOfWork.SavingRepository.FindAsync(u => u.Id == goalId);
				if (goals.Count == 0)
				{
					return ApiResponse<DebitResponseDto>.Failed(new List<string>() { "Invalid goal saving Id." });
				}
				var goal = goals.First();
				DateTime end_date = goal.TargetDate;
				DateTime today = DateTime.Now;
				if (today > end_date)
				{
					RecurringJob.RemoveIfExists(goalId);
					return ApiResponse<DebitResponseDto>.Failed(new List<string>() { "Savings target date has passed. This job is ended." });
				}

				var wallets = await _unitOfWork.WalletRepository.FindAsync(x => x.WalletNumber == goal.WalletNumber);
				if (wallets.Count == 0)
				{
					return ApiResponse<DebitResponseDto>.Failed(new List<string>() { "Invalid wallet number." });
				}

				var wallet = wallets.First();
				if (wallet.Balance < goal.GoalAmount)
				{
					return ApiResponse<DebitResponseDto>.Failed(new List<string>() { "Insufficient balance in wallet." });
				}

				decimal newBalance = wallet.Balance - goal.GoalAmount;
				wallet.Balance = newBalance;
				_unitOfWork.WalletRepository.Update(wallet);
				await _unitOfWork.SaveChangesAsync();

				var userTransaction = new UserTransaction
				{
					ActionId = 2,
					Amount = goal.GoalAmount,
					WalletNumber = wallet.WalletNumber,
					Description = "Automatically Funded " + goal.Title,
					SavingsId = goal.Id,
					UserId = wallet.UserId
				};

				await _unitOfWork.UserTransactionRepository.AddAsync(userTransaction);
				await _unitOfWork.SaveChangesAsync();

				var savings = await _unitOfWork.SavingRepository.GetByIdAsync(goalId);
				savings.Balance = savings.Balance + goal.GoalAmount;
				_unitOfWork.SavingRepository.Update(savings);
				await _unitOfWork.SaveChangesAsync();

				var debitResponse = new DebitResponseDto
				{
					WalletNumber = wallet.WalletNumber,
					Balance = newBalance,
					Message = "Your goal has been funded successfully.",
				};
				return ApiResponse<DebitResponseDto>.Success(debitResponse, "Goal funded automatically", StatusCodes.Status200OK);
			}
			catch (Exception e)
			{
				// Handle exceptions and return an error response
				return ApiResponse<DebitResponseDto>.Failed("Error auto funding goal.", StatusCodes.Status500InternalServerError, new List<string> { e.Message });
			}
		}

		public async Task<ApiResponse<DebitResponseDto>> FundsPersonalGoal(FundsPersonalGoalDto personalGoalDto)
		{
			try
			{
				// Retrieve user's savings goal              
				var walletInfo = await _unitOfWork.WalletRepository.FindAsync(u => u.WalletNumber == personalGoalDto.WalletNumber);
				var wallet = walletInfo.FirstOrDefault();
				//Check if the goalsSavings is null
				if (wallet == null)
				{
					return ApiResponse<DebitResponseDto>.Failed("Goal savings is null.", StatusCodes.Status400BadRequest, new List<string>());
				}
				// Check if there are sufficient funds in the savings goal
				if (wallet.Balance < personalGoalDto.Amount)
				{
					return ApiResponse<DebitResponseDto>.Failed("Insufficient funds in the savings goal.", StatusCodes.Status400BadRequest, new List<string>());
				}

				decimal newBalance = wallet.Balance - personalGoalDto.Amount;
				wallet.Balance = newBalance;
				_unitOfWork.WalletRepository.Update(wallet);
				await _unitOfWork.SaveChangesAsync();


				var userTransaction = new UserTransaction
				{
					ActionId = 1,
					Amount = personalGoalDto.Amount,
					WalletNumber = personalGoalDto.WalletNumber,
					Description = "Funded a saving",
					SavingsId = personalGoalDto.SavingsId,
					UserId = personalGoalDto.UserId,
				};

				await _unitOfWork.UserTransactionRepository.AddAsync(userTransaction);
				await _unitOfWork.SaveChangesAsync();

				var savings = await _unitOfWork.SavingRepository.GetByIdAsync(personalGoalDto.SavingsId);
				savings.Balance = savings.Balance + personalGoalDto.Amount;

				_unitOfWork.SavingRepository.Update(savings);
				await _unitOfWork.SaveChangesAsync();

				var debitResponse = new DebitResponseDto
				{
					WalletNumber = personalGoalDto.WalletNumber,
					Balance = newBalance,
					Message = "Your goal has been funded successfully.",
				};
				return ApiResponse<DebitResponseDto>.Success(debitResponse, "Goal funded successfully", StatusCodes.Status200OK);
			}
			catch (Exception e)
			{
				// Handle exceptions and return an error response
				return ApiResponse<DebitResponseDto>.Failed("Error funding goal.", StatusCodes.Status500InternalServerError, new List<string> { e.Message });
			}
		}


	}
}
