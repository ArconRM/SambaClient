namespace SambaClient.Core.DTOs;

public class UploadFileRequest
{
    public string ConnectionId { get; set; }
    
    public string LocalFilePath { get; set; }
    
    public string RemoteFilePath { get; set; }
    
    public bool OverwriteIfExists { get; set; }
}