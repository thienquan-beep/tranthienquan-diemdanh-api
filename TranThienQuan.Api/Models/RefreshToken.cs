namespace TranThienQuan.Api.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Token { get; set; } = "";
    public DateTime HetHan { get; set; }
    public bool DaThuHoi { get; set; }
}
