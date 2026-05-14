using System.Security.Cryptography;
using Core.Application.Identity.Common.Interfaces;
using Core.Infrastructure.Identity.DTOs;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.Identity.Services
{
    public class HasherService : IHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int HashPartsCount = 3;
        private readonly HashingOptions _options;
        public HasherService(IOptions<HashingOptions> options)
        {
            _options = options.Value;
        }
        public (bool Verified, bool NeedsUpgrade) Check(string hash, string password)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return (false, false);
            }
            var parts = hash.Split(".", HashPartsCount);
            if (parts.Length != HashPartsCount)
            {
                return (false, false);
            }
            try
            {
                var iterations = Convert.ToInt32(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var key = Convert.FromBase64String(parts[2]);
                var needsUpgrade = iterations != _options.Iterations;
                using var algorithm = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
                var keyToCheck = algorithm.GetBytes(KeySize);
                var verified = keyToCheck.SequenceEqual(key);
                return (verified, needsUpgrade);
            }
            catch (Exception)
            {
                return (false, false);
            }
        }
        public string Hash(string password)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(password))
            {
                return string.Empty;
            }
            using var algorithm = new Rfc2898DeriveBytes(password, SaltSize, _options.Iterations, HashAlgorithmName.SHA256);
            var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
            var salt = Convert.ToBase64String(algorithm.Salt);
            return $"{_options.Iterations}.{salt}.{key}";
        }
    }
}