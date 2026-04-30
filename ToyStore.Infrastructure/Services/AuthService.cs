using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;

    public AuthService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IRedisService redisService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IMapper mapper,
        IValidator<LoginDto> loginValidator,
        IValidator<SendRegisterOtpDto> sendRegisterOtpValidator,
        IValidator<RegisterDto> registerValidator,
        IValidator<ForgotPasswordDto> forgotPasswordValidator,
        IValidator<ResetPasswordDto> resetPasswordValidator)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _redisService = redisService;
        _configuration = configuration;
        _logger = logger;
        _mapper = mapper;
        _loginValidator = loginValidator;
        _sendRegisterOtpValidator = sendRegisterOtpValidator;
        _registerValidator = registerValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
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
        var account = await _unitOfWork.Accounts.GetByEmailForAuthAsync(normalizedEmail, cancellationToken);

        if (account == null || account.IsDeleted)
        {
            return Result<AuthResponseDto>.Failure("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        if (!VerifyPassword(dto.Password, account.PasswordHash))
        {
            return Result<AuthResponseDto>.Failure("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        if (!account.IsActive)
        {
            return Result<AuthResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account has been deactivated. Please contact support.");
        }

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
                cancellationToken);

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
            return Result.Success();
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
        await _redisService.SetAsync(blacklistKey, "blacklisted", remainingTime);

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
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
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
}
