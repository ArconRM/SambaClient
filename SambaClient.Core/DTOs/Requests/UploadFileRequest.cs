namespace SambaClient.Core.DTOs.Requests;

public class UploadFileRequest: FileRequest
{
    public bool OverwriteIfExists { get; set; }
}