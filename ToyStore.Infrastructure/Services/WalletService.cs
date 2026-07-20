using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using BCryptNet = BCrypt.Net.BCrypt;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.DTOs;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Wallets;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

public class WalletService : IWalletService
{
    private const string WalletStatusActive = "Active";
    private const string WalletStatusFrozen = "Frozen";
    private const string WalletStatusClosed = "Closed";

    private const int MaxFailedAttempts = 3;
    private const int MaxTotalFailedAttempts = 6;

    private const string ForgotOtpPrefix = "wallet:pin:forgot:";
    private const string ForgotOtpCooldownPrefix = "wallet:pin:forgot:cooldown:";
    private const string ForgotOtpVerifiedPrefix = "wallet:pin:forgot:verified:";
    private const string TopUpVerifyPrefix = "wallet:topup:verify:";
    private const string TopUpAttemptPrefix = "wallet:topup:attempt:";

    private static readonly TimeSpan PinLockDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan OtpCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan TopUpVerifyExpiry = TimeSpan.FromMinutes(10);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRedisService _redisService;
    private readonly IEmailService _emailService;
    private readonly SePayOptions _sePayOptions;
    private readonly IMapper _mapper;
    private readonly ILogger<WalletService> _logger;
    private readonly IValidator<CreateWalletRequestDto> _createWalletValidator;
    private readonly IValidator<VerifyWalletPinRequestDto> _verifyPinValidator;
    private readonly IValidator<CreateSePayTopUpQrRequestDto> _createSePayTopUpQrValidator;
    private readonly IValidator<ChangeWalletPinRequestDto> _changePinValidator;
    private readonly IValidator<VerifyForgotWalletPinOtpRequestDto> _verifyOtpValidator;
    private readonly IValidator<ResetForgotWalletPinRequestDto> _resetPinValidator;

