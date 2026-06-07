using System.ComponentModel.DataAnnotations;

public class PaymentOptions
{
    public const string SectionName = "Payments";

    [Required(ErrorMessage = "The Payment Gateway URL is strictly mandatory.")]
    [Url(ErrorMessage = "The Gateway URL format must be a valid fully-qualified web address.")]
    public string GatewayUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "The Merchant API Key must be supplied.")]
    [StringLength(32, MinimumLength = 16, ErrorMessage = "The API Key must be between 16 and 32 characters long.")]
    public string ApiKey { get; set; } = string.Empty;
}