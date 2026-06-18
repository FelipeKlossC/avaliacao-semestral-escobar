using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SalaReuniaoApi.Data;
using SalaReuniaoApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------- CONFIGURAÇÃO ----------------------------
// Usuário fixo (sem banco de usuários, conforme pedido na avaliação)
const string USUARIO_EMAIL = "teste@teste.com";
const string USUARIO_SENHA = "123";

// Chave secreta usada para assinar o token JWT (troque por algo mais seguro em produção)
const string JWT_KEY = "ChaveSecretaSuperSeguraParaAvaliacaoJWT123456";
const string JWT_ISSUER = "SalaReuniaoApi";

// ---------------------------- SERVIÇOS ----------------------------

// Banco de dados (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=salas.db"));

// Autenticação JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = JWT_ISSUER,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWT_KEY))
    };
});

builder.Services.AddAuthorization();

// CORS liberado para o front-end (index.html aberto direto no navegador)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTudo", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("PermitirTudo");

// Cria o banco e a tabela automaticamente se não existirem
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------- ROTA DE LOGIN ----------------------------
app.MapPost("/login", (LoginRequest login) =>
{
    if (login.Email != USUARIO_EMAIL || login.Senha != USUARIO_SENHA)
    {
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, login.Email)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWT_KEY));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: JWT_ISSUER,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new LoginResponse(tokenString));
});

// ---------------------------- ROTAS CRUD: SalasReuniao ----------------------------
// Todas exigem o header Authorization: Bearer {token}

// GET /salas - listar todas
app.MapGet("/salas", async (AppDbContext db) =>
{
    var salas = await db.SalasReuniao.ToListAsync();
    return Results.Ok(salas);
}).RequireAuthorization();

// GET /salas/{id} - buscar uma sala específica
app.MapGet("/salas/{id:int}", async (int id, AppDbContext db) =>
{
    var sala = await db.SalasReuniao.FindAsync(id);
    return sala is null ? Results.NotFound() : Results.Ok(sala);
}).RequireAuthorization();

// POST /salas - criar nova sala
app.MapPost("/salas", async (SalaRequest request, AppDbContext db) =>
{
    var sala = new SalaReuniao
    {
        Nome = request.Nome,
        Capacidade = request.Capacidade,
        PossuiProjetor = request.PossuiProjetor
    };

    db.SalasReuniao.Add(sala);
    await db.SaveChangesAsync();

    return Results.Created($"/salas/{sala.Id}", sala);
}).RequireAuthorization();

// PUT /salas/{id} - atualizar sala existente
app.MapPut("/salas/{id:int}", async (int id, SalaRequest request, AppDbContext db) =>
{
    var sala = await db.SalasReuniao.FindAsync(id);
    if (sala is null) return Results.NotFound();

    sala.Nome = request.Nome;
    sala.Capacidade = request.Capacidade;
    sala.PossuiProjetor = request.PossuiProjetor;

    await db.SaveChangesAsync();
    return Results.Ok(sala);
}).RequireAuthorization();

// DELETE /salas/{id} - excluir sala
app.MapDelete("/salas/{id:int}", async (int id, AppDbContext db) =>
{
    var sala = await db.SalasReuniao.FindAsync(id);
    if (sala is null) return Results.NotFound();

    db.SalasReuniao.Remove(sala);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

app.Run();
