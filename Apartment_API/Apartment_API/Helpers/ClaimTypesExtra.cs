namespace Apartment_API.Helpers;

public static class ClaimTypesExtra
{
    /// <summary>Custom JWT claim for phone (must match token creation).</summary>
    public const string Phone = "phone";

    /// <summary>Values: <see cref="TokenPurposeValues.ApartmentSelect" />, <see cref="TokenPurposeValues.ApiAccess" />.</summary>
    public const string TokenPurpose = "token_purpose";

    public const string ApartmentId = "apartment_id";
    public const string ApartmentName = "apartment_name";
    public const string ApartmentUserRoleId = "apartment_user_role_id";
}

public static class TokenPurposeValues
{
    public const string ApartmentSelect = "apartment_select";
    public const string ApiAccess = "api_access";
}
