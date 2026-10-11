using ShelfOps.Domain.Enums;

namespace ShelfOps.Domain.Models
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string DisplayName { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }
        public DateTime? LastLoginAtUtc { get; private set; }

        private User() { }

        public User(string username, string displayName, string passwordHash, UserRole role)
        {
            Id = Guid.NewGuid();
            SetUsername(username);
            SetDisplayName(displayName);
            SetPasswordHash(passwordHash);
            Role = role;
            IsActive = true;
            CreatedAtUtc = DateTime.UtcNow;
            UpdatedAtUtc = CreatedAtUtc;
        }

        public void SetUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));
            Username = username.Trim().ToLowerInvariant();
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void SetDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            DisplayName = displayName.Trim();
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
            PasswordHash = passwordHash;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void SetRole(UserRole role)
        {
            Role = role;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void RecordLogin()
        {
            LastLoginAtUtc = DateTime.UtcNow;
        }
    }
}