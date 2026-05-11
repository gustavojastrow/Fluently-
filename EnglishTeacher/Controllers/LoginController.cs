using EnglishTeacher.Models;
using EnglishTeacher.Services;
using EnglishTeacher.Services.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishTeacher.Controllers;

public class LoginController(TokenService tokenService, SqlUserService userService) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("JwtToken") != null)
        {
            return RedirectToAction("Teacher", "Teacher");
        }

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Login");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Authenticate([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Senha))
            {
                return BadRequest(new { message = "Usuario e senha sao obrigatorios." });
            }

            var user = await userService.GetByLoginAsync(request.Login);
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Senha, user.SenhaHash))
            {
                return Unauthorized(new { message = "Credenciais invalidas." });
            }

            if (user.Tipo != 2)
            {
                return Unauthorized(new { message = "Este usuario nao esta autorizado para o sistema." });
            }

            var token = tokenService.GenerateToken(user.Usuario, user.Cargo);

            HttpContext.Session.SetString("JwtToken", token);
            HttpContext.Session.SetString("UserLogin", user.Usuario);
            HttpContext.Session.SetString("UserRole", user.Cargo);
            HttpContext.Session.SetString("UserTipo", user.Tipo.ToString());

            return Ok(new
            {
                token,
                message = "Login bem-sucedido.",
                user = new
                {
                    login = user.Usuario,
                    role = user.Cargo,
                    tipo = user.Tipo
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no login: {ex.Message}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Usuario) ||
                string.IsNullOrWhiteSpace(request.Senha) ||
                string.IsNullOrWhiteSpace(request.Cargo))
            {
                return BadRequest(new { message = "Usuario, senha e cargo sao obrigatorios." });
            }

            if (request.Senha.Length < 6)
            {
                return BadRequest(new { message = "A senha deve ter pelo menos 6 caracteres." });
            }

            var existingUser = await userService.GetByLoginAsync(request.Usuario);
            if (existingUser is not null)
            {
                return Conflict(new { message = "Este usuario ja esta cadastrado." });
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Senha);
            var id = await userService.CreateAsync(
                request.Usuario,
                hashedPassword,
                request.Cargo,
                request.Tipo ?? 2,
                request.Email,
                request.Nome ?? request.Usuario);

            return Ok(new
            {
                message = "Usuario registrado com sucesso.",
                user = new
                {
                    id,
                    login = request.Usuario,
                    role = request.Cargo,
                    tipo = request.Tipo ?? 2
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no registro: {ex.Message}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }
}

public class RegisterRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public int? Tipo { get; set; }
    public string? Email { get; set; }
    public string? Nome { get; set; }
}
