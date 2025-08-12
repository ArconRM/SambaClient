namespace SambaClient.Core.DTOs.Responses;

public class BaseResponse
{
    public bool IsSuccess { get; set; }

    public string? ErrorMessage { get; set; }
}