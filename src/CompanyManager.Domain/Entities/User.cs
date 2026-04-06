namespace CompanyManager.Domain.Entities;

public class User
{
    private const int MaxFailedAttempts = 5;

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public bool IsBlocked { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public bool HasTechnicianAccess { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Required by EF Core
    private User() { }

    public User(Guid id, string email, string passwordHash, string role = "user")
    {
        Id = id;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedAttempts)
            IsBlocked = true;
    }

    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
    }

    public void Unblock()
    {
        IsBlocked = false;
        FailedLoginAttempts = 0;
    }

    public void UpdateProfile(string? role, string? passwordHash)
    {
        if (role is not null) Role = role;
        if (passwordHash is not null) PasswordHash = passwordHash;
    }

    public void SetTechnicianAccess(bool value) => HasTechnicianAccess = value;

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
}
