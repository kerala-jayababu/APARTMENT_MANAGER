using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IModuleService
{
    Task<IReadOnlyList<ModuleListItemDto>> ListModulesAsync(
        bool? isActive,
        string? moduleGroup,
        string? parentModuleCode,
        CancellationToken cancellationToken = default);
}
