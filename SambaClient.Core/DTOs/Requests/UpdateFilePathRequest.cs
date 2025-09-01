namespace SambaClient.Core.DTOs.Requests;

public class UpdateFilePathRequest: FileRequest
{
    public string NewRemotePath { get; set; }
}