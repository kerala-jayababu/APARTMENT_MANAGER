using Asp.Versioning;
using Apartment_API.Configuration;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Apartment_API.Controllers;

/// <summary>Budget revision batches (M09) — draft, submit, L1/L2 approval, files.</summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ApiAccess)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/budget-revisions")]
public sealed class BudgetRevisionsController(
    IBudgetRevisionService service,
    ICurrentUser currentUser,
    ILogger<BudgetRevisionsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResult<BudgetRevisionBatchListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<PagedResult<BudgetRevisionBatchListItemDto>>>> ListBatches(
        [FromQuery] int? fiscalYearId,
        [FromQuery] string? status,
        [FromQuery] int? budgetId,
        [FromQuery] int? createdBy,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<PagedResult<BudgetRevisionBatchListItemDto>>();
        try
        {
            var data = await service.ListBatchesAsync(apartmentId, fiscalYearId, status, budgetId, createdBy, from, to, page, pageSize,
                cancellationToken);
            return Ok(new ApiResponseDto<PagedResult<BudgetRevisionBatchListItemDto>>
                { Success = true, Message = "Budget revision batches loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "List budget revision batches.");
            return ServerError<PagedResult<BudgetRevisionBatchListItemDto>>();
        }
    }

    [HttpGet("eligible-budgets")]
    [ProducesResponseType(typeof(ApiResponseDto<EligibleBudgetListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<EligibleBudgetListDto>>> GetEligibleBudgets(
        [FromQuery] int fiscalYearId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<EligibleBudgetListDto>();
        try
        {
            var data = await service.GetEligibleBudgetsAsync(apartmentId, fiscalYearId, cancellationToken);
            return Ok(new ApiResponseDto<EligibleBudgetListDto>
                { Success = true, Message = "Eligible budgets loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Eligible budgets for budget revision.");
            return ServerError<EligibleBudgetListDto>();
        }
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionSummaryDto>>> GetSummary(
        [FromQuery] int fiscalYearId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionSummaryDto>();
        try
        {
            var data = await service.GetSummaryAsync(apartmentId, fiscalYearId, cancellationToken);
            return Ok(new ApiResponseDto<BudgetRevisionSummaryDto>
                { Success = true, Message = "Budget revision summary loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Budget revision summary.");
            return ServerError<BudgetRevisionSummaryDto>();
        }
    }

    [HttpGet("lines/{idBudgetRevision:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionLineDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionLineDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionLineDetailDto>>> GetLine(
        [FromRoute] int idBudgetRevision,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionLineDetailDto>();
        try
        {
            var data = await service.GetLineAsync(apartmentId, idBudgetRevision, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<BudgetRevisionLineDetailDto>
                    { Success = false, Message = "Revision line not found.", Errors = ["NOT_FOUND"] });
            return Ok(new ApiResponseDto<BudgetRevisionLineDetailDto>
                { Success = true, Message = "Revision line loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get budget revision line.");
            return ServerError<BudgetRevisionLineDetailDto>();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseDto<CreateBudgetRevisionBatchResultDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponseDto<CreateBudgetRevisionBatchResultDto>>> CreateDraft(
        [FromBody] CreateBudgetRevisionBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<CreateBudgetRevisionBatchResultDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<CreateBudgetRevisionBatchResultDto>();
        try
        {
            var data = await service.CreateDraftAsync(apartmentId, userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseDto<CreateBudgetRevisionBatchResultDto>
                { Success = true, Message = "Draft batch created.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<CreateBudgetRevisionBatchResultDto>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<CreateBudgetRevisionBatchResultDto>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create budget revision draft.");
            return ServerError<CreateBudgetRevisionBatchResultDto>();
        }
    }

    [HttpPut("{batchId}")]
    [ProducesResponseType(typeof(ApiResponseDto<CreateBudgetRevisionBatchResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<CreateBudgetRevisionBatchResultDto>>> ReplaceDraft(
        [FromRoute] string batchId,
        [FromBody] UpdateBudgetRevisionBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<CreateBudgetRevisionBatchResultDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<CreateBudgetRevisionBatchResultDto>();
        try
        {
            var data = await service.ReplaceDraftBatchAsync(apartmentId, userId, batchId, request, cancellationToken);
            return Ok(new ApiResponseDto<CreateBudgetRevisionBatchResultDto>
                { Success = true, Message = "Draft batch replaced.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<CreateBudgetRevisionBatchResultDto>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<CreateBudgetRevisionBatchResultDto>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Replace budget revision draft.");
            return ServerError<CreateBudgetRevisionBatchResultDto>();
        }
    }

    [HttpPatch("lines/{idBudgetRevision:int}")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionLineDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionLineDetailDto>>> PatchLine(
        [FromRoute] int idBudgetRevision,
        [FromBody] PatchBudgetRevisionLineRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<BudgetRevisionLineDetailDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionLineDetailDto>();
        try
        {
            var data = await service.PatchLineAsync(apartmentId, userId, idBudgetRevision, request, cancellationToken);
            return Ok(new ApiResponseDto<BudgetRevisionLineDetailDto>
                { Success = true, Message = "Line updated.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<BudgetRevisionLineDetailDto>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<BudgetRevisionLineDetailDto>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Patch budget revision line.");
            return ServerError<BudgetRevisionLineDetailDto>();
        }
    }

    [HttpDelete("lines/{idBudgetRevision:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ApiResponseDto<object?>>> DeleteLine(
        [FromRoute] int idBudgetRevision,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<object?> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<object?>();
        try
        {
            await service.DeleteLineAsync(apartmentId, userId, idBudgetRevision, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<object?>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<object?>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete budget revision line.");
            return ServerError<object?>();
        }
    }

    [HttpDelete("{batchId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ApiResponseDto<object?>>> DeleteBatch(
        [FromRoute] string batchId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<object?> { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<object?>();
        try
        {
            await service.DeleteBatchAsync(apartmentId, userId, batchId, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<object?>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<object?>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete budget revision batch.");
            return ServerError<object?>();
        }
    }

    [HttpPost("{batchId}/submit")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionSubmitResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionSubmitResultDto>>> Submit(
        [FromRoute] string batchId,
        [FromBody] BudgetRevisionSubmitRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<BudgetRevisionSubmitResultDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionSubmitResultDto>();
        try
        {
            var data = await service.SubmitAsync(apartmentId, userId, batchId, request ?? new BudgetRevisionSubmitRequest(),
                cancellationToken);
            return Ok(new ApiResponseDto<BudgetRevisionSubmitResultDto>
                { Success = true, Message = "Batch submitted for approval.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<BudgetRevisionSubmitResultDto>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<BudgetRevisionSubmitResultDto>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Submit budget revision batch.");
            return ServerError<BudgetRevisionSubmitResultDto>();
        }
    }

    [HttpPost("{batchId}/recall")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionBatchDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionBatchDetailDto>>> Recall(
        [FromRoute] string batchId,
        [FromBody] BudgetRevisionRecallRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<BudgetRevisionBatchDetailDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionBatchDetailDto>();
        try
        {
            var data = await service.RecallAsync(apartmentId, userId, batchId, request ?? new BudgetRevisionRecallRequest(),
                cancellationToken);
            return Ok(new ApiResponseDto<BudgetRevisionBatchDetailDto>
                { Success = true, Message = "Batch recalled to draft.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<BudgetRevisionBatchDetailDto>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<BudgetRevisionBatchDetailDto>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recall budget revision batch.");
            return ServerError<BudgetRevisionBatchDetailDto>();
        }
    }

    [HttpPost("{batchId}/approve")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionApproveResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionApproveResultDto>>> Approve(
        [FromRoute] string batchId,
        [FromBody] BudgetRevisionApproveRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<BudgetRevisionApproveResultDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionApproveResultDto>();
        try
        {
            var data = await service.ApproveAsync(apartmentId, userId, batchId, request ?? new BudgetRevisionApproveRequest(),
                cancellationToken);
            return Ok(new ApiResponseDto<BudgetRevisionApproveResultDto>
                { Success = true, Message = "Approval recorded.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<BudgetRevisionApproveResultDto>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<BudgetRevisionApproveResultDto>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Approve budget revision batch.");
            return ServerError<BudgetRevisionApproveResultDto>();
        }
    }

    [HttpPost("{batchId}/reject")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionRejectResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionRejectResultDto>>> Reject(
        [FromRoute] string batchId,
        [FromBody] BudgetRevisionRejectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<BudgetRevisionRejectResultDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionRejectResultDto>();
        try
        {
            var data = await service.RejectAsync(apartmentId, userId, batchId, request, cancellationToken);
            return Ok(new ApiResponseDto<BudgetRevisionRejectResultDto>
                { Success = true, Message = "Batch rejected.", Data = data });
        }
        catch (UnauthorizedAccessException)
        {
            return ForbidResponse<BudgetRevisionRejectResultDto>();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictOrBadRequest<BudgetRevisionRejectResultDto>(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reject budget revision batch.");
            return ServerError<BudgetRevisionRejectResultDto>();
        }
    }

    [HttpPost("files")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionFileUploadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionFileUploadDto>>> UploadFile(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdUser is not { } userId)
            return Unauthorized(new ApiResponseDto<BudgetRevisionFileUploadDto>
                { Success = false, Message = "User id is not available in the token." });
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionFileUploadDto>();
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponseDto<BudgetRevisionFileUploadDto>
                { Success = false, Message = "file is required.", Errors = ["VALIDATION_FAILED"] });
        try
        {
            var data = await service.UploadFileAsync(apartmentId, userId, file, cancellationToken);
            return Ok(new ApiResponseDto<BudgetRevisionFileUploadDto>
                { Success = true, Message = "File uploaded.", Data = data });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponseDto<BudgetRevisionFileUploadDto>
                { Success = false, Message = ex.Message, Errors = ["VALIDATION_FAILED"] });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload budget revision file.");
            return ServerError<BudgetRevisionFileUploadDto>();
        }
    }

    [HttpGet("{batchId}")]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionBatchDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<BudgetRevisionBatchDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<BudgetRevisionBatchDetailDto>>> GetBatch(
        [FromRoute] string batchId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IdApartment is not { } apartmentId)
            return Forbidden<BudgetRevisionBatchDetailDto>();
        try
        {
            var data = await service.GetBatchAsync(apartmentId, batchId, cancellationToken);
            if (data is null)
                return NotFound(new ApiResponseDto<BudgetRevisionBatchDetailDto>
                    { Success = false, Message = "Batch not found.", Errors = ["NOT_FOUND"] });
            return Ok(new ApiResponseDto<BudgetRevisionBatchDetailDto>
                { Success = true, Message = "Batch loaded.", Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get budget revision batch.");
            return ServerError<BudgetRevisionBatchDetailDto>();
        }
    }

    private ActionResult<ApiResponseDto<T>> ConflictOrBadRequest<T>(InvalidOperationException ex)
    {
        var msg = ex.Message;
        if (msg.StartsWith("CONFLICT:", StringComparison.Ordinal) || msg.StartsWith("LIMIT_REACHED:", StringComparison.Ordinal))
        {
            return StatusCode(StatusCodes.Status409Conflict, new ApiResponseDto<T>
            {
                Success = false,
                Message = msg,
                Errors = ["CONFLICT"]
            });
        }
        return BadRequest(new ApiResponseDto<T> { Success = false, Message = msg, Errors = ["VALIDATION_FAILED"] });
    }

    private ActionResult<ApiResponseDto<T>> Forbidden<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T>
            {
                Success = false,
                Message = "Apartment context is required. Use a tenant access token with apartment_id.",
                Errors = ["NO_APARTMENT_CONTEXT"]
            });

    private ActionResult<ApiResponseDto<T>> ForbidResponse<T>() =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ApiResponseDto<T> { Success = false, Message = "FORBIDDEN", Errors = ["FORBIDDEN"] });

    private ActionResult<ApiResponseDto<T>> ServerError<T>() =>
        StatusCode(StatusCodes.Status500InternalServerError,
            new ApiResponseDto<T> { Success = false, Message = "An unexpected error occurred.", Errors = ["INTERNAL_SERVER_ERROR"] });
}
