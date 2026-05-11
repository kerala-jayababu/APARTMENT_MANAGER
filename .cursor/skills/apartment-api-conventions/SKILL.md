---
name: apartment-api-conventions
description: Enforces Apartment_API patterns—ApiResponseDto return types, controller try/catch, ApiServerError helpers, logging, API versioning, and folder layout. Use when editing or adding controllers, services, DTOs, or API endpoints in the Apartment_API project, or when the user asks for API conventions, standard responses, or error handling.
---

# Apartment API conventions

Applies to `Apartment_API/Apartment_API/`. When adding or changing API code, follow these rules **every time** you create or edit controllers/endpoints.

## Return types (mandatory)

- Expose data only through **`Apartment_API.DTO.ApiResponseDto<T>`** (never return raw entities or DTOs without the wrapper in controllers).
- Controller actions should be typed as:
  - `Task<ActionResult<ApiResponseDto<T>>>` (async) or
  - `ActionResult<ApiResponseDto<T>>` (sync).
- On success: `Success = true`, set `Message`, set `Data`.
- On failure: `Success = false`, set `Message`, set `Errors` (e.g. error codes or short strings); omit or leave `Data` null.
- Add `[ProducesResponseType(typeof(ApiResponseDto<...>), StatusCodes....)]` for documented status codes (200, 400, 404, 500, etc.).

## 500 errors in controllers (mandatory — do not hand-roll generic 500 bodies)

**Never** return a hard-coded 500 like `Message = "An unexpected error occurred."` and only `Errors = ["INTERNAL_SERVER_ERROR"]` from controllers. That hides real failures and bypasses the shared policy.

### Use the extensions (always)

From **`Apartment_API.Configuration.ControllerServerErrorExtensions`**:

| Action return type | On unhandled `Exception` after `LogError` |
|--------------------|-------------------------------------------|
| `Task<ActionResult<ApiResponseDto<T>>>` (or any typed `ActionResult<ApiResponseDto<...>>`) | `return this.ApiServerError<T>(environment, configuration, ex);` |
| `Task<IActionResult>` (e.g. `NoContent()`, `File(...)`) | `return this.ApiServerErrorAction<T>(environment, configuration, ex);` |

- **`ApiServerError<T>`** builds `ActionResult<ApiResponseDto<T>>`.
- **`ApiServerErrorAction<T>`** builds **`IActionResult`**. Use this for actions that return **`IActionResult`** — **`ActionResult<...>` does not implicitly convert to `IActionResult`** in all cases.
- For “no payload” error JSON, use **`object?`** as `T` when appropriate (e.g. `ApiServerErrorAction<object?>(...)`).

### Controller constructor (when you have a `catch (Exception)` that returns 500)

Inject **`IWebHostEnvironment environment`** and **`IConfiguration configuration`** in the controller’s primary constructor (alongside existing services/logger) **whenever** the controller uses `ApiServerError` / `ApiServerErrorAction`.

Usings typically needed:

- `Microsoft.AspNetCore.Hosting`
- `Microsoft.Extensions.Configuration`

### Behaviour (trust the helper)

`ApiServerError` / `ApiServerErrorAction` delegate to **`ApiErrorResponseHelper.FormatException`**:

- In **Development** (`IWebHostEnvironment.IsDevelopment()`), **or** when config **`Api:ExposeExceptionDetails`** is **`true`**: the client gets the **real exception message** (and a second `errors` entry with type + message).
- Otherwise: generic safe message **`"An unexpected error occurred."`** and **`INTERNAL_SERVER_ERROR`** only.

Do **not** duplicate that logic in controllers.

### try/catch in controllers

- Wrap the action body in **`try/catch`**. Call services inside `try`.
- On **`Exception`**: **`LogError`** with the exception, then **`ApiServerError` / `ApiServerErrorAction`** (not raw `StatusCode(500, ...)` for generic failures).
- Map **expected domain failures** (e.g. `InvalidOperationException` with stable messages) to **400 / 409 / 404** with **`ApiResponseDto`** where the codebase already does so — unchanged.
- Re-throw only if agreed; default is **catch, log, return 500 via helpers**.

### Global middleware

Unhandled exceptions still flow to **`UseGlobalApiExceptionHandler`**, which also uses **`ApiErrorResponseHelper`**. Prefer **catching inside the controller** for actions that already use **`try/catch`**, so the same JSON shape applies consistently.

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
- **`Configuration/`** — cross-cutting config (e.g. **`ControllerServerErrorExtensions`**, **`ApiErrorResponseHelper`**, Swagger + versioning).
- **`Controllers/`** — thin: validate input, call services, build **`ApiResponseDto`**, error handling via helpers above.

## Checklist (before finishing a new/changed endpoint)

- [ ] Return type is `ActionResult<ApiResponseDto<...>>` (or `Task<...>` / `IActionResult` where appropriate).
- [ ] Success and error paths both use **`ApiResponseDto`** where a JSON body is returned.
- [ ] **`try/catch`** in controller with **`LogError`** on unexpected failure.
- [ ] Unexpected failures use **`ApiServerError`** or **`ApiServerErrorAction`** — **not** manual generic 500 payloads.
- [ ] Controller injects **`IWebHostEnvironment`** and **`IConfiguration`** if it handles 500 from exceptions.
- [ ] Service registered if new; versioning attributes/routes if new controller.
- [ ] Swagger/`ProducesResponseType` updated if the contract changed.

## Minimal controller shape (reference)

```csharp
public sealed class ExampleController(
    IExampleService service,
    ILogger<ExampleController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ExampleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<ExampleDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<ExampleDto>>>> GetList(CancellationToken ct)
    {
        try
        {
            var data = await service.ListAsync(ct);
            return Ok(new ApiResponseDto<IReadOnlyList<ExampleDto>>
            {
                Success = true,
                Message = "Loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetList example.");
            return this.ApiServerError<IReadOnlyList<ExampleDto>>(environment, configuration, ex);
        }
    }
}
```

For **`Task<IActionResult>`** actions (e.g. `Update` returning `NoContent()`), replace the catch return with:

`return this.ApiServerErrorAction<object?>(environment, configuration, ex);`

For deeper stack-specific rules, prefer matching **`AmenitiesController`**, **`ApartmentsController`**, **`Configuration/ControllerServerErrorExtensions.cs`**, **`Configuration/ApiErrorResponseHelper.cs`**, and **`Program.cs`** in this repo.
