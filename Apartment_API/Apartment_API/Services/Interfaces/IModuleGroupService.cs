using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IModuleGroupService
{
    Task<IReadOnlyList<ModuleGroupListItemDto>> ListModuleGroupsAsync(
        bool? isActive,
        CancellationToken cancellationToken = default);
}
