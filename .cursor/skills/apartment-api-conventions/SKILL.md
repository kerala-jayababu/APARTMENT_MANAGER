---
name: apartment-api-conventions
description: Enforces Apartment_API patterns—ApiResponseDto return types, controller try/catch, logging, API versioning, and folder layout. Use when editing or adding controllers, services, DTOs, or API endpoints in the Apartment_API project, or when the user asks for API conventions, standard responses, or error handling.
---

# Apartment API conventions

Applies to `Apartment_API/Apartment_API/`. When adding or changing API code, follow these rules.

## Return types (mandatory)

- Expose data only through **`Apartment_API.DTO.ApiResponseDto<T>`** (never return raw entities or DTOs without the wrapper in controllers).
- Controller actions should be typed as:
  - `Task<ActionResult<ApiResponseDto<T>>>` (async) or
  - `ActionResult<ApiResponseDto<T>>` (sync).
- On success: `Success = true`, set `Message`, set `Data`.
- On failure: `Success = false`, set `Message`, set `Errors` (e.g. error codes or short strings); omit or leave `Data` null.
- Add `[ProducesResponseType(typeof(ApiResponseDto<...>), StatusCodes....)]` for documented status codes (200, 400, 404, 500, etc.).

## try/catch in controllers (mandatory)

- Wrap the action body in **`try/catch`**. Call services inside `try`.
- On exception: log with **`ILogger<TController>`** (`LogError` with exception), return **`StatusCode(500, new ApiResponseDto<...> { Success = false, Message = "...", Errors = ["INTERNAL_SERVER_ERROR"] })`** (or a more specific error code when appropriate).
- Re-throw only if a global exception middleware is agreed for the project; default pattern here is **catch, log, return 500 with ApiResponseDto**.

## Services

- Services return domain/DTO data (`Task<...>`, `IReadOnlyCollection<ApartmentDto>`, etc.), **not** `ApiResponseDto`—wrapping is the controller’s job.
- Use **`try/catch` + `ILogger`** in services for non-trivial work; on failure log and **rethrow** so the controller can return the unified 500 response (unless the service can handle a non-exceptional case without throwing).

## API versioning

- Controllers: **`[ApiVersion("1.0")]`** (or the correct version) and route **`[Route("api/v{version:apiVersion}/[controller]")]`** unless the team adds a new agreed pattern.
- New endpoints use versioned URLs (e.g. `GET /api/v1/Apartments`). Update **`Apartment_API.http`** or client samples when adding routes.

## Files and layers

- **`DTO/`** — `ApiResponseDto`, request/response DTOs (no business logic).
- **`Models/`** — entities.
- **`Services/`** — `I...` + implementations; register in **`Program.cs`**.
- **`Database/`** — data access or in-memory stores.
- **`Helpers/`** — mappers, e.g. `MappingExtensions` entity → DTO.
- **`Validators/`** — validation helpers for DTOs.
- **`Configuration/`** — cross-cutting config (e.g. Swagger + versioning).
- **`Controllers/`** — thin: validate input, call services, build **`ApiResponseDto`**, error handling.

## Checklist (before finishing a new/changed endpoint)

- [ ] Return type is `ActionResult<ApiResponseDto<...>>` (or `Task<...>`).
- [ ] Success and error paths both use **`ApiResponseDto`**.
- [ ] `try/catch` in controller with logging on failure.
- [ ] Service registered if new; versioning attributes/routes if new controller.
- [ ] Swagger/`ProducesResponseType` updated if the contract changed.

## Minimal controller shape (reference)

```csharp
[HttpGet]
[ProducesResponseType(typeof(ApiResponseDto<IReadOnlyCollection<ApartmentDto>>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponseDto<IReadOnlyCollection<ApartmentDto>>), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<ApiResponseDto<IReadOnlyCollection<ApartmentDto>>>> GetAll(CancellationToken ct)
{
    try
    {
        var data = await _service.GetAllAsync(ct);
        return Ok(new ApiResponseDto<IReadOnlyCollection<ApartmentDto>>
        {
            Success = true,
            Message = "...",
            Data = data
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "...");
        return StatusCode(StatusCodes.Status500InternalServerError,
            new ApiResponseDto<IReadOnlyCollection<ApartmentDto>>
            {
                Success = false,
                Message = "An unexpected error occurred.",
                Errors = ["INTERNAL_SERVER_ERROR"]
            });
    }
}
```

For deeper stack-specific rules, prefer matching existing controllers (e.g. `ApartmentsController`) and `Program.cs` in this repo.
