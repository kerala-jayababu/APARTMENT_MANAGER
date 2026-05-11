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
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController(
    IUserService userService,
    IJwtTokenService jwtTokenService,
    IOtpAuthService otpAuthService,
    IApartmentAuthService apartmentAuthService,
    ICurrentUser currentUser,
    ILogger<AuthController> logger,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Who am I, based on the Bearer token (selection or access JWT).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<CurrentUserInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponseDto<CurrentUserInfoDto>> GetCurrent()
    {
        if (currentUser.IdUser is not { } id)
        {
            return Unauthorized(new ApiResponseDto<CurrentUserInfoDto>
            {
                Success = false,
                Message = "Not authenticated."
            });
        }

        return Ok(new ApiResponseDto<CurrentUserInfoDto>
        {
            Success = true,
            Message = "Current user from JWT claims.",
            Data = new CurrentUserInfoDto
            {
                IdUser = id,
                FullName = currentUser.FullName ?? string.Empty,
                Email = currentUser.Email ?? string.Empty,
                PhoneNumber = string.IsNullOrEmpty(currentUser.PhoneNumber) ? null : currentUser.PhoneNumber,
                IsSuperAdmin = currentUser.IsSuperAdmin
            }
        });
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponseDto<UserPublicDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<UserPublicDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<UserPublicDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponseDto<UserPublicDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<UserPublicDto>>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<UserPublicDto>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        try
        {
            var (user, err) = await userService.RegisterAsync(request, cancellationToken);
            if (err == "EMAIL_ALREADY_EXISTS")
            {
                return Conflict(new ApiResponseDto<UserPublicDto>
                {
                    Success = false,
                    Message = "A user with this email already exists.",
                    Errors = [err]
                });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                new ApiResponseDto<UserPublicDto>
                {
                    Success = true,
                    Message = "User registered successfully.",
                    Data = user
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration.");
            return this.ApiServerError<UserPublicDto>(environment, configuration, ex);
        }
    }

    /// <summary>Validates credentials only. Returns a short-lived <see cref="LoginAuthenticationResultDto.ApartmentSelectionToken" /> (not the API access JWT). Use tenant-access-token next.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponseDto<LoginAuthenticationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginAuthenticationResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginAuthenticationResultDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginAuthenticationResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<LoginAuthenticationResultDto>>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<LoginAuthenticationResultDto>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        try
        {
            var (user, err) = await userService.LoginAsync(request, cancellationToken);
            if (err == "INVALID_CREDENTIALS")
            {
                return Unauthorized(new ApiResponseDto<LoginAuthenticationResultDto>
                {
                    Success = false,
                    Message = "Invalid email or password.",
                    Errors = [err]
                });
            }

            var (selectionToken, expires) = jwtTokenService.CreateApartmentSelectionToken(user);
            return Ok(new ApiResponseDto<LoginAuthenticationResultDto>
            {
                Success = true,
                Message = "Authenticated. Use ApartmentSelectionToken to list apartments, then tenant-access-token.",
                Data = new LoginAuthenticationResultDto
                {
                    IdUser = user.IdUser,
                    ApartmentSelectionToken = selectionToken,
                    ApartmentSelectionExpiresAtUtc = expires
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login.");
            return this.ApiServerError<LoginAuthenticationResultDto>(environment, configuration, ex);
        }
    }

    [HttpPost("login-otp/request")]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<object?>>> RequestLoginOtp(
        [FromBody] RequestOtpDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<object?>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        try
        {
            await otpAuthService.RequestLoginOtpAsync(request.Email, cancellationToken);
            return Ok(new ApiResponseDto<object?>
            {
                Success = true,
                Message =
                    "If an account with this email exists, a login code was sent. Check your inbox and spam folder."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending login OTP email.");
            return this.ApiServerError<object?>(environment, configuration, ex);
        }
    }

    /// <summary>Verifies OTP only. Returns the same shape as password login (selection token, not API access JWT).</summary>
    [HttpPost("login-otp/verify")]
    [ProducesResponseType(typeof(ApiResponseDto<LoginAuthenticationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginAuthenticationResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginAuthenticationResultDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginAuthenticationResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<LoginAuthenticationResultDto>>> VerifyLoginOtp(
        [FromBody] VerifyOtpDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<LoginAuthenticationResultDto>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        try
        {
            var (user, err) = await otpAuthService.VerifyLoginOtpAsync(request.Email, request.Otp, cancellationToken);
            if (err is "EXPIRED_OR_INVALID_OTP" or "INVALID_OTP")
            {
                return Unauthorized(new ApiResponseDto<LoginAuthenticationResultDto>
                {
                    Success = false,
                    Message = err == "EXPIRED_OR_INVALID_OTP"
                        ? "Code expired or invalid. Request a new code."
                        : "Invalid code.",
                    Errors = [err!]
                });
            }

            var (selectionToken, expires) = jwtTokenService.CreateApartmentSelectionToken(user!);
            return Ok(new ApiResponseDto<LoginAuthenticationResultDto>
            {
                Success = true,
                Message = "OTP verified. Use ApartmentSelectionToken for apartment list and tenant-access-token.",
                Data = new LoginAuthenticationResultDto
                {
                    IdUser = user!.IdUser,
                    ApartmentSelectionToken = selectionToken,
                    ApartmentSelectionExpiresAtUtc = expires
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during OTP verification.");
            return this.ApiServerError<LoginAuthenticationResultDto>(environment, configuration, ex);
        }
    }

    /// <summary>
    /// Lists apartments the user may access. Requires the short-lived selection JWT. <paramref name="userId" /> must match the token subject.
    /// </summary>
    [HttpGet("users/{userId:int}/apartments")]
    [Authorize(Policy = AuthorizationPolicies.ApartmentSelect)]
    [ProducesResponseType(typeof(ApiResponseDto<IReadOnlyList<AvailableApartmentItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyList<AvailableApartmentItemDto>>>> GetUserApartments(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        if (currentUser.IdUser != userId)
        {
            return Forbid();
        }

        try
        {
            var data = await apartmentAuthService.GetApartmentsForUserAsync(userId, cancellationToken);
            return Ok(new ApiResponseDto<IReadOnlyList<AvailableApartmentItemDto>>
            {
                Success = true,
                Message = "Apartments loaded.",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetUserApartments");
            return this.ApiServerError<IReadOnlyList<AvailableApartmentItemDto>>(environment, configuration, ex);
        }
    }

    /// <summary>
    /// Issues the API access JWT for a specific apartment. Requires selection JWT. Fails if the user has no apartment membership or is not mapped to the requested apartment.
    /// </summary>
    [HttpPost("tenant-access-token")]
    [Authorize(Policy = AuthorizationPolicies.ApartmentSelect)]
    [ProducesResponseType(typeof(ApiResponseDto<TenantTokenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<TenantTokenResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<TenantTokenResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<TenantTokenResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponseDto<TenantTokenResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<TenantTokenResponseDto>>> CreateTenantAccessToken(
        [FromBody] TenantTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<TenantTokenResponseDto>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        if (currentUser.IdUser is not { } uid)
        {
            return Unauthorized(new ApiResponseDto<TenantTokenResponseDto>
            {
                Success = false,
                Message = "Not authenticated."
            });
        }

        try
        {
            var (data, err) = await apartmentAuthService.CreateTenantAccessTokenAsync(uid, request.ApartmentId, cancellationToken);
            if (err == "USER_INACTIVE_OR_MISSING")
            {
                return Unauthorized(new ApiResponseDto<TenantTokenResponseDto>
                {
                    Success = false,
                    Message = "User is inactive or missing.",
                    Errors = [err]
                });
            }

            if (err == "NO_APARTMENT_MEMBERSHIP")
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ApiResponseDto<TenantTokenResponseDto>
                    {
                        Success = false,
                        Message = "You are not assigned to any apartment. Cannot issue an access token.",
                        Errors = [err]
                    });
            }

            if (err is "NOT_MAPPED_TO_APARTMENT" or "APARTMENT_INACTIVE_OR_MISSING" or "INVALID_APARTMENT_ID")
            {
                return NotFound(new ApiResponseDto<TenantTokenResponseDto>
                {
                    Success = false,
                    Message = err == "NOT_MAPPED_TO_APARTMENT"
                        ? "You are not mapped to this apartment."
                        : "Apartment not found or inactive.",
                    Errors = [err!]
                });
            }

            return Ok(new ApiResponseDto<TenantTokenResponseDto>
            {
                Success = true,
                Message = "Access token issued. Use it as Bearer for API calls.",
                Data = data!
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CreateTenantAccessToken");
            return this.ApiServerError<TenantTokenResponseDto>(environment, configuration, ex);
        }
    }
}
