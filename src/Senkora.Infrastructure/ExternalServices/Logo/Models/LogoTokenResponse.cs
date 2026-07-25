using Newtonsoft.Json;

namespace Senkora.Infrastructure.ExternalServices.Logo.Models;

/// <summary>
/// Logo REST /api/v1/token endpoint response model
/// Ref: Logo REST Teknik Dokumani
/// </summary>
public sealed class LogoTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonProperty("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonProperty("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonProperty("firmNo")]
    public string FirmNo { get; set; } = string.Empty;

    [JsonProperty(".issued")]
    public string Issued { get; set; } = string.Empty;

    [JsonProperty(".expires")]
    public string Expires { get; set; } = string.Empty;
}
