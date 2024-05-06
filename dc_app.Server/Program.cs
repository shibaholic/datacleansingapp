using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using dc_app.ServiceLibrary.ServiceLayer;
using ServiceLibrary.RepositoryLayer;
using dc_app.ServiceLibrary.RepositoryLayer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using dc_app.Server.Authorization;
using dc_app.Server.Controllers;

Console.WriteLine("program start 0.1");

// BUILD
var builder = WebApplication.CreateBuilder(args);

// environment variables
string DB_CONNECTION_STRING = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
//Console.WriteLine(DB_CONNECTION_STRING);

// automapper
builder.Services.AddAutoMapper(typeof(Program));

// controller layer
builder.Services.AddControllers();
// controller helper service
builder.Services.AddTransient<IAuthHelperService, AuthHelperService>();

// service layer
builder.Services.AddTransient<ISpreadsheetConfigService, SpreadsheetConfigService>();
builder.Services.AddTransient<ISpreadsheetDataService, SpreadsheetDataService>();
builder.Services.AddTransient<ISpreadsheetCreateDeleteService, SpreadsheetCreateDeleteService>();
builder.Services.AddTransient<IDataCleansingService, DataCleansingService>();
builder.Services.AddTransient<ISpreadsheetFileService, SpreadsheetFileService>();

// repository layer
builder.Services.AddTransient<ISpreadsheetConfigRepo, SpreadsheetConfigRepo>();
builder.Services.AddTransient<IColumnConfigRepo, ColumnConfigRepo>();
builder.Services.AddTransient<ISpreadsheetDataRepo, SpreadsheetDataRepo>();
builder.Services.AddTransient<IUserHasSpreadsheetRepo, userHasSpreadsheetRepo>();
builder.Services.AddTransient<IUploadStatusRepo, UploadStatusRepo>();
SqlConnectionFactory.SetConfig(DB_CONNECTION_STRING);

// identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddDefaultTokenProviders();
builder.Services.AddTransient<IUserStore<IdentityUser>, UserService>();
builder.Services.AddTransient<IUserRepository, UserRepo>();
builder.Services.AddTransient<IRoleStore<IdentityRole>, RoleService>();
builder.Services.AddTransient<IPasswordHasher<IdentityUser>, PasswordHasher<IdentityUser>>();

// cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeUser", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("UserHasSpreadshAllowed", pb =>
    {
        pb.Requirements.Add(new UserHasSpreadshAllowed());
    });
});

// authorization handler
builder.Services.AddTransient<IAuthorizationHandler, UserSpreadshAuthorizationHandler>();

// cors policy
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins,
                          policy =>
                          {
                              policy.WithOrigins("https://datacleansingapp.azurewebsites.net",
                                                  "https://localhost:4200",
                                                  "https://localhost:8080")
                                                  .AllowAnyHeader()
                                                  .AllowAnyMethod()
                                                  .AllowCredentials();
                          });
});

// form options
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 512 * 1024 * 1024;
});


// APP
var app = builder.Build();

// custom middleware
app.Use(async (ctx, next) =>
{
    // if the path is the client route, then route to index.html so react is loaded again.
    if (!ctx.Request.Path.Value.StartsWith("/api") && 
        !ctx.Request.Path.Value.StartsWith("/assets") && 
        !ctx.Request.Path.Value.StartsWith("/favicon"))
    {
        ctx.Request.Path = "/index.html";
    }

    await next();
});

var cookiePolicyOptions = new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
};

app.UseCookiePolicy(cookiePolicyOptions);

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
