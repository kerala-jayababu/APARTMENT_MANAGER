using Asp.Versioning;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apartment_API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ApiAccess)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documents")]
public sealed class DocumentsController(
    IDocumentService service,
    ICurrentUser currentUser,
    ILogger<DocumentsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<DocumentListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<DocumentListDto>>>> GetList(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] string? linkedEntityType,
        [FromQuery] int? linkedEntityId,
        [FromQuery] int? uploadedByUserId,
        [FromQuery] DateTime? uploadedFrom,
        [FromQuery] DateTime? uploadedTo,
        [FromQuery] int? expiringWithinDays,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = "uploadedAt",
        [FromQuery] string? sortDir = "desc",
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<PagedResult<DocumentListDto>>();
        try
        {
            var data = await service.ListAsync(
                apartmentId, search, categoryId, linkedEntityType, linkedEntityId, uploadedByUserId, uploadedFrom, uploadedTo,
                expiringWithinDays, page, pageSize, sortBy, sortDir, cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<DocumentListDto>> { Success = true, Message = "Documents loaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<PagedResult<DocumentListDto>> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList documents.");
            return ServerError<PagedResult<DocumentListDto>>();
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<DocumentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<DocumentDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<DocumentDetailDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<DocumentDetailDto>();
        try
        {
            var data = await service.GetAsync(apartmentId, id, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<DocumentDetailDto> { Success = false, Message = "Document not found.", Errors = ["NOT_FOUND"] });
            return Ok(new ApiResponseDto<DocumentDetailDto> { Success = true, Message = "Document loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get document {Id}.", id);
            return ServerError<DocumentDetailDto>();
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<IdResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<IdResultDto>>> Upload(
        [FromForm] UploadDocumentFormRequest request,
        CancellationToken cancellationToken = default)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = "file is required.", Errors = ["VALIDATION_FAILED"] });
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<IdResultDto> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<IdResultDto>();
        try
        {
            var uploadRequest = new UploadDocumentRequest
            {
                DocumentName = request.DocumentName,
                Description = request.Description,
                CategoryId = request.CategoryId,
                LinkedEntityType = request.LinkedEntityType,
                LinkedEntityId = request.LinkedEntityId,
                ExpiryDate = request.ExpiryDate
            };
            var id = await service.UploadAsync(apartmentId, userId, uploadRequest, file, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponseDto<IdResultDto> { Success = true, Message = "Document uploaded.", Data = new IdResultDto { Id = id } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<IdResultDto> { Success = false, Message = ex.Message, Errors = ["FORBIDDEN"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload document.");
            return ServerError<IdResultDto>();
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId) return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.UpdateAsync(apartmentId, userId, id, request, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update document {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            var data = await service.DownloadAsync(apartmentId, id, cancellationToken);
            return File(data.Stream, data.ContentType, data.FileName, enableRangeProcessing: true);
        }
        catch (InvalidOperationException ex) { return NotFound(ex.Message); }
        catch (FileNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Download document {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:int}/preview-url")]
    [ProducesResponseType(typeof(ApiResponseDto<DocumentPreviewUrlDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<DocumentPreviewUrlDto>>> PreviewUrl([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<DocumentPreviewUrlDto>();
        try
        {
            var data = await service.GetPreviewUrlAsync(apartmentId, id, cancellationToken);
            return Ok(new ApiResponseDto<DocumentPreviewUrlDto> { Success = true, Message = "Preview URL generated.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiResponseDto<DocumentPreviewUrlDto> { Success = false, Message = ex.Message, Errors = ["NOT_FOUND"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Preview document {Id}.", id);
            return ServerError<DocumentPreviewUrlDto>();
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId) return Unauthorized();
        if (currentUser.IdApartment is not { } apartmentId) return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            await service.DeleteAsync(apartmentId, userId, id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete document {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("linked-entities")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<LinkedEntityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<LinkedEntityDto>>>> LinkedEntities(
        [FromQuery] string type,
        [FromQuery] string? search,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<IReadOnlyList<LinkedEntityDto>>();
        try
        {
            var data = await service.SearchLinkedEntitiesAsync(apartmentId, type, search, take, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<LinkedEntityDto>> { Success = true, Message = "Linked entities loaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IReadOnlyList<LinkedEntityDto>> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Linked entities {Type}.", type);
            return ServerError<IReadOnlyList<LinkedEntityDto>>();
        }
    }

    [HttpGet("expiring")]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ExpiringDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<ExpiringDocumentDto>>>> Expiring(
        [FromQuery] int windowDays = 60,
        [FromQuery] int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<IReadOnlyList<ExpiringDocumentDto>>();
        try
        {
            var data = await service.GetExpiringAsync(apartmentId, windowDays, categoryId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<ExpiringDocumentDto>> { Success = true, Message = "Expiring documents loaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<IReadOnlyList<ExpiringDocumentDto>> { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Expiring documents.");
            return ServerError<IReadOnlyList<ExpiringDocumentDto>>();
        }
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponseDto<DocumentStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<DocumentStatsDto>>> Stats(CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId) return ForbiddenNoApartment<DocumentStatsDto>();
        try
        {
            var data = await service.GetStatsAsync(apartmentId, cancellationToken);
            return Ok(new ApiResponseDto<DocumentStatsDto> { Success = true, Message = "Document stats loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Document stats.");
            return ServerError<DocumentStatsDto>();
        }
    }

    private ActionResult<ApiResponseDto<T>> ForbiddenNoApartment<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T>
            {
                Success = false,
                Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                Errors = ["NO_APARTMENT_CONTEXT"]
            });

    private ActionResult<ApiResponseDto<T>> ServerError<T>() =>
        StatusCode(StatusCodes.Status500InternalServerError,
            new ApiResponseDto<T> { Success = false, Message = "An unexpected error occurred.", Errors = ["INTERNAL_SERVER_ERROR"] });
}
