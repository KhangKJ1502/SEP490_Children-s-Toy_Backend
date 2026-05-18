using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Wallets;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IWalletService
{
    Task<Result<WalletDto>> GetMyWalletAsync(CancellationToken cancellationToken = default);

    Task<Result<PaginatedResponse<WalletTransactionDto>>> GetWalletTransactionsAsync(
        WalletTransactionQueryDto query,
        CancellationToken cancellationToken = default);

    Task<Result<WalletDto>> CreateWalletWithPinAsync(
        CreateWalletRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<VerifyWalletPinResponseDto>> VerifyWalletPinAsync(
        VerifyWalletPinRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<SePayTopUpQrResponseDto>> CreateSePayTopUpQrAsync(
        CreateSePayTopUpQrRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<SePayTopUpStatusResponseDto>> GetSePayTopUpStatusAsync(
        string attemptCode,
        CancellationToken cancellationToken = default);

    Task<Result> ChangeWalletPinAsync(
        ChangeWalletPinRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> SendForgotWalletPinOtpAsync(CancellationToken cancellationToken = default);

    Task<Result> VerifyForgotWalletPinOtpAsync(
        VerifyForgotWalletPinOtpRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> ResetForgotWalletPinAsync(
        ResetForgotWalletPinRequestDto dto,
        CancellationToken cancellationToken = default);
}
