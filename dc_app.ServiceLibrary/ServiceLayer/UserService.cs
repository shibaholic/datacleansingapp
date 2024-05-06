using dc_app.ServiceLibrary.RepositoryLayer;
using Microsoft.AspNetCore.Identity;

namespace dc_app.ServiceLibrary.ServiceLayer;

public class UserService : IUserStore<IdentityUser>
{
    private readonly IUserRepository _usersRepo;

    public UserService(IUserRepository usersRepo)
    {
        _usersRepo = usersRepo;
    }

    #region createuser
    public async Task<IdentityResult> CreateAsync(IdentityUser user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user == null) throw new ArgumentNullException(nameof(user));

        return await _usersRepo.CreateAsync(user);
    }
    #endregion

    public async Task<IdentityResult> DeleteAsync(IdentityUser user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user == null) throw new ArgumentNullException(nameof(user));

        return await _usersRepo.DeleteAsync(user);

    }

    public void Dispose()
    {
    }

    public async Task<IdentityUser> FindByIdAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userId == null) throw new ArgumentNullException(nameof(userId));
        Guid idGuid;
        if (!Guid.TryParse(userId, out idGuid))
        {
            throw new ArgumentException("Not a valid Guid id", nameof(userId));
        }

        return await _usersRepo.FindByIdAsync(idGuid);

    }

    public async Task<IdentityUser> FindByNameAsync(string userName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userName == null) throw new ArgumentNullException(nameof(userName));

        return await _usersRepo.FindByNameAsync(userName);
    }

    public Task<string> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user == null) throw new ArgumentNullException(nameof(user));

        return Task.FromResult(user.PasswordHash);
    }

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user == null) throw new ArgumentNullException(nameof(user));

        return Task.FromResult(user.Id.ToString());
    }

    public Task<string> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user == null) throw new ArgumentNullException(nameof(user));

        return Task.FromResult(user.UserName);
    }

    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SetNormalizedUserNameAsync(IdentityUser user, string normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (normalizedName == null) throw new ArgumentNullException(nameof(normalizedName));

        user.NormalizedUserName = normalizedName;
        return Task.FromResult<object>(null);
    }

    public Task SetPasswordHashAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (passwordHash == null) throw new ArgumentNullException(nameof(passwordHash));

        user.PasswordHash = passwordHash;
        return Task.FromResult<object>(null);

    }

    public Task SetUserNameAsync(IdentityUser user, string userName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}