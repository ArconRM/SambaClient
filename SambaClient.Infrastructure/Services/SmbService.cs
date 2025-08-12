using SambaClient.Core.DTOs;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.DTOs.Responses;
using SambaClient.Core.Entities;
using SambaClient.Infrastructure.Services.Interfaces;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace SambaClient.Infrastructure.Services;

public class SmbService : ISmbService
{
    private readonly ISmbConnectionManager _connectionManager;
    private readonly ISmbClientProvider _clientProvider;

    public SmbService(
        ISmbConnectionManager connectionManager,
        ISmbClientProvider clientProvider)
    {
        _connectionManager = connectionManager;
        _clientProvider = clientProvider;
    }

    public async Task<GetFilesResponse> GetAllFilesAsync(Guid connectionUuid, CancellationToken token)
    {
        try
        {
            var fileStore = await GetVerifiedClientAsync(connectionUuid, token)
                            ?? throw new InvalidOperationException("No active share for this connection.");
            object directoryHandle;
            var status = fileStore.CreateFile(out directoryHandle, out _, String.Empty, AccessMask.GENERIC_READ, FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
            if (status == NTStatus.STATUS_SUCCESS)
            {
                List<QueryDirectoryFileInformation> fileList;
                status = fileStore.QueryDirectory(out fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                status = fileStore.CloseFile(directoryHandle);

                return new GetFilesResponse()
                {
                    IsSuccess = true,
                    Files = fileList
                        .OfType<FileDirectoryInformation>()
                        .Select(f => new FileEntity()
                        {
                            FileIndex = f.FileIndex,
                            FileName = f.FileName,
                            Size = f.EndOfFile,
                            IsDirectory = (f.FileAttributes & FileAttributes.Directory) != 0,
                            ModifiedDate = f.ChangeTime
                        }).ToList()
                };
            }
            else
            {
                return new GetFilesResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to list directory: {status}",
                };
            }
        }
        catch (Exception ex)
        {
            return new GetFilesResponse()
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    
    private async Task<ISMBFileStore> GetVerifiedClientAsync(Guid connectionUuid, CancellationToken token)
    {
        var connection = await _connectionManager.GetConnectionAsync(connectionUuid, token);
        var client = _clientProvider.GetSambaClient();

        if (!client.IsConnected)
            throw new InvalidOperationException("Connection is not active.");

        var fileStore = client.TreeConnect(connection.ShareName, out var shareStatus);
        if (shareStatus != NTStatus.STATUS_SUCCESS)
            throw new InvalidOperationException($"Connection to share is broken: {shareStatus}");

        // var status = fileStore?.QueryDirectory(out _, null, "*", FileInformationClass.FileDirectoryInformation);
        // if (status != NTStatus.STATUS_SUCCESS)
        //     throw new InvalidOperationException($"Share not accessible: {status}");

        return fileStore;
    }

    public async Task<Stream> DownloadFileAsync(FileRequest request, CancellationToken token)
    {
        var client = await GetVerifiedClientAsync(request.ConnectionUuid, token);
        throw new NotImplementedException();
    }

    public async Task<BaseResponse> UploadFileAsync(UploadFileRequest request, CancellationToken token)
    {
        var client = await GetVerifiedClientAsync(request.ConnectionUuid, token);
        throw new NotImplementedException();
    }

    public async Task<BaseResponse> DeleteFileAsync(FileRequest request, CancellationToken token)
    {
        var client = await GetVerifiedClientAsync(request.ConnectionUuid, token);
        throw new NotImplementedException();
    }
}