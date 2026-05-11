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
[Route("api/v{version:apiVersion}/masters")]
public sealed class ExpenseMastersController(
    IExpenseManagementService service,
    ICurrentUser currentUser,
    ILogger<ExpenseMastersController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("vendors")]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<ExpenseSimpleLookupDto>>>> Vendors(
        [FromQuery] bool isActive = true,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        Run(apartmentId => service.GetVendorsAsync(apartmentId, isActive, search, cancellationToken), "Vendors loaded.", "Expense vendors.");

    [HttpGet("expense-heads")]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<ExpenseSimpleLookupDto>>>> ExpenseHeads(
        [FromQuery] bool isActive = true,
        CancellationToken cancellationToken = default) =>
        Run(apartmentId => service.GetExpenseHeadsAsync(apartmentId, isActive, cancellationToken), "Expense heads loaded.", "Expense heads.");

    [HttpGet("bank-accounts")]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<ExpenseSimpleLookupDto>>>> BankAccounts(CancellationToken cancellationToken = default) =>
        Run(apartmentId => service.GetBankAccountsAsync(apartmentId, cancellationToken), "Bank accounts loaded.", "Expense bank accounts.");

    [HttpGet("fiscal-years")]
    public Task<ActionResult<ApiResponseDto<IReadOnlyList<ExpenseSimpleLookupDto>>>> FiscalYears(CancellationToken cancellationToken = default) =>
        Run(apartmentId => service.GetFiscalYearsAsync(apartmentId, cancellationToken), "Fiscal years loaded.", "Expense fiscal years.");

    private async Task<ActionResult<ApiResponseDto<IReadOnlyList<ExpenseSimpleLookupDto>>>> Run(
        Func<int, Task<IReadOnlyList<ExpenseSimpleLookupDto>>> work,
        string message,
        string logMessage)
    {
        if (currentUser.IdApartment is not { } apartmentId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponseDto<IReadOnlyList<ExpenseSimpleLookupDto>>
                {
                    Success = false,
                    Message = "Apartment context is required.",
                    Errors = ["NO_APARTMENT_CONTEXT"]
                });
        }

        try
        {
            var data = await work(apartmentId);
            return Ok(new ApiResponseDto<IReadOnlyList<ExpenseSimpleLookupDto>> { Success = true, Message = message, Data = data });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{LogMessage}", logMessage);
            return this.ApiServerError<IReadOnlyList<ExpenseSimpleLookupDto>>(environment, configuration, ex);
        }
    }
}
