using HelpDesk.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly HelpDeskDbContext _db;

    public UsersController(HelpDeskDbContext db)
    {
        _db = db;
    }

    // GET api/v1/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET api/v1/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _db.Users
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user is null) return NotFound();
        return Ok(user);
    }

    // GET api/v1/users/soporte
    [HttpGet("soporte")]
    public async Task<IActionResult> GetSoporte()
    {
        var users = await _db.Users
            .Where(u => u.Role == HelpDesk.Domain.Enums.UserRole.Soporte)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email
            })
            .ToListAsync();

        return Ok(users);
    }
}