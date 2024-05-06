using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using DocumentFormat.OpenXml.InkML;
using dc_app.ServiceLibrary.RepositoryLayer;

namespace dc_app.Server.Controllers;

public class UserCredentials
{
    public string username { get; set; }
    public string password { get; set; }
}

[ApiController]
[Route("api/user")]
[Authorize(Policy = "MustBeUser")]
public class UserController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IPasswordHasher<IdentityUser> _hasher;
    /*        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;*/

    public UserController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IPasswordHasher<IdentityUser> hasher)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _hasher = hasher;
    }

    public class UserResult
    {
        public UserResult(bool success)
        {
            Success = success;
        }
        public UserResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    // POST: /api/user/signup
    [HttpPost]
    [AllowAnonymous]
    [Route("signup")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> SignUp(
        [FromForm] UserCredentials userCredentials)
    {
        var connectToDBResult = await SqlConnectionFactory.TestConnection();
        if (!connectToDBResult.success)
        {
            return StatusCode(500, new UserResult(false, "Server error. Sometimes the database needs 1 minute to warm up. Please try again."));
        }

        IdentityUser user = new IdentityUser { UserName = userCredentials.username, Email = userCredentials.username + "@email", EmailConfirmed = false };
        user.PasswordHash = _hasher.HashPassword(user, userCredentials.password);

        var result = await _userManager.CreateAsync(user);
        Console.WriteLine(result.ToString());

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: true, CookieAuthenticationDefaults.AuthenticationScheme);
            return StatusCode(200, user);
        } else if (result.Errors.First().Code == "DuplicateUserName")
        {
            return StatusCode(400, new UserResult(false, "This username is already taken. Please try another username."));
        }
        await Console.Out.WriteLineAsync("return status code 500");
        return StatusCode(500, new UserResult(false, "Server error. Sometimes the database needs 1 minute to warm up. Please try again."));
    }

    // POST: /api/user/login
    [HttpPost]
    [AllowAnonymous]
    [Route("login")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> LogIn(
        [FromForm] UserCredentials userCredentials)
    {
        var connectToDBResult = await SqlConnectionFactory.TestConnection();
        if (!connectToDBResult.success)
        {
            return StatusCode(500, new UserResult(false, "Server error. Sometimes the database needs 1 minute to warm up. Please try again."));
        }

        var user = await _userManager.FindByNameAsync(userCredentials.username);

        if(user == null)
        {
            return StatusCode(400, new UserResult(false, "Incorrect username or password. Please try again."));
        }

        var pwResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, userCredentials.password);

        if (pwResult == PasswordVerificationResult.Failed)
        {
            return StatusCode(400, new UserResult(false, "Incorrect username or password. Please try again."));
        }

        await _signInManager.SignInAsync(user, isPersistent: true, CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(user);
    }

    // POST: /api/user/logout
    [HttpPost]
    [Route("logout")]
    public async Task<IActionResult> LogOut()
    {
        await Console.Out.WriteLineAsync("logged out");
        await _signInManager.SignOutAsync();

        return Ok();
    }

    // GET: /api/user
    [HttpGet]
    [Route("user")]
    public async Task<IActionResult> GetUser()
    {
        IdentityUser user = await _userManager.GetUserAsync(HttpContext.User);
        return Ok(user);
    }
}