using SambaClient.Core.Entities;

namespace SambaClient.Core.DTOs.Requests;

public class FileRequest
{
    public Guid ConnectionUuid { get; set; }

    public FileEntity FileEntity { get; set; }
}