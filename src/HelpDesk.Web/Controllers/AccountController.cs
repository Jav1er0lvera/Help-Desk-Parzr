using HelpDesk.Domain.Users;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Web.Controllers;

// TODO: mover la lógica de autenticación a HelpDesk.API
// y consumirla desde aquí usando HelpDesk.SDK (Refit).
// Por ahora se conecta directo a Infrastructure para avanzar.
public class AccountController : Controller
{
    private readonly HelpDeskDbContext _db;

    public AccountController(HelpDeskDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(request);
        }

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(System.Security.Claims.ClaimTypes.Email, user.Email),
            new(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal);

        return RedirectToAction("Dashboard", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            ModelState.AddModelError("", "Las contraseñas no coinciden.");
            return View(request);
        }

        if (request.Password.Length < 8)
        {
            ModelState.AddModelError("", "La contraseña debe tener al menos 8 caracteres.");
            return View(request);
        }

        var emailNormalizado = request.Email.Trim().ToLower();
        var existe = await _db.Users.AnyAsync(u => u.Email == emailNormalizado);
        if (existe)
        {
            ModelState.AddModelError("", "Ya existe una cuenta con este correo.");
            return View(request);
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = emailNormalizado,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role switch
            {
                "Soporte" => HelpDesk.Domain.Enums.UserRole.Soporte,
                "Supervisor" => HelpDesk.Domain.Enums.UserRole.Supervisor,
                _ => HelpDesk.Domain.Enums.UserRole.Usuario
            }
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToAction("Index", "Home");
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Role { get; set; } = "Usuario";
}