namespace SambaClient.Core.DTOs.Requests;

public class UploadFileRequest: FileRequest
{
    public Stream SourceStream { get; set; }
    
    public bool OverwriteIfExists { get; set; }
}