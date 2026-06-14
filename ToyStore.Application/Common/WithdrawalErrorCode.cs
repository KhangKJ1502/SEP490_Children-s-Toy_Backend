namespace ToyStore.Application.Common;

public enum WithdrawalErrorCode
{
    Success = 0,
    WalletNotFound = 1,
    WalletFrozen = 2,
    WalletNotActive = 3,
    WalletHasNoPin = 4,
    InsufficientAvailable = 5,
    AmountBelowMinimum = 6,
    AmountAboveMaximum = 7,
    DailyAmountExceeded = 8,
    DailyCountExceeded = 9,
    BankAccountNotFound = 10,
    DuplicateReference = 11,
    InvalidStatus = 12,
    WithdrawalInProgress = 13,
    SystemError = 99,
}

public static class WithdrawalErrorMessages
{
    public static string For(WithdrawalErrorCode code) => code switch
    {
        WithdrawalErrorCode.WalletNotFound      => "Wallet not found.",
        WithdrawalErrorCode.WalletFrozen        => "Wallet is frozen. Withdrawals are not allowed.",
        WithdrawalErrorCode.WalletNotActive     => "Wallet is not active.",
        WithdrawalErrorCode.WalletHasNoPin      => "Wallet PIN is not set. Please set your PIN first.",
        WithdrawalErrorCode.InsufficientAvailable => "Insufficient available balance for this transaction.",
        WithdrawalErrorCode.AmountBelowMinimum  => "Minimum withdrawal amount is 10,000 VND.",
        WithdrawalErrorCode.AmountAboveMaximum  => "Withdrawal amount exceeds the per-transaction limit.",
        WithdrawalErrorCode.DailyAmountExceeded => "Daily withdrawal amount limit has been reached.",
        WithdrawalErrorCode.DailyCountExceeded  => "Maximum number of withdrawals for today has been reached.",
        WithdrawalErrorCode.BankAccountNotFound => "Bank account not found.",
        WithdrawalErrorCode.DuplicateReference  => "Withdrawal request already exists. Please do not resubmit.",
        WithdrawalErrorCode.InvalidStatus       => "Withdrawal status is invalid for this operation.",
        WithdrawalErrorCode.WithdrawalInProgress => "You already have a withdrawal in progress. Please wait for it to complete before making a new request.",
        WithdrawalErrorCode.SystemError         => "System error. Please try again later.",
        _                                       => "An unknown error occurred.",
    };
}
