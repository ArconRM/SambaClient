namespace SambaClient.Core.DTOs.Responses;

public class DownloadFileResponse : BaseResponse
{
    public Stream Stream { get; set; }
}