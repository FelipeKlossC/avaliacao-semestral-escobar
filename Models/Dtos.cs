namespace SalaReuniaoApi.Models;

// Dados enviados pelo cliente no login
public record LoginRequest(string Email, string Senha);

// Resposta do login com o token JWT
public record LoginResponse(string Token);

// Dados enviados pelo cliente ao criar/atualizar uma sala
public record SalaRequest(string Nome, int Capacidade, bool PossuiProjetor);
