namespace SambaClient.Core.DTOs.Requests;

public class UpdateFileNameRequest: FileRequest
{
    public string NewRemoteTargetPath { get; set; }
}