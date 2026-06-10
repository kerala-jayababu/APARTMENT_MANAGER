namespace Apartment_API.Configuration;

public sealed class ModulePermissionDeniedException(string message) : Exception(message);
