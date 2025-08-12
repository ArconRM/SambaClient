using SambaClient.Core.Entities;

namespace SambaClient.Core.DTOs.Responses;

public class GetFilesResponse: BaseResponse
{
    public List<FileEntity> Files { get; set; }
}