namespace mapos_dotnet.Models;

public class ResetSenha
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime DataExpiracao { get; set; }
    public bool TokenUtilizado { get; set; }
}
