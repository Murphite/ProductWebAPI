using System.ComponentModel.DataAnnotations;

namespace Products.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 signing key; must be at least 256 bits. Supply via secrets/env vars, never source control.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(1);
}
