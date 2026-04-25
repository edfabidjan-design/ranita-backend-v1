using EcommerceApi.Models;
using Microsoft.AspNetCore.Identity;

public static class PasswordHasherUtil
{
    private static readonly PasswordHasher<User> _hasher = new();

    public static string Hash(User u, string pwd) => _hasher.HashPassword(u, pwd);

    public static bool Verify(User u, string hash, string pwd)
        => _hasher.VerifyHashedPassword(u, hash, pwd) == PasswordVerificationResult.Success;
}
