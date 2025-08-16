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


    public async Task<GetFilesResponse> GetAllFilesAsync(Guid connectionUuid,
        string innerPath,
        CancellationToken token)
    {
        try
        {
            var fileStore = await GetVerifiedClientAsync(connectionUuid, token)
                            ?? throw new InvalidOperationException("No active share for this connection.");
            object directoryHandle;
            var status = fileStore.CreateFile(out directoryHandle,
                out _,
                innerPath,
                AccessMask.GENERIC_READ,
                FileAttributes.Directory,
                ShareAccess.Read | ShareAccess.Write,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            if (status == NTStatus.STATUS_SUCCESS)
            {
                List<QueryDirectoryFileInformation> fileList;

                status = fileStore.QueryDirectory(out fileList,
                    directoryHandle,
                    "*",
                    FileInformationClass.FileDirectoryInformation);
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

            return new GetFilesResponse()
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to list directory: {status}",
            };
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

    public async Task<Stream> DownloadFileAsync(FileRequest request, CancellationToken token)
    {
        var client = await GetVerifiedClientAsync(request.ConnectionUuid, token);
        throw new NotImplementedException();
    }

    public async Task<BaseResponse> UploadFileAsync(UploadFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var fileStore = await GetVerifiedClientAsync(request.ConnectionUuid, cancellationToken);
            var path = request.TargetRemotePath;

            var status = fileStore.CreateFile(
                out var fileHandle,
                out _,
                path,
                AccessMask.GENERIC_WRITE,
                FileAttributes.Normal,
                ShareAccess.None,
                request.OverwriteIfExists ? CreateDisposition.FILE_OVERWRITE_IF : CreateDisposition.FILE_CREATE,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT,
                null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                return new BaseResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Unable to open or create remote file: {status}"
                };
            }

            long offset = 0;
            var client = _clientProvider.GetSambaClient();
            var buffer = new byte[client.MaxWriteSize];
            int bytesRead;

            while ((bytesRead = await request.SourceStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                status = fileStore.WriteFile(out _, fileHandle, offset, buffer.Take(bytesRead).ToArray());
                if (status != NTStatus.STATUS_SUCCESS)
                    throw new IOException($"Write failed at offset {offset}: {status}");

                offset += bytesRead;
            }

            fileStore.CloseFile(fileHandle);

            return new BaseResponse { IsSuccess = true };
        }
        catch (Exception ex)
        {
            return new BaseResponse
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<BaseResponse> DeleteFileAsync(FileRequest request, CancellationToken token)
    {
        var client = await GetVerifiedClientAsync(request.ConnectionUuid, token);
        throw new NotImplementedException();
    }
}