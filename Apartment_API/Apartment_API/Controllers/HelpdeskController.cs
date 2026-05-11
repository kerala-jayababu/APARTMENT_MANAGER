using Asp.Versioning;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Apartment_API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ApiAccess)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/helpdesk")]
public sealed class HelpdeskController(
    IHelpdeskService helpdesk,
    ICurrentUser currentUser,
    ILogger<HelpdeskController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponseDto<HelpDeskCategoryListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<HelpDeskCategoryListDto>>> GetCategories(
        [FromQuery] string? q,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<HelpDeskCategoryListDto>();
        try
        {
            var data = await helpdesk.ListCategoriesAsync(apartmentId, q, activeOnly, cancellationToken);
            return Ok(new ApiResponseDto<HelpDeskCategoryListDto>
                { Success = true, Message = "Categories loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Helpdesk categories list.");
            return this.ApiServerError<HelpDeskCategoryListDto>(environment, configuration, ex);
        }
    }

    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<HelpDeskCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<HelpDeskCategoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDto<HelpDeskCategoryDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<HelpDeskCategoryDto>>> GetCategory(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<HelpDeskCategoryDto>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<HelpDeskCategoryDto>();
        try
        {
            var data = await helpdesk.GetCategoryAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<HelpDeskCategoryDto>
                    { Success = false, Message = "Category not found.", Errors = ["NOT_FOUND"] });
            return Ok(new ApiResponseDto<HelpDeskCategoryDto> { Success = true, Message = "Category loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Helpdesk category {Id}.", id);
            return this.ApiServerError<HelpDeskCategoryDto>(environment, configuration, ex);
        }
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> CreateCategory(
        [FromBody] CreateHelpDeskCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IdResultDto>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<IdResultDto>();
        try
        {
            var id = await helpdesk.CreateCategoryAsync(apartmentId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto>
                    { Success = true, Message = "Category created.", Data = new IdResultDto { Id = id } });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("CONFLICT:", StringComparison.Ordinal))
        {
            return Conflict(new ApiResponseDto<IdResultDto>
                { Success = false, Message = ex.Message, Errors = ["CONFLICT"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create helpdesk category.");
            return this.ApiServerError<IdResultDto>(environment, configuration, ex);
        }
    }

    [HttpPut("categories/{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponseDto<object?>>> UpdateCategory(
        [FromRoute] int id,
        [FromBody] UpdateHelpDeskCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<object?>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<object?>();
        try
        {
            await helpdesk.UpdateCategoryAsync(apartmentId, id, request, cancellationToken);
            return Ok(new ApiResponseDto<object?> { Success = true, Message = "Category updated.", Data = null });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("CONFLICT:", StringComparison.Ordinal))
        {
            return Conflict(new ApiResponseDto<object?> { Success = false, Message = ex.Message, Errors = ["CONFLICT"] });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new ApiResponseDto<object?> { Success = false, Message = ex.Message, Errors = ["NOT_FOUND"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<object?> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update helpdesk category {Id}.", id);
            return this.ApiServerError<object?>(environment, configuration, ex);
        }
    }

    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponseDto<object?>>> DeleteCategory(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<object?>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<object?>();
        try
        {
            await helpdesk.DeleteCategoryAsync(apartmentId, id, cancellationToken);
            return Ok(new ApiResponseDto<object?> { Success = true, Message = "Category deleted.", Data = null });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("CONFLICT:", StringComparison.Ordinal))
        {
            return Conflict(new ApiResponseDto<object?> { Success = false, Message = ex.Message, Errors = ["CONFLICT"] });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new ApiResponseDto<object?> { Success = false, Message = ex.Message, Errors = ["NOT_FOUND"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete helpdesk category {Id}.", id);
            return this.ApiServerError<object?>(environment, configuration, ex);
        }
    }

    [HttpPost("complaints")]
    [ProducesResponseType(typeof(ApiResponseDto<LogComplaintResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<LogComplaintResponseDto>>> LogComplaint(
        [FromBody] LogComplaintRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<LogComplaintResponseDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<LogComplaintResponseDto>();
        var isAdmin = await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await helpdesk.LogComplaintAsync(apartmentId, userId, request, isAdmin, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<LogComplaintResponseDto> { Success = true, Message = "Complaint logged.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<LogComplaintResponseDto>
                { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Log complaint.");
            return this.ApiServerError<LogComplaintResponseDto>(environment, configuration, ex);
        }
    }

    [HttpGet("complaints")]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<ComplaintListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<ComplaintListItemDto>>>> ListComplaints(
        [FromQuery] int? categoryId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? unitId,
        [FromQuery] int? ownerTenantId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<PagedResult<ComplaintListItemDto>>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<PagedResult<ComplaintListItemDto>>();
        var isAdmin = await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await helpdesk.ListComplaintsAsync(
                apartmentId, userId, isAdmin, categoryId, status, priority, unitId, ownerTenantId,
                from, to, q, page, pageSize, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<ComplaintListItemDto>>
                { Success = true, Message = "Complaints loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "List complaints.");
            return this.ApiServerError<PagedResult<ComplaintListItemDto>>(environment, configuration, ex);
        }
    }

    [HttpGet("complaints/{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<ComplaintDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<ComplaintDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<ComplaintDetailDto>>> GetComplaint(
        [FromRoute] int id,
        [FromQuery] bool includeHistory = true,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<ComplaintDetailDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<ComplaintDetailDto>();
        var isAdmin = await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await helpdesk.GetComplaintAsync(apartmentId, userId, isAdmin, id, includeHistory, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<ComplaintDetailDto>
                    { Success = false, Message = "Complaint not found.", Errors = ["NOT_FOUND"] });
            return Ok(new ApiResponseDto<ComplaintDetailDto> { Success = true, Message = "Complaint loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get complaint {Id}.", id);
            return this.ApiServerError<ComplaintDetailDto>(environment, configuration, ex);
        }
    }

    [HttpPost("complaints/{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponseDto<AppendComplaintStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<AppendComplaintStatusResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<AppendComplaintStatusResponseDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponseDto<AppendComplaintStatusResponseDto>>> AppendStatus(
        [FromRoute] int id,
        [FromBody] AppendComplaintStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<AppendComplaintStatusResponseDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<AppendComplaintStatusResponseDto>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<AppendComplaintStatusResponseDto>();
        try
        {
            var data = await helpdesk.AppendStatusAsync(apartmentId, userId, id, request, cancellationToken);
            return Ok(new ApiResponseDto<AppendComplaintStatusResponseDto>
                { Success = true, Message = "Status updated.", Data = data });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("CONFLICT:", StringComparison.Ordinal))
        {
            return Conflict(new ApiResponseDto<AppendComplaintStatusResponseDto>
                { Success = false, Message = ex.Message, Errors = ["CONFLICT"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<AppendComplaintStatusResponseDto>
                { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Append complaint status {Id}.", id);
            return this.ApiServerError<AppendComplaintStatusResponseDto>(environment, configuration, ex);
        }
    }

    [HttpGet("complaints/{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<HelpdeskStatusEntryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<HelpdeskStatusEntryDto>>>> GetStatusTimeline(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IReadOnlyList<HelpdeskStatusEntryDto>>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<IReadOnlyList<HelpdeskStatusEntryDto>>();
        var isAdmin = await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await helpdesk.GetStatusTimelineAsync(apartmentId, userId, isAdmin, id, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<HelpdeskStatusEntryDto>>
                { Success = true, Message = "Status timeline loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Complaint status timeline {Id}.", id);
            return this.ApiServerError<IReadOnlyList<HelpdeskStatusEntryDto>>(environment, configuration, ex);
        }
    }

    [HttpPost("files")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<HelpdeskFileUploadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<HelpdeskFileUploadDto>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiResponseDto<HelpdeskFileUploadDto>), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<ApiResponseDto<HelpdeskFileUploadDto>>> UploadFile(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<HelpdeskFileUploadDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<HelpdeskFileUploadDto>();
        try
        {
            var data = await helpdesk.UploadFileAsync(apartmentId, userId, file, cancellationToken);
            return Ok(new ApiResponseDto<HelpdeskFileUploadDto> { Success = true, Message = "File uploaded.", Data = data });
        }
        catch (InvalidOperationException ex) when (ex.Message == "FILE_TOO_LARGE")
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new ApiResponseDto<HelpdeskFileUploadDto>
                    { Success = false, Message = "File exceeds 5 MB.", Errors = ["FILE_TOO_LARGE"] });
        }
        catch (InvalidOperationException ex) when (ex.Message == "FILE_TYPE_NOT_ALLOWED")
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                new ApiResponseDto<HelpdeskFileUploadDto>
                    { Success = false, Message = "Only JPG, PNG, or PDF are allowed.", Errors = ["FILE_TYPE_NOT_ALLOWED"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<HelpdeskFileUploadDto>
                { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Helpdesk file upload.");
            return this.ApiServerError<HelpdeskFileUploadDto>(environment, configuration, ex);
        }
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponseDto<HelpdeskStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<HelpdeskStatsDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<HelpdeskStatsDto>>> GetStats(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<HelpdeskStatsDto>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<HelpdeskStatsDto>();
        try
        {
            var data = await helpdesk.GetStatsAsync(apartmentId, month, year, cancellationToken);
            return Ok(new ApiResponseDto<HelpdeskStatsDto> { Success = true, Message = "Stats loaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<HelpdeskStatsDto>
                { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Helpdesk stats.");
            return this.ApiServerError<HelpdeskStatsDto>(environment, configuration, ex);
        }
    }

    [HttpGet("reports/status-summary")]
    [ProducesResponseType(typeof(ApiResponseDto<StatusSummaryReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<StatusSummaryReportDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<StatusSummaryReportDto>>> StatusSummaryReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int? categoryId,
        [FromQuery] string? format,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<StatusSummaryReportDto>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<StatusSummaryReportDto>();
        try
        {
            var data = await helpdesk.GetStatusSummaryReportAsync(apartmentId, from, to, categoryId, format, cancellationToken);
            return Ok(new ApiResponseDto<StatusSummaryReportDto> { Success = true, Message = "Report loaded.", Data = data });
        }
        catch (InvalidOperationException ex) when (ex.Message == "EXPORT_FORMAT_NOT_IMPLEMENTED")
        {
            return BadRequest(new ApiResponseDto<StatusSummaryReportDto>
                { Success = false, Message = "PDF and Excel export are not implemented yet.", Errors = ["NOT_IMPLEMENTED"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<StatusSummaryReportDto>
                { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Helpdesk status summary report.");
            return this.ApiServerError<StatusSummaryReportDto>(environment, configuration, ex);
        }
    }

    [HttpGet("reports/category-breakdown")]
    [ProducesResponseType(typeof(ApiResponseDto<CategoryBreakdownReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<CategoryBreakdownReportDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<CategoryBreakdownReportDto>>> CategoryBreakdownReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int? categoryId,
        [FromQuery] string? format,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<CategoryBreakdownReportDto>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<CategoryBreakdownReportDto>();
        try
        {
            var data = await helpdesk.GetCategoryBreakdownReportAsync(apartmentId, from, to, categoryId, format, cancellationToken);
            return Ok(new ApiResponseDto<CategoryBreakdownReportDto> { Success = true, Message = "Report loaded.", Data = data });
        }
        catch (InvalidOperationException ex) when (ex.Message == "EXPORT_FORMAT_NOT_IMPLEMENTED")
        {
            return BadRequest(new ApiResponseDto<CategoryBreakdownReportDto>
                { Success = false, Message = "PDF and Excel export are not implemented yet.", Errors = ["NOT_IMPLEMENTED"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<CategoryBreakdownReportDto>
                { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Helpdesk category breakdown report.");
            return this.ApiServerError<CategoryBreakdownReportDto>(environment, configuration, ex);
        }
    }

    [HttpGet("reports/aging")]
    [ProducesResponseType(typeof(ApiResponseDto<AgingReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<AgingReportDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponseDto<AgingReportDto>>> AgingReport(
        [FromQuery] DateOnly? asOf,
        [FromQuery] string? buckets,
        [FromQuery] string? format,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<AgingReportDto>();
        if (!await ResolveIsAdminAsync(cancellationToken).ConfigureAwait(false))
            return ForbiddenRole<AgingReportDto>();
        try
        {
            var data = await helpdesk.GetAgingReportAsync(apartmentId, asOf, buckets, format, cancellationToken);
            return Ok(new ApiResponseDto<AgingReportDto> { Success = true, Message = "Report loaded.", Data = data });
        }
        catch (InvalidOperationException ex) when (ex.Message == "EXPORT_FORMAT_NOT_IMPLEMENTED")
        {
            return BadRequest(new ApiResponseDto<AgingReportDto>
                { Success = false, Message = "PDF and Excel export are not implemented yet.", Errors = ["NOT_IMPLEMENTED"] });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<AgingReportDto>
                { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Helpdesk aging report.");
            return this.ApiServerError<AgingReportDto>(environment, configuration, ex);
        }
    }

    private async Task<bool> ResolveIsAdminAsync(CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin) return true;
        return await helpdesk.IsHelpdeskAdminAsync(currentUser.ApartmentUserRoleId, cancellationToken).ConfigureAwait(false);
    }

    private ActionResult<ApiResponseDto<T>> Forbidden<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T>
            {
                Success = false,
                Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                Errors = ["NO_APARTMENT_CONTEXT"]
            });

    private ActionResult<ApiResponseDto<T>> ForbiddenRole<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T> { Success = false, Message = "This action requires apartment administrator access.", Errors = ["FORBIDDEN"] });

}
