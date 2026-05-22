namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service để quản lý các lần thử đăng nhập thất bại
/// </summary>
public interface ILoginAttemptService
{
    /// <summary>
    /// Ghi nhận một lần đăng nhập thất bại
    /// </summary>
    /// <param name="email">Email của tài khoản</param>
    /// <returns>Số lần thất bại còn lại trước khi bị khóa</returns>
    int RecordFailedAttempt(string email);

    /// <summary>
    /// Reset các lần thử thất bại khi đăng nhập thành công
    /// </summary>
    /// <param name="email">Email của tài khoản</param>
    void ResetAttempts(string email);

    /// <summary>
    /// Kiểm tra xem tài khoản có đang bị khóa không
    /// </summary>
    /// <param name="email">Email của tài khoản</param>
    /// <returns>True nếu đang bị khóa, False nếu không</returns>
    bool IsLocked(string email);

    /// <summary>
    /// Lấy thời gian còn lại của khóa (tính bằng giây)
    /// </summary>
    /// <param name="email">Email của tài khoản</param>
    /// <returns>Số giây còn lại, 0 nếu không bị khóa</returns>
    int GetRemainingLockTimeInSeconds(string email);

    /// <summary>
    /// Lấy số lần thất bại hiện tại
    /// </summary>
    /// <param name="email">Email của tài khoản</param>
    /// <returns>Số lần thất bại</returns>
    int GetFailedAttempts(string email);
}
