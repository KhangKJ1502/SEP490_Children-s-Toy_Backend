using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using FluentValidation;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Auth;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const byte CustomerRoleId = 1;
    private const string OtpRegisterPrefix = "auth:otp:register:";
    private const string PendingRegisterPrefix = "auth:pending-register:";
    private const string OtpForgotPrefix = "auth:otp:forgot:";
    private const string TokenBlacklistPrefix = "auth:blacklist:";
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IRedisService _redisService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<SendRegisterOtpDto> _sendRegisterOtpValidator;
    private readonly IValidator<RequestRegisterOtpDto> _requestRegisterOtpValidator;
    private readonly IValidator<VerifyRegisterOtpDto> _verifyRegisterOtpValidator;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
    private readonly IValidator<GoogleLoginDto> _googleLoginValidator;
    private readonly IValidator<GoogleRegisterDto> _googleRegisterValidator;
    private readonly ITimeProvider _timeProvider;
    private readonly ILoginAttemptService _loginAttemptService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IRedisService redisService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IMapper mapper,
        IValidator<LoginDto> loginValidator,
        IValidator<SendRegisterOtpDto> sendRegisterOtpValidator,
        IValidator<RequestRegisterOtpDto> requestRegisterOtpValidator,
        IValidator<VerifyRegisterOtpDto> verifyRegisterOtpValidator,
        IValidator<RegisterDto> registerValidator,
        IValidator<ForgotPasswordDto> forgotPasswordValidator,
        IValidator<ResetPasswordDto> resetPasswordValidator,
        IValidator<GoogleLoginDto> googleLoginValidator,
        IValidator<GoogleRegisterDto> googleRegisterValidator,
        ITimeProvider timeProvider,
        ILoginAttemptService loginAttemptService)
    {
        _unitOfWork               = unitOfWork;
        _emailService             = emailService;
        _redisService             = redisService;
        _configuration            = configuration;
        _logger                   = logger;
        _mapper                   = mapper;
        _loginValidator           = loginValidator;
        _sendRegisterOtpValidator = sendRegisterOtpValidator;
        _requestRegisterOtpValidator = requestRegisterOtpValidator;
        _verifyRegisterOtpValidator = verifyRegisterOtpValidator;
        _registerValidator        = registerValidator;
        _forgotPasswordValidator  = forgotPasswordValidator;
        _resetPasswordValidator   = resetPasswordValidator;
        _googleLoginValidator     = googleLoginValidator;
        _googleRegisterValidator  = googleRegisterValidator;
        _timeProvider             = timeProvider;
        _loginAttemptService      = loginAttemptService;
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<AuthResponseDto>.ValidationFailure(errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        // Kiểm tra xem tài khoản có bị khóa do nhập sai quá nhiều lần không
        if (_loginAttemptService.IsLocked(normalizedEmail))
        {
            var remainingSeconds = _loginAttemptService.GetRemainingLockTimeInSeconds(normalizedEmail);
            var remainingMinutes = Math.Ceiling(remainingSeconds / 60.0);
            return Result<AuthResponseDto>.Failure(
                "ACCOUNT_LOCKED",
                $"Too many failed login attempts. Your account is locked for {remainingMinutes} minute(s). Please try again later.");
        }

        var account = await _unitOfWork.Accounts.GetByEmailForAuthAsync(normalizedEmail, cancellationToken);

        if (account == null || account.IsDeleted)
        {
            return Result<AuthResponseDto>.Failure("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        if (!VerifyPassword(dto.Password, account.PasswordHash))
        {
            // Ghi nhận lần thất bại và lấy số lần còn lại
            var remainingAttempts = _loginAttemptService.RecordFailedAttempt(normalizedEmail);
            
            if (remainingAttempts == 0)
            {
                _logger.LogWarning("Account {Email} has been locked due to too many failed login attempts.", normalizedEmail);
                return Result<AuthResponseDto>.Failure(
                    "ACCOUNT_LOCKED",
                    "Too many failed login attempts. Your account has been locked for 5 minutes.");
            }

            _logger.LogWarning("Failed login attempt for {Email}. Remaining attempts: {Remaining}", normalizedEmail, remainingAttempts);
            return Result<AuthResponseDto>.Failure(
                "INVALID_CREDENTIALS",
                $"Invalid email or password. You have {remainingAttempts} attempt(s) remaining before your account is locked.");
        }

        if (!account.IsActive)
        {
            return Result<AuthResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account has been locked for violating our system policy. Please contact support.");
        }

        if (dto.RoleId.HasValue && account.RoleId != dto.RoleId.Value)
        {
            var message = dto.RoleId.Value == CustomerRoleId 
                ? "Administrator accounts cannot be used to log into the customer portal."
                : (account.RoleId == CustomerRoleId 
                    ? "You do not have permission to access the admin portal with a customer account."
                    : "You do not have the required permissions to access this portal.");
            return Result<AuthResponseDto>.Failure("ROLE_MISMATCH", message);
        }

        if (dto.AllowedRoleIds is { Count: > 0 } && !dto.AllowedRoleIds.Contains(account.RoleId))
        {
            var isCustomerRequired = dto.AllowedRoleIds.Count == 1 && dto.AllowedRoleIds.Contains(CustomerRoleId);
            var message = isCustomerRequired 
                ? "Administrator accounts cannot be used to log into the customer portal."
                : (account.RoleId == CustomerRoleId 
                    ? "You do not have permission to access the admin portal with a customer account."
                    : "You do not have the required permissions to access this portal.");
            return Result<AuthResponseDto>.Failure("ROLE_MISMATCH", message);
        }

        // Đăng nhập thành công → reset các lần thất bại
        _loginAttemptService.ResetAttempts(normalizedEmail);

        var token = GenerateJwtToken(account);
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

        _logger.LogInformation("Account {AccountId} logged in successfully.", account.AccountId);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expirationMinutes * 60,
            Account = _mapper.Map<AccountInfoDto>(account)
        });
    }

    public async Task<Result> SendRegisterOtpAsync(SendRegisterOtpDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _sendRegisterOtpValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var emailExists = await _unitOfWork.Accounts.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (emailExists)
        {
            return Result.Conflict("Email already registered. Please use a different email.");
        }

        var otpCode = GenerateOtpCode();
        var redisKey = $"{OtpRegisterPrefix}{normalizedEmail}";
        await _redisService.SetAsync(redisKey, otpCode, OtpExpiry);

        await _emailService.SendRegisterOtpEmailAsync(normalizedEmail, otpCode, cancellationToken);

        _logger.LogInformation("Register OTP sent to {Email}.", normalizedEmail);
        return Result.Success();
    }

    public async Task<Result> RequestRegisterOtpAsync(RequestRegisterOtpDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _requestRegisterOtpValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var emailExists = await _unitOfWork.Accounts.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (emailExists)
        {
            return Result.Conflict("Email already registered. Please use a different email.");
        }

        var otpCode = GenerateOtpCode();
        var pendingRegistration = new PendingRegisterCache(
            AccountName: dto.AccountName.Trim(),
            Email: normalizedEmail,
            PasswordHash: HashPassword(dto.Password.Trim()),
            OtpCode: otpCode);

        var redisKey = $"{PendingRegisterPrefix}{normalizedEmail}";
        var pendingJson = JsonSerializer.Serialize(pendingRegistration);
        await _redisService.SetAsync(redisKey, pendingJson, OtpExpiry);

        await _emailService.SendRegisterOtpEmailAsync(normalizedEmail, otpCode, cancellationToken);

        _logger.LogInformation("Pending registration OTP sent to {Email}.", normalizedEmail);
        return Result.Success();
    }

    public async Task<Result> ResendRegisterOtpAsync(SendRegisterOtpDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _sendRegisterOtpValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var redisKey = $"{PendingRegisterPrefix}{normalizedEmail}";
        var pendingJson = await _redisService.GetAsync(redisKey);
        if (string.IsNullOrWhiteSpace(pendingJson))
        {
            return Result.Failure("OTP_EXPIRED", "OTP code has expired or was not requested. Please register again.");
        }

        PendingRegisterCache? pendingRegistration;
        try
        {
            pendingRegistration = JsonSerializer.Deserialize<PendingRegisterCache>(pendingJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid pending registration cache for {Email}.", normalizedEmail);
            await _redisService.DeleteAsync(redisKey);
            return Result.Failure("OTP_EXPIRED", "OTP code has expired or was not requested. Please register again.");
        }

        if (pendingRegistration == null || !string.Equals(pendingRegistration.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            await _redisService.DeleteAsync(redisKey);
            return Result.Failure("OTP_EXPIRED", "OTP code has expired or was not requested. Please register again.");
        }

        var emailExists = await _unitOfWork.Accounts.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (emailExists)
        {
            await _redisService.DeleteAsync(redisKey);
            return Result.Conflict("Email already registered. Please use a different email.");
        }

        var otpCode = GenerateOtpCode();
        var refreshedPendingRegistration = pendingRegistration with { OtpCode = otpCode };
        var refreshedJson = JsonSerializer.Serialize(refreshedPendingRegistration);
        await _redisService.SetAsync(redisKey, refreshedJson, OtpExpiry);

        await _emailService.SendRegisterOtpEmailAsync(normalizedEmail, otpCode, cancellationToken);

        _logger.LogInformation("Pending registration OTP resent to {Email}.", normalizedEmail);
        return Result.Success();
    }

    public async Task<Result<AuthResponseDto>> VerifyRegisterOtpAsync(VerifyRegisterOtpDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _verifyRegisterOtpValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<AuthResponseDto>.ValidationFailure(errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var redisKey = $"{PendingRegisterPrefix}{normalizedEmail}";
        var pendingJson = await _redisService.GetAsync(redisKey);

        if (string.IsNullOrWhiteSpace(pendingJson))
        {
            return Result<AuthResponseDto>.Failure("OTP_EXPIRED", "OTP code has expired or was not requested. Please register again.");
        }

        PendingRegisterCache? pendingRegistration;
        try
        {
            pendingRegistration = JsonSerializer.Deserialize<PendingRegisterCache>(pendingJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid pending registration cache for {Email}.", normalizedEmail);
            await _redisService.DeleteAsync(redisKey);
            return Result<AuthResponseDto>.Failure("OTP_EXPIRED", "OTP code has expired or was not requested. Please register again.");
        }

        if (pendingRegistration == null || !string.Equals(pendingRegistration.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            await _redisService.DeleteAsync(redisKey);
            return Result<AuthResponseDto>.Failure("OTP_EXPIRED", "OTP code has expired or was not requested. Please register again.");
        }

        if (!string.Equals(pendingRegistration.OtpCode, dto.OtpCode, StringComparison.Ordinal))
        {
            return Result<AuthResponseDto>.Failure("OTP_INVALID", "Invalid OTP code. Please try again.");
        }

        var emailExists = await _unitOfWork.Accounts.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (emailExists)
        {
            await _redisService.DeleteAsync(redisKey);
            return Result<AuthResponseDto>.Conflict("Email already registered.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Accounts.CreateAsync(
                CustomerRoleId,
                null,
                pendingRegistration.AccountName,
                null,
                normalizedEmail,
                pendingRegistration.PasswordHash,
                true,
                "Email",
                cancellationToken,
                hasPassword: true);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            await _redisService.DeleteAsync(redisKey);

            var token = GenerateJwtToken(created);
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

            _logger.LogInformation("Customer account {AccountId} registered successfully from pending OTP.", created.AccountId);
            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = expirationMinutes * 60,
                Account = _mapper.Map<AccountInfoDto>(created)
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to complete pending registration with email {Email}.", normalizedEmail);
            throw;
        }
    }

    public async Task<Result<AccountInfoDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<AccountInfoDto>.ValidationFailure(errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var redisKey = $"{OtpRegisterPrefix}{normalizedEmail}";
        var storedOtp = await _redisService.GetAsync(redisKey);

        if (storedOtp == null)
        {
            return Result<AccountInfoDto>.Failure("OTP_EXPIRED", "OTP code has expired or was not requested. Please request a new OTP.");
        }

        if (storedOtp != dto.OtpCode)
        {
            return Result<AccountInfoDto>.Failure("OTP_INVALID", "Invalid OTP code. Please try again.");
        }

        var emailExists = await _unitOfWork.Accounts.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (emailExists)
        {
            return Result<AccountInfoDto>.Conflict("Email already registered.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await _unitOfWork.Accounts.CreateAsync(
                CustomerRoleId,
                null,
                dto.AccountName.Trim(),
                null,
                normalizedEmail,
                HashPassword(dto.Password),
                true,
                "Email",
                cancellationToken,
                hasPassword: true);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            await _redisService.DeleteAsync(redisKey);

            _logger.LogInformation("Customer account {AccountId} registered successfully.", created.AccountId);


            return Result<AccountInfoDto>.Success(_mapper.Map<AccountInfoDto>(created));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to register account with email {Email}.", normalizedEmail);
            throw;
        }
    }

    public async Task<Result> SendForgotPasswordOtpAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _forgotPasswordValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var account = await _unitOfWork.Accounts.GetByEmailForAuthAsync(normalizedEmail, cancellationToken);

        if (account == null || account.IsDeleted)
        {
            return Result.Failure("ACCOUNT_NOT_FOUND", "No account was found with this email address.");
        }

        if (!account.IsActive)
        {
            return Result.Failure("ACCOUNT_INACTIVE", "Your account has been locked for violating our system policy. Please contact support.");
        }

        var otpCode = GenerateOtpCode();
        var redisKey = $"{OtpForgotPrefix}{normalizedEmail}";
        await _redisService.SetAsync(redisKey, otpCode, OtpExpiry);

        await _emailService.SendForgotPasswordOtpEmailAsync(normalizedEmail, otpCode, cancellationToken);

        _logger.LogInformation("Forgot password OTP sent to {Email}.", normalizedEmail);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _resetPasswordValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result.ValidationFailure(errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var redisKey = $"{OtpForgotPrefix}{normalizedEmail}";
        var storedOtp = await _redisService.GetAsync(redisKey);

        if (storedOtp == null)
        {
            return Result.Failure("OTP_EXPIRED", "OTP code has expired or was not requested. Please request a new OTP.");
        }

        if (storedOtp != dto.OtpCode)
        {
            return Result.Failure("OTP_INVALID", "Invalid OTP code. Please try again.");
        }

        var account = await _unitOfWork.Accounts.GetByEmailForAuthAsync(normalizedEmail, cancellationToken);
        if (account == null || account.IsDeleted)
        {
            return Result.NotFound("Account");
        }

        if (!account.IsActive)
        {
            return Result.Failure("ACCOUNT_INACTIVE", "Your account has been locked for violating our system policy. Please contact support.");
        }

        await _unitOfWork.Accounts.UpdatePasswordHashAsync(account.AccountId, HashPassword(dto.NewPassword), cancellationToken);
        await _redisService.DeleteAsync(redisKey);

        _logger.LogInformation("Password reset for account {AccountId}.", account.AccountId);
        return Result.Success();
    }

    public async Task<Result> LogoutAsync(string jti, TimeSpan remainingTime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return Result.Success();
        }

        var blacklistKey = $"{TokenBlacklistPrefix}{jti}";
        try
        {
            await _redisService.SetAsync(blacklistKey, "blacklisted", remainingTime);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable; skip token blacklist for {Jti}", jti);
        }

        _logger.LogInformation("Token {Jti} blacklisted.", jti);
        return Result.Success();
    }

    private string GenerateJwtToken(Account account)
    {
        var secretKey = _configuration["Jwt:SecretKey"]!;
        var issuer = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, account.Email),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.Role, account.Role.RoleName),
            new Claim("accountId", account.AccountId.ToString()),
            new Claim("roleId", account.RoleId.ToString()),
            new Claim("roleName", account.Role.RoleName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: _timeProvider.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var hash = HashPassword(password);
        return string.Equals(hash, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string HashPassword(string password)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hashBytes);
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private sealed record PendingRegisterCache(
        string AccountName,
        string Email,
        string PasswordHash,
        string OtpCode);

    /// <summary>
    /// Đăng nhập bằng Google OAuth cho account đã tồn tại trong hệ thống.
    /// Không tự động tạo account mới trong luồng login.
    /// </summary>
    public async Task<Result<AuthResponseDto>> GoogleLoginAsync(GoogleLoginDto dto, CancellationToken cancellationToken = default)
    {
        // Validate DTO
        var validationResult = await _googleLoginValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<AuthResponseDto>.ValidationFailure(errors);
        }

        // Verify Google ID Token
        var payload = await VerifyGoogleTokenAsync(dto.IdToken);
        if (payload == null)
        {
            return Result<AuthResponseDto>.Failure("INVALID_TOKEN", "Invalid Google ID token.");
        }

        // Extract thông tin từ Google payload
        var googleEmail = payload.Email.Trim().ToLowerInvariant();
        // Kiểm tra account đã tồn tại chưa
        var existingAccount = await _unitOfWork.Accounts.GetByEmailForAuthAsync(googleEmail, cancellationToken);

        // Nếu account chưa tồn tại → yêu cầu đăng ký trước
        if (existingAccount == null)
        {
            _logger.LogWarning("Google login failed: Account with email {Email} does not exist.", googleEmail);
            return Result<AuthResponseDto>.Failure("ACCOUNT_NOT_FOUND", 
                "No account found with this email. Please register first.");
        }

        // Kiểm tra account có bị xóa hoặc inactive không
        if (existingAccount.IsDeleted)
        {
            return Result<AuthResponseDto>.Failure("ACCOUNT_DELETED", "This account has been deleted.");
        }

        if (!existingAccount.IsActive)
        {
            return Result<AuthResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account has been locked for violating our system policy. Please contact support.");
        }

        var provider = existingAccount.Provider?.Trim().ToLowerInvariant();
        var providerSupportedForGoogle = string.IsNullOrWhiteSpace(provider)
            || provider is "local"
            || provider is "email"
            || provider is "google";
        if (!providerSupportedForGoogle)
        {
            return Result<AuthResponseDto>.Failure(
                "PROVIDER_NOT_SUPPORTED",
                "This account does not support Google login.");
        }

        // Nếu chỉ định RoleId, kiểm tra role có khớp không
        if (dto.RoleId.HasValue && existingAccount.RoleId != dto.RoleId.Value)
        {
            var message = dto.RoleId.Value == CustomerRoleId 
                ? "Administrator accounts cannot be used to log into the customer portal."
                : (existingAccount.RoleId == CustomerRoleId 
                    ? "You do not have permission to access the admin portal with a customer account."
                    : "You do not have the required permissions to access this portal.");
            return Result<AuthResponseDto>.Failure("ROLE_MISMATCH", message);
        }

        // Login thành công
        var token = GenerateJwtToken(existingAccount);
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

        _logger.LogInformation("Account {AccountId} logged in via Google OAuth.", existingAccount.AccountId);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expirationMinutes * 60,
            Account = _mapper.Map<AccountInfoDto>(existingAccount)
        });
    }

    /// <summary>
    /// Register account mới bằng Google OAuth (chỉ Customer).
    /// Nếu account đã tồn tại, trả lỗi.
    /// </summary>
    public async Task<Result<AuthResponseDto>> GoogleRegisterAsync(GoogleRegisterDto dto, CancellationToken cancellationToken = default)
    {
        // Validate DTO
        var validationResult = await _googleRegisterValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<AuthResponseDto>.ValidationFailure(errors);
        }

        // Verify Google ID Token
        var payload = await VerifyGoogleTokenAsync(dto.IdToken);
        if (payload == null)
        {
            return Result<AuthResponseDto>.Failure("INVALID_TOKEN", "Invalid Google ID token.");
        }

        // Extract thông tin từ Google payload
        var googleEmail = payload.Email.Trim().ToLowerInvariant();
        var googleName = payload.Name ?? "Google User";
        var googlePicture = payload.Picture;

        // Kiểm tra email đã được sử dụng chưa
        var emailExists = await _unitOfWork.Accounts.ExistsByEmailAsync(googleEmail, cancellationToken);
        if (emailExists)
        {
            return Result<AuthResponseDto>.Conflict("Email already registered. Please login instead.");
        }

        // Tạo account mới với role Customer
        var randomPassword = GenerateRandomPassword();
        var newAccount = await _unitOfWork.Accounts.CreateAsync(
            roleId: CustomerRoleId,
            employeeCode: null,
            accountName: googleName,
            phoneNumber: null,
            email: googleEmail,
            passwordHash: HashPassword(randomPassword),
            isActive: true,
            provider: "Google",
            cancellationToken: cancellationToken,
            hasPassword: false);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Load lại account với Role navigation property
        var createdAccount = await _unitOfWork.Accounts.GetByEmailForAuthAsync(googleEmail, cancellationToken);
        if (createdAccount == null)
        {
            return Result<AuthResponseDto>.Failure("ACCOUNT_CREATION_FAILED", "Failed to create account.");
        }

        var token = GenerateJwtToken(createdAccount);
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

        _logger.LogInformation("New Customer account {AccountId} registered via Google OAuth.", createdAccount.AccountId);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expirationMinutes * 60,
            Account = _mapper.Map<AccountInfoDto>(createdAccount)
        });
    }

    /// <summary>
    /// Verify Google ID Token và trả về payload nếu hợp lệ.
    /// </summary>
    private async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var googleClientId = _configuration["GoogleOAuth:ClientId"];
            if (string.IsNullOrWhiteSpace(googleClientId))
            {
                _logger.LogError("GoogleOAuth:ClientId is not configured in appsettings.json.");
                return null;
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify Google ID token.");
            return null;
        }
    }

    /// <summary>
    /// Generate random password cho Google OAuth users (16 ký tự: chữ hoa, chữ thường, số, ký tự đặc biệt).
    /// </summary>
    private static string GenerateRandomPassword()
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        const string allChars = upperCase + lowerCase + digits + specialChars;

        var random = new Random();
        var password = new char[16];

        // Đảm bảo có ít nhất 1 ký tự mỗi loại
        password[0] = upperCase[random.Next(upperCase.Length)];
        password[1] = lowerCase[random.Next(lowerCase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = specialChars[random.Next(specialChars.Length)];

        // Fill các ký tự còn lại random
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle password để không có pattern cố định
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    /// <summary>
    /// Get role name từ RoleId.
    /// </summary>
    private static string GetRoleName(byte roleId)
    {
        return roleId switch
        {
            1 => "Customer",
            2 => "Staff",
            3 => "Merchandise",
            4 => "Admin",
            5 => "Guest",
            _ => "Unknown"
        };
    }
}
