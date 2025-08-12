using SambaClient.Core.Entities;

namespace SambaClient.Core.DTOs;

public class ConnectionResponse
{
    public bool IsSuccess { get; set; }

    public string? ErrorMessage { get; set; }

    public List<string> Shares { get; set; }
}