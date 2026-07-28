namespace CustomerAPI.Domain.Sagas;

/// <summary>
/// Shared data object carried through all steps of the OnboardCustomerSaga.
/// Steps read from and write back to this object to communicate state.
/// </summary>
public class OnboardCustomerData
{
    // --- input ---
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Set to true in the request to trigger a simulated gift-service failure
    /// and demonstrate the compensation path.
    /// </summary>
    public bool SimulateGiftFailure { get; set; }

    // --- set by steps during execution ---
    public Guid? CustomerId { get; set; }
    public string? WelcomeGiftCode { get; set; }
    public bool WelcomeEmailSent { get; set; }
}
