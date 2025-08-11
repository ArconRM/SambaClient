namespace SambaClient.Core.Entities;

public class SmbServerConnection
{
    public Guid Uuid { get; set; }
    
    public string Name { get; set; }
    
    public string Host { get; set; }
    
    public string Username { get; set; }
    
    public string Password { get; set; }
    
    public bool IsConnected { get; set; }
}