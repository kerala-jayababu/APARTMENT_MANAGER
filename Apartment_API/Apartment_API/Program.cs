using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Apartment_API.Configuration;
using Apartment_API.Data;
using Apartment_API.Helpers;
using Apartment_API.Services.Implementation;
using Apartment_API.Services.Implementation.Committee;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection(OtpSettings.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? new JwtOptions();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IOtpAuthService, OtpAuthService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
        ValidateLifetime = true
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(AuthorizationPolicies.ApartmentSelect, p =>
        p.RequireClaim(ClaimTypesExtra.TokenPurpose, TokenPurposeValues.ApartmentSelect));
    o.AddPolicy(AuthorizationPolicies.ApiAccess, p =>
        p.RequireAssertion(ctx =>
        {
            if (ctx.User.IsInRole("SuperAdmin"))
                return true;
            return ctx.User.HasClaim(c =>
                c.Type == ClaimTypesExtra.ApartmentId && int.TryParse(c.Value, out _));
        }));
});
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IApartmentAuthService, ApartmentAuthService>();
builder.Services.AddScoped<IApartmentService, ApartmentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGlobalMasterDataService, GlobalMasterDataService>();
builder.Services.AddScoped<IApprovalRuleService, ApprovalRuleService>();
builder.Services.AddScoped<IAmenityService, AmenityService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICollectionsService, CollectionsService>();
builder.Services.AddScoped<IExpenseManagementService, ExpenseManagementService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IExpenseHeadService, ExpenseHeadService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IMmcService, MmcService>();
builder.Services.AddScoped<IIncomeHeadService, IncomeHeadService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<IUnitResidentService, UnitResidentService>();
builder.Services.AddScoped<IOwnerResidentService, OwnerResidentService>();
builder.Services.AddScoped<ICoOwnerResidentService, CoOwnerResidentService>();
builder.Services.AddScoped<ITenantResidentService, TenantResidentService>();
builder.Services.AddScoped<IFamilyMemberResidentService, FamilyMemberResidentService>();
builder.Services.AddScoped<IOwnershipTransferResidentService, OwnershipTransferResidentService>();
builder.Services.AddScoped<CommitteeDataHelper>();
builder.Services.AddScoped<ICommitteeTenureService, CommitteeTenureService>();
builder.Services.AddScoped<ICommitteeMemberService, CommitteeMemberService>();
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            description.GroupName);
    }
});
app.UseHttpsRedirection();
var uploadRoot = Path.Combine(app.Environment.ContentRootPath, "Uploads");
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = "/uploads"
});
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
