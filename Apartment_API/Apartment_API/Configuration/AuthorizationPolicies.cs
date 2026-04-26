namespace Apartment_API.Configuration;

/// <summary>Authorization policy names. Selection token: choose apartment. API access: tenant or SuperAdmin access token.</summary>
public static class AuthorizationPolicies
{
    /// <summary>JWT with <c>token_purpose = apartment_select</c> (after password/OTP, before tenant is chosen).</summary>
    public const string ApartmentSelect = "ApartmentSelect";

    /// <summary>Tenant access token (has <c>apartment_id</c>) or SuperAdmin access token.</summary>
    public const string ApiAccess = "ApiAccess";
}
