namespace Core.Infrastructure.Identity.Services.Hasher.DTOs;

public class HashingOptions
{
    public int Iterations { get; set; } = 10000;
}