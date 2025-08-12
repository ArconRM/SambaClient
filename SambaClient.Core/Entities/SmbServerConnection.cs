using System.Text.Json.Serialization;
using SMBLibrary.Client;

namespace SambaClient.Core.Entities;

public class SmbServerConnection: ServerConnection
{
    public string Name { get; set; }
    
    public string Host { get; set; }
    
    public string ShareName { get; set; }
    
    public string Username { get; set; }
    
    public string Password { get; set; }
}