    public WalletService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRedisService redisService,
        IEmailService emailService,
        IOptions<SePayOptions> sePayOptions,
        IMapper mapper,
        ILogger<WalletService> logger,
        IValidator<CreateWalletRequestDto> createWalletValidator,
        IValidator<VerifyWalletPinRequestDto> verifyPinValidator,
        IValidator<CreateSePayTopUpQrRequestDto> createSePayTopUpQrValidator,
        IValidator<ChangeWalletPinRequestDto> changePinValidator,
        IValidator<VerifyForgotWalletPinOtpRequestDto> verifyOtpValidator,
        IValidator<ResetForgotWalletPinRequestDto> resetPinValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _redisService = redisService;
        _emailService = emailService;
        _sePayOptions = sePayOptions.Value;
        _mapper = mapper;
        _logger = logger;
        _createWalletValidator = createWalletValidator;
        _verifyPinValidator = verifyPinValidator;
        _createSePayTopUpQrValidator = createSePayTopUpQrValidator;
        _changePinValidator = changePinValidator;
        _verifyOtpValidator = verifyOtpValidator;
        _resetPinValidator = resetPinValidator;
    }

    public async Task<Result<WalletDto>> GetMyWalletAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<WalletDto>.Unauthorized("User is not authenticated.");
        }

        var wallet = await _unitOfWork.Wallets.GetByAccountIdWithActivePinAsync(accountId, cancellationToken);
        if (wallet == null)
        {
            return Result<WalletDto>.NotFound("Wallet");
        }

        return Result<WalletDto>.Success(MapToWalletDto(wallet));
    }

    public async Task<Result<PaginatedResponse<WalletTransactionDto>>> GetWalletTransactionsAsync(
        WalletTransactionQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<PaginatedResponse<WalletTransactionDto>>.Unauthorized("User is not authenticated.");
        }

        query ??= new WalletTransactionQueryDto();
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(accountId, cancellationToken);
        if (wallet == null)
        {
            return Result<PaginatedResponse<WalletTransactionDto>>.NotFound("Wallet");
        }

        var totalCount = await _unitOfWork.Wallets.CountTransactionsByWalletIdAsync(wallet.WalletId, cancellationToken);
        var transactions = await _unitOfWork.Wallets.GetTransactionsByWalletIdAsync(
            wallet.WalletId,
            pageNumber,
            pageSize,
            cancellationToken);

        var items = transactions.Select(x => new WalletTransactionDto
        {
            WalletTransactionId = x.WalletTransactionId,
            RelatedOrderId = x.RelatedOrderId,
            RelatedOrderCode = x.RelatedOrder?.OrderCode,
            TxnType = x.TxnType,
            Direction = x.Direction,
            Amount = x.Amount,
            SignedAmount = string.Equals(x.Direction, "DR", StringComparison.OrdinalIgnoreCase)
                ? -x.Amount
                : x.Amount,
            Method = x.Method,
            Status = x.Status,
            Reason = x.Reason,
            CreatedAt = x.CreatedAt,
            CompletedAt = x.CompletedAt
        }).ToList();

        var result = new PaginatedResponse<WalletTransactionDto>(items, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<WalletTransactionDto>>.Success(result);
    }

    public async Task<Result<WalletDto>> CreateWalletWithPinAsync(
        CreateWalletRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createWalletValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<WalletDto>();
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<WalletDto>.Unauthorized("User is not authenticated.");
        }

        var existingWallet = await _unitOfWork.Wallets.GetByAccountIdAsync(accountId, cancellationToken);
        var now = DateTime.UtcNow;

        var walletPin = new WalletPin
        {
            PinHash = HashPin(dto.Pin),
            IsActive = true,
            FailedAttempts = 0,
            TotalFailedAttempts = 0,
            LastChangedAt = now,
            CreatedAt = now
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            Wallet wallet;
            if (existingWallet != null)
            {
                var existingPin = await _unitOfWork.Wallets.GetActivePinByWalletIdAsync(
                    existingWallet.WalletId, cancellationToken);
                if (existingPin != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<WalletDto>.Conflict("Wallet already exists for this account.");
                }

                wallet = existingWallet;
                walletPin.WalletId = wallet.WalletId;
                await _unitOfWork.Wallets.AddPinAsync(walletPin, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "PIN attached to existing wallet {WalletId} for account {AccountId}.",
                    wallet.WalletId, accountId);
            }
            else
            {
                wallet = new Wallet
                {
                    AccountId = accountId,
                    Currency = "VND",
                    Balance = 0,
                    Status = WalletStatusActive,
                    CreatedAt = now
                };

                await _unitOfWork.Wallets.CreateAsync(wallet, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                walletPin.WalletId = wallet.WalletId;
                await _unitOfWork.Wallets.AddPinAsync(walletPin, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Wallet {WalletId} created for account {AccountId}.", wallet.WalletId, accountId);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result<WalletDto>.Success(MapToWalletDto(wallet, hasPin: true));
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogWarning(ex, "Conflict while creating wallet for account {AccountId}.", accountId);
            return Result<WalletDto>.Conflict("Wallet already exists for this account.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create wallet for account {AccountId}.", accountId);
            throw;
        }
    }

    public async Task<Result<VerifyWalletPinResponseDto>> VerifyWalletPinAsync(
        VerifyWalletPinRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await _verifyPinValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<VerifyWalletPinResponseDto>();
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<VerifyWalletPinResponseDto>.Unauthorized("User is not authenticated.");
        }

        var actionType = dto.ActionType.Trim().ToUpperInvariant();
        var wallet = await _unitOfWork.Wallets.GetByAccountIdWithActivePinAsync(accountId, cancellationToken);
        if (wallet == null)
        {
            return Result<VerifyWalletPinResponseDto>.NotFound("Wallet");
        }

        if (IsFrozenOrClosed(wallet.Status))
        {
            return Result<VerifyWalletPinResponseDto>.BusinessError(
                "Wallet is not available. Please contact support.");
        }

        if (!IsWalletActive(wallet.Status))
        {
            return Result<VerifyWalletPinResponseDto>.BusinessError("Wallet is not active.");
        }

        var activePin = wallet.WalletPins.FirstOrDefault(x => x.IsActive);
        if (activePin == null)
        {
            return Result<VerifyWalletPinResponseDto>.BusinessError("Wallet PIN is not initialized.");
        }

        var now = DateTime.UtcNow;
        if (activePin.LockedUntil.HasValue && activePin.LockedUntil.Value > now)
        {
            return Result<VerifyWalletPinResponseDto>.BusinessError("PIN is currently locked. Please try again later.");
        }

        if (activePin.LockedUntil.HasValue && activePin.LockedUntil.Value <= now)
        {
            // Lock window has elapsed, start a fresh consecutive-attempt cycle.
            activePin.FailedAttempts = 0;
            activePin.LockedUntil = null;
            activePin.UpdatedAt = now;
        }

        if (VerifyPin(dto.Pin, activePin.PinHash))
        {
            activePin.FailedAttempts = 0;
            activePin.LockedUntil = null;
            activePin.UpdatedAt = now;
            _unitOfWork.Wallets.UpdatePin(activePin);

            await _unitOfWork.Wallets.AddPinAttemptAsync(new WalletPinAttempt
            {
                WalletId = wallet.WalletId,
                AccountId = accountId,
                ActionType = actionType,
                IsSuccess = true,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            string? topUpToken = null;
            if (string.Equals(actionType, "TOP_UP", StringComparison.OrdinalIgnoreCase))
            {
                topUpToken = GenerateTopUpToken();
                await _redisService.SetAsync(
                    BuildTopUpVerifyKey(accountId, topUpToken),
                    wallet.WalletId.ToString(),
                    TopUpVerifyExpiry);
            }

            return Result<VerifyWalletPinResponseDto>.Success(new VerifyWalletPinResponseDto
            {
                WalletId = wallet.WalletId,
                ActionType = actionType,
                IsVerified = true,
                RemainingAttempts = MaxFailedAttempts,
                LockedUntil = null,
                WalletStatus = wallet.Status,
                TopUpToken = topUpToken
            });
        }

        activePin.FailedAttempts = (byte)Math.Min(MaxFailedAttempts, activePin.FailedAttempts + 1);
        activePin.TotalFailedAttempts = (byte)Math.Min(MaxTotalFailedAttempts, activePin.TotalFailedAttempts + 1);
        activePin.UpdatedAt = now;

        var isLocked = activePin.FailedAttempts >= MaxFailedAttempts;
        if (isLocked)
        {
            activePin.LockedUntil = now.Add(PinLockDuration);
        }

        var isFrozen = activePin.TotalFailedAttempts >= MaxTotalFailedAttempts;
        if (isFrozen)
        {
            wallet.Status = WalletStatusFrozen;
            wallet.UpdatedAt = now;
            _unitOfWork.Wallets.UpdateWallet(wallet);
        }

        _unitOfWork.Wallets.UpdatePin(activePin);
        await _unitOfWork.Wallets.AddPinAttemptAsync(new WalletPinAttempt
        {
            WalletId = wallet.WalletId,
            AccountId = accountId,
            ActionType = actionType,
            IsSuccess = false,
            CreatedAt = now
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (isFrozen)
        {
            return Result<VerifyWalletPinResponseDto>.BusinessError(
                "Wallet has been frozen due to too many incorrect PIN attempts. Please contact support to unfreeze your wallet.");
        }

        if (isLocked)
        {
            return Result<VerifyWalletPinResponseDto>.BusinessError("PIN is locked for 5 minutes due to 3 consecutive failed attempts.");
        }

        var remainingAttempts = MaxFailedAttempts - activePin.FailedAttempts;
        return Result<VerifyWalletPinResponseDto>.BusinessError(
            $"Incorrect PIN. You have {remainingAttempts} attempt(s) remaining before temporary lock.");
    }

    public async Task<Result<SePayTopUpQrResponseDto>> CreateSePayTopUpQrAsync(
        CreateSePayTopUpQrRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createSePayTopUpQrValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<SePayTopUpQrResponseDto>();
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<SePayTopUpQrResponseDto>.Unauthorized("User is not authenticated.");
        }

        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(accountId, cancellationToken);
        if (wallet == null)
        {
            return Result<SePayTopUpQrResponseDto>.NotFound("Wallet");
        }

        if (IsFrozenOrClosed(wallet.Status) || !IsWalletActive(wallet.Status))
        {
            return Result<SePayTopUpQrResponseDto>.BusinessError("Wallet is not available for top-up.");
        }

        var verifyKey = BuildTopUpVerifyKey(accountId, dto.TopUpToken.Trim());
        var verifyValue = await _redisService.GetAsync(verifyKey);
        if (string.IsNullOrWhiteSpace(verifyValue))
        {
            return Result<SePayTopUpQrResponseDto>.BusinessError("Top-up PIN verification has expired. Please verify PIN again.");
        }

        if (!string.Equals(verifyValue, wallet.WalletId.ToString(), StringComparison.Ordinal))
        {
            return Result<SePayTopUpQrResponseDto>.Unauthorized("Invalid top-up verification token.");
        }

        var amount = decimal.Round(dto.Amount, 0, MidpointRounding.AwayFromZero);
        var attemptCode = BuildTopUpAttemptCode(accountId);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(Math.Max(1, _sePayOptions.PaymentTtlMinutes));

        var topUpAttempt = new WalletTopUpAttemptCache
        {
            AttemptCode = attemptCode,
            AccountId = accountId,
            WalletId = wallet.WalletId,
            Amount = amount,
            Status = "PENDING",
            CreatedAt = now,
            CompletedAt = null,
            WalletTransactionId = null
        };

        await _redisService.SetAsync(
            BuildTopUpAttemptKey(attemptCode),
            JsonSerializer.Serialize(topUpAttempt),
            TimeSpan.FromHours(24));

        var qrImageUrl = BuildVietQrUrl(attemptCode, (long)amount);

        return Result<SePayTopUpQrResponseDto>.Success(new SePayTopUpQrResponseDto
        {
            AttemptCode = attemptCode,
            Amount = amount,
            QrImageUrl = qrImageUrl,
            ExpiresAt = expiresAt,
            BankName = string.IsNullOrWhiteSpace(_sePayOptions.BankName) ? _sePayOptions.BankCode : _sePayOptions.BankName,
            BankCode = _sePayOptions.BankCode,
            AccountNumber = _sePayOptions.AccountNumber,
            AccountName = _sePayOptions.AccountName
        });
    }

    public async Task<Result<SePayTopUpStatusResponseDto>> GetSePayTopUpStatusAsync(
        string attemptCode,
        CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<SePayTopUpStatusResponseDto>.Unauthorized("User is not authenticated.");
        }

        var normalizedAttemptCode = (attemptCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedAttemptCode)
            || !normalizedAttemptCode.StartsWith("WLT", StringComparison.OrdinalIgnoreCase))
        {
            return Result<SePayTopUpStatusResponseDto>.BusinessError("Invalid top-up attempt code.");
        }

        var raw = await _redisService.GetAsync(BuildTopUpAttemptKey(normalizedAttemptCode));
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result<SePayTopUpStatusResponseDto>.NotFound("Top-up attempt");
        }

        WalletTopUpAttemptCache? topUpAttempt;
        try
        {
            topUpAttempt = JsonSerializer.Deserialize<WalletTopUpAttemptCache>(raw);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Top-up attempt cache parse failed for code {AttemptCode}", normalizedAttemptCode);
            return Result<SePayTopUpStatusResponseDto>.Failure("INTERNAL_ERROR", "Top-up status data is invalid.");
        }

        if (topUpAttempt == null || topUpAttempt.AccountId != accountId)
        {
            return Result<SePayTopUpStatusResponseDto>.NotFound("Top-up attempt");
        }

        return Result<SePayTopUpStatusResponseDto>.Success(new SePayTopUpStatusResponseDto
        {
            AttemptCode = topUpAttempt.AttemptCode,
            Amount = topUpAttempt.Amount,
            Status = topUpAttempt.Status,
            WalletTransactionId = topUpAttempt.WalletTransactionId,
            CreatedAt = topUpAttempt.CreatedAt,
            CompletedAt = topUpAttempt.CompletedAt
        });
    }

    public async Task<Result> ChangeWalletPinAsync(
        ChangeWalletPinRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await _changePinValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");
        }

        var wallet = await _unitOfWork.Wallets.GetByAccountIdWithActivePinAsync(accountId, cancellationToken);
        if (wallet == null)
        {
            return Result.NotFound("Wallet");
        }

        if (!IsWalletActive(wallet.Status))
        {
            return Result.BusinessError("Wallet is not active. Please contact support if your wallet is frozen.");
        }

        var activePin = wallet.WalletPins.FirstOrDefault(x => x.IsActive);
        if (activePin == null)
        {
            return Result.BusinessError("Wallet PIN is not initialized.");
        }

        if (!VerifyPin(dto.OldPin, activePin.PinHash))
        {
            return Result.BusinessError("Old PIN is incorrect.");
        }

        var now = DateTime.UtcNow;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Wallets.DeactivateActivePinsAsync(wallet.WalletId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.Wallets.AddPinAsync(new WalletPin
            {
                WalletId = wallet.WalletId,
                PinHash = HashPin(dto.NewPin),
                IsActive = true,
                FailedAttempts = 0,
                TotalFailedAttempts = 0,
                LockedUntil = null,
                LastChangedAt = now,
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Wallet PIN changed for account {AccountId}, wallet {WalletId}.", accountId, wallet.WalletId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to change wallet PIN for account {AccountId}.", accountId);
            throw;
        }
    }

    public async Task<Result> SendForgotWalletPinOtpAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");
        }

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.IsDeleted || string.IsNullOrWhiteSpace(account.Email))
        {
            return Result.NotFound("Account");
        }

        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(accountId, cancellationToken);
        if (wallet == null)
        {
            return Result.NotFound("Wallet");
        }

        if (string.Equals(wallet.Status, WalletStatusFrozen, StringComparison.OrdinalIgnoreCase))
        {
            return Result.BusinessError("Wallet is frozen. Please contact support to unfreeze your wallet.");
        }

        if (string.Equals(wallet.Status, WalletStatusClosed, StringComparison.OrdinalIgnoreCase))
        {
            return Result.BusinessError("Wallet is closed.");
        }

        var cooldownKey = $"{ForgotOtpCooldownPrefix}{accountId}";
        var otpKey = $"{ForgotOtpPrefix}{accountId}";
        var verifiedKey = $"{ForgotOtpVerifiedPrefix}{accountId}";

        try
        {
            if (await _redisService.ExistsAsync(cooldownKey))
            {
                return Result.BusinessError("OTP was sent recently. Please wait 60 seconds and try again.");
            }

            var otpCode = GenerateOtpCode();
            await _redisService.SetAsync(otpKey, otpCode, OtpExpiry);
            await _redisService.SetAsync(cooldownKey, "1", OtpCooldown);
            await _redisService.DeleteAsync(verifiedKey);

            await _emailService.SendForgotWalletPinOtpEmailAsync(account.Email.Trim(), otpCode, cancellationToken);
            _logger.LogInformation("Forgot wallet PIN OTP sent for account {AccountId}.", accountId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send forgot wallet PIN OTP for account {AccountId}.", accountId);
            try
            {
                await _redisService.DeleteAsync(otpKey);
                await _redisService.DeleteAsync(cooldownKey);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup OTP cache for account {AccountId}.", accountId);
            }

            return Result.BadGateway("Unable to send OTP at the moment. Please try again later.");
        }
    }

    public async Task<Result> VerifyForgotWalletPinOtpAsync(
        VerifyForgotWalletPinOtpRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await _verifyOtpValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");
        }

        var wallet = await _unitOfWork.Wallets.GetByAccountIdAsync(accountId, cancellationToken);
        if (wallet == null)
        {
            return Result.NotFound("Wallet");
        }

        if (string.Equals(wallet.Status, WalletStatusFrozen, StringComparison.OrdinalIgnoreCase))
        {
            return Result.BusinessError("Wallet is frozen. Please contact support to unfreeze your wallet.");
        }

        if (string.Equals(wallet.Status, WalletStatusClosed, StringComparison.OrdinalIgnoreCase))
        {
            return Result.BusinessError("Wallet is closed.");
        }

        var otpKey = $"{ForgotOtpPrefix}{accountId}";
        var verifiedKey = $"{ForgotOtpVerifiedPrefix}{accountId}";

        try
        {
            var storedOtp = await _redisService.GetAsync(otpKey);
            if (storedOtp == null)
            {
                return Result.Failure("OTP_EXPIRED", "OTP has expired or was not requested.");
            }

            if (!string.Equals(storedOtp, dto.OtpCode, StringComparison.Ordinal))
            {
                return Result.Failure("OTP_INVALID", "Invalid OTP code.");
            }

            await _redisService.SetAsync(verifiedKey, "1", OtpExpiry);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify forgot wallet PIN OTP for account {AccountId}.", accountId);
            return Result.BadGateway("Unable to verify OTP at the moment. Please try again later.");
        }
    }

    public async Task<Result> ResetForgotWalletPinAsync(
        ResetForgotWalletPinRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation = await _resetPinValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");
        }

        var wallet = await _unitOfWork.Wallets.GetByAccountIdWithActivePinAsync(accountId, cancellationToken);
        if (wallet == null)
        {
            return Result.NotFound("Wallet");
        }

        if (string.Equals(wallet.Status, WalletStatusFrozen, StringComparison.OrdinalIgnoreCase))
        {
            return Result.BusinessError("Wallet is frozen. Please contact support to unfreeze your wallet.");
        }

        if (string.Equals(wallet.Status, WalletStatusClosed, StringComparison.OrdinalIgnoreCase))
        {
            return Result.BusinessError("Wallet is closed.");
        }

        var otpKey = $"{ForgotOtpPrefix}{accountId}";
        var cooldownKey = $"{ForgotOtpCooldownPrefix}{accountId}";
        var verifiedKey = $"{ForgotOtpVerifiedPrefix}{accountId}";

        try
        {
            var isVerified = await _redisService.ExistsAsync(verifiedKey);
            if (!isVerified)
            {
                return Result.Failure("OTP_EXPIRED", "OTP verification has expired. Please verify OTP again.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check OTP verification state for account {AccountId}.", accountId);
            return Result.BadGateway("Unable to verify OTP state. Please try again later.");
        }

        var now = DateTime.UtcNow;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Wallets.DeactivateActivePinsAsync(wallet.WalletId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.Wallets.AddPinAsync(new WalletPin
            {
                WalletId = wallet.WalletId,
                PinHash = HashPin(dto.NewPin),
                IsActive = true,
                FailedAttempts = 0,
                TotalFailedAttempts = 0,
                LockedUntil = null,
                LastChangedAt = now,
                CreatedAt = now
            }, cancellationToken);



            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to reset wallet PIN for account {AccountId}.", accountId);
            throw;
        }

        try
        {
            await _redisService.DeleteAsync(otpKey);
            await _redisService.DeleteAsync(cooldownKey);
            await _redisService.DeleteAsync(verifiedKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup wallet PIN OTP keys for account {AccountId}.", accountId);
        }

        return Result.Success();
    }

    private static WalletDto MapToWalletDto(Wallet wallet, bool? hasPin = null) =>
        new()
        {
            WalletId = wallet.WalletId,
            Currency = wallet.Currency,
            Balance = wallet.Balance,
            LockedBalance = wallet.LockedBalance,
            AvailableBalance = wallet.Balance - wallet.LockedBalance,
            Status = wallet.Status,
            HasPin = hasPin ?? wallet.WalletPins.Any(p => p.IsActive),
        };

    private static bool IsWalletActive(string status) =>
        string.Equals(status, WalletStatusActive, StringComparison.OrdinalIgnoreCase);

    private static bool IsFrozenOrClosed(string status) =>
        string.Equals(status, WalletStatusFrozen, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, WalletStatusClosed, StringComparison.OrdinalIgnoreCase);

    private static string HashPin(string pin) => BCryptNet.HashPassword(pin);

    private static bool VerifyPin(string inputPin, string pinHash) => BCryptNet.Verify(inputPin, pinHash);

    private static string GenerateTopUpToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    private static string BuildTopUpVerifyKey(int accountId, string topUpToken)
        => $"{TopUpVerifyPrefix}{accountId}:{topUpToken}";

    private static string BuildTopUpAttemptKey(string attemptCode)
        => $"{TopUpAttemptPrefix}{attemptCode.ToUpperInvariant()}";

    private static string BuildTopUpAttemptCode(int accountId)
    {
        _ = accountId;
        var bytes = RandomNumberGenerator.GetBytes(6);
        var uid = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"WLT{uid}";
    }

    private string BuildVietQrUrl(string attemptCode, long amount)
    {
        var p = new System.Collections.Specialized.NameValueCollection
        {
            ["acc"] = _sePayOptions.AccountNumber,
            ["bank"] = _sePayOptions.BankCode,
            ["amount"] = amount.ToString(),
            ["des"] = attemptCode
        };

        var qs = string.Join("&", p.AllKeys.Select(k => $"{k}={Uri.EscapeDataString(p[k]!)}"));
        return $"https://qr.sepay.vn/img?{qs}";
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private sealed class WalletTopUpAttemptCache
    {
        public string AttemptCode { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public int WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "PENDING";
        public int? WalletTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
