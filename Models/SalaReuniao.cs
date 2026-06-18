using System.ComponentModel.DataAnnotations;

namespace SalaReuniaoApi.Models;

public class SalaReuniao
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public int Capacidade { get; set; }

    public bool PossuiProjetor { get; set; }
}
