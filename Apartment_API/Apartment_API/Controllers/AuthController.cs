using Asp.Versioning;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apartment_API.Controllers;

[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController(
    IUserService userService,
    IJwtTokenService jwtTokenService,
    IOtpAuthService otpAuthService,
    ICurrentUser currentUser,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>Who am I, based on the Bearer token. Requires Authorization header.</summary>
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
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<UserPublicDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponseDto<LoginWithTokenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginWithTokenResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginWithTokenResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginWithTokenResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<LoginWithTokenResponseDto>>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<LoginWithTokenResponseDto>
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
                return Unauthorized(new ApiResponseDto<LoginWithTokenResponseDto>
                {
                    Success = false,
                    Message = "Invalid email or password.",
                    Errors = [err]
                });
            }

            var (accessToken, expiresAt) = jwtTokenService.CreateToken(user);
            return Ok(new ApiResponseDto<LoginWithTokenResponseDto>
            {
                Success = true,
                Message = "Login successful.",
                Data = new LoginWithTokenResponseDto
                {
                    AccessToken = accessToken,
                    TokenType = "Bearer",
                    ExpiresAtUtc = expiresAt,
                    User = user
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<LoginWithTokenResponseDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }

    /// <summary>Send a one-time code to the user's email (stores LoginOTP, OTPCreatedDate, OTPExpiryDate in Users).</summary>
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
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<object?>
                {
                    Success = false,
                    Message = "Could not send login code. Try again later.",
                    Errors = ["EMAIL_SEND_FAILED"]
                });
        }
    }

    /// <summary>Verify the emailed OTP and return the same token payload as password login.</summary>
    [HttpPost("login-otp/verify")]
    [ProducesResponseType(typeof(ApiResponseDto<LoginWithTokenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginWithTokenResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginWithTokenResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<LoginWithTokenResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<LoginWithTokenResponseDto>>> VerifyLoginOtp(
        [FromBody] VerifyOtpDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponseDto<LoginWithTokenResponseDto>
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
                return Unauthorized(new ApiResponseDto<LoginWithTokenResponseDto>
                {
                    Success = false,
                    Message = err == "EXPIRED_OR_INVALID_OTP"
                        ? "Code expired or invalid. Request a new code."
                        : "Invalid code.",
                    Errors = [err!]
                });
            }

            var (accessToken, expiresAt) = jwtTokenService.CreateToken(user!);
            return Ok(new ApiResponseDto<LoginWithTokenResponseDto>
            {
                Success = true,
                Message = "Login successful.",
                Data = new LoginWithTokenResponseDto
                {
                    AccessToken = accessToken,
                    TokenType = "Bearer",
                    ExpiresAtUtc = expiresAt,
                    User = user!
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during OTP verification.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponseDto<LoginWithTokenResponseDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    Errors = ["INTERNAL_SERVER_ERROR"]
                });
        }
    }
}
