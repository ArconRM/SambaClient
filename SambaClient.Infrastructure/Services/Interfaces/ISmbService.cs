using SambaClient.Core.DTOs;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.DTOs.Responses;
using SambaClient.Core.Entities;

namespace SambaClient.Infrastructure.Services.Interfaces;

public interface ISmbService
{
    Task<GetFilesResponse> GetAllFilesAsync(Guid connectionUuid, string innerPath, CancellationToken token);
    
    Task<DownloadFileResponse> DownloadFileAsync(FileRequest request, CancellationToken token);
    
    Task<BaseResponse> UploadFileAsync(UploadFileRequest request, CancellationToken token);
    
    Task<BaseResponse> UpdateFileNameAsync(UpdateFilePathRequest request, CancellationToken token);
    
    Task<BaseResponse> CreateFolderAsync(FileRequest request, CancellationToken token);
    
    Task<BaseResponse> MoveFileAsync(UpdateFilePathRequest request, CancellationToken token);
    
    Task<BaseResponse> DeleteFileAsync(FileRequest request, CancellationToken token);
}