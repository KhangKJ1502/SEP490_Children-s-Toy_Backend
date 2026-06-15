namespace ToyStore.Application.Validators.CustomerChildren;

public static class CustomerChildValidationRules
{
    public const int MaxChildrenPerUser = 4;
    public const int MaxChildProfileEdits = 2;
    public const int MaxChildAgeYears = 25;
    public const int MaxPastYears = 100;
    public const int FullNameMinLength = 2;
    public const int FullNameMaxLength = 100;
    public const int NickNameMaxLength = 50;
    public static readonly byte[] AllowedSexIds = [1, 2, 3];

    public static bool IsValidChildDob(DateTime dob, DateTime todayUtc, out string? errorMessage)
    {
        var today = todayUtc.Date;
        if (dob.Date > today)
        {
            errorMessage = "Date of birth must be a valid past date.";
            return false;
        }

        var ageYears = CalculateAge(dob.Date, today);
        if (ageYears > MaxPastYears)
        {
            errorMessage = "Date of birth must not be more than 100 years ago.";
            return false;
        }

        if (ageYears > MaxChildAgeYears)
        {
            errorMessage = "Child age must be 25 or younger.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static int CalculateAge(DateTime dobDate, DateTime today)
    {
        var age = today.Year - dobDate.Year;
        if (dobDate > today.AddYears(-age))
            age -= 1;
        return age;
    }

    public static bool IsAllowedSexId(byte sexId) => AllowedSexIds.Contains(sexId);
}
