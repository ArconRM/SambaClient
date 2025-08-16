using SambaClient.Core.Entities;

namespace SambaClient.Core.DTOs.Requests;

public class FileRequest
{
    public Guid ConnectionUuid { get; set; }

    // Относительный путь в share
    public string TargetRemotePath { get; set; }
}