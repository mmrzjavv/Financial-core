namespace Core.Application.Identity.Common.Interfaces
{
    public interface IHasher
    {
        string Hash(string password);
        (bool Verified, bool NeedsUpgrade) Check(string hash, string password);
    }
}