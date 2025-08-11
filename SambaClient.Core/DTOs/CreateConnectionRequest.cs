namespace SambaClient.Core.DTOs;

public class CreateConnectionRequest
{
    public string Name { get; set; }
    
    public string Host { get; set; }
    
    public string Username { get; set; }
    
    public string Password { get; set; }
}