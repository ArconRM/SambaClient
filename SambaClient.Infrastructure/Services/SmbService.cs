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


    private async Task<ISMBFileStore> GetVerifiedFileStoreAsync(Guid connectionUuid, CancellationToken token)
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

    public async Task<GetFilesResponse> GetAllFilesAsync(
        Guid connectionUuid,
        string innerPath,
        CancellationToken token)
    {
        try
        {
            var fileStore = await GetVerifiedFileStoreAsync(connectionUuid, token)
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

    public async Task<DownloadFileResponse> DownloadFileAsync(FileRequest request, CancellationToken token)
    {
        try
        {
            var fileStore = await GetVerifiedFileStoreAsync(request.ConnectionUuid, token);

            var status = fileStore.CreateFile(
                out var fileHandle,
                out var fileStatus,
                request.RemotePath,
                AccessMask.GENERIC_READ,
                FileAttributes.Normal,
                ShareAccess.Read,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT,
                null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new FileNotFoundException($"Unable to open remote file: {status}");
            }

            status = fileStore.GetFileInformation(out var fileInfo, fileHandle, FileInformationClass.FileStandardInformation);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                fileStore.CloseFile(fileHandle);
                throw new IOException($"Unable to get file information: {status}");
            }

            var standardInfo = (FileStandardInformation)fileInfo;
            var fileSize = standardInfo.EndOfFile;

            var memoryStream = new MemoryStream();

            try
            {
                var client = _clientProvider.GetSambaClient();
                var buffer = new byte[client.MaxReadSize];
                long offset = 0;

                while (offset < fileSize)
                {
                    var bytesToRead = (int)Math.Min(buffer.Length, fileSize - offset);

                    status = fileStore.ReadFile(out byte[] data, fileHandle, offset, bytesToRead);
                    if (status != NTStatus.STATUS_SUCCESS)
                    {
                        throw new IOException($"Read failed at offset {offset}: {status}");
                    }

                    if (data == null || data.Length == 0)
                        break;

                    await memoryStream.WriteAsync(data, 0, data.Length, token);
                    offset += data.Length;
                }

                memoryStream.Position = 0;
                return new DownloadFileResponse
                {
                    IsSuccess = true,
                    Stream = memoryStream
                };
            }
            catch (Exception ex)
            {
                await memoryStream.DisposeAsync();
                return new DownloadFileResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                fileStore.CloseFile(fileHandle);
            }
        }
        catch (Exception ex)
        {
            return new DownloadFileResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to download file: {ex.Message}"
            };
        }
    }
    
    public async Task<BaseResponse> UploadFileAsync(UploadFileRequest request, CancellationToken token)
    {
        try
        {
            var fileStore = await GetVerifiedFileStoreAsync(request.ConnectionUuid, token);
            var path = request.RemotePath;

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

            while ((bytesRead = await request.SourceStream.ReadAsync(buffer, token)) > 0)
            {
                status = fileStore.WriteFile(out _, fileHandle, offset, buffer.Take(bytesRead).ToArray());
                if (status != NTStatus.STATUS_SUCCESS)
                    throw new IOException($"Write failed at offset {offset}: {status}");

                offset += bytesRead;
            }

            fileStore.CloseFile(fileHandle);

            return new BaseResponse
            {
                IsSuccess = true
            };
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

    public async Task<BaseResponse> UpdateFileNameAsync(UpdateFilePathRequest request, CancellationToken token)
    {
        try
        {
            var fileStore = await GetVerifiedFileStoreAsync(request.ConnectionUuid, token);
            var oldPath = request.RemotePath;
            var newName = request.NewRemotePath;

            var status = fileStore.CreateFile(
                out var fileHandle,
                out _,
                oldPath,
                AccessMask.DELETE | AccessMask.GENERIC_WRITE,
                FileAttributes.Normal,
                ShareAccess.None,
                CreateDisposition.FILE_OPEN,
                request.IsDirectory ? CreateOptions.FILE_DIRECTORY_FILE : CreateOptions.FILE_NON_DIRECTORY_FILE,
                null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                return new BaseResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to open file for rename: {status}"
                };
            }

            var renameInfo = new FileRenameInformationType2
            {
                ReplaceIfExists = false,
                FileName = newName.StartsWith("\\") ? newName : "\\" + newName
            };

            status = fileStore.SetFileInformation(fileHandle, renameInfo);

            fileStore.CloseFile(fileHandle);

            return status == NTStatus.STATUS_SUCCESS
                ? new BaseResponse
                {
                    IsSuccess = true
                }
                : new BaseResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Rename failed: {status}"
                };
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

    public async Task<BaseResponse> CreateFolderAsync(FileRequest request, CancellationToken token)
    {
        try
        {
            var fileStore = await GetVerifiedFileStoreAsync(request.ConnectionUuid, token);
            var path = request.RemotePath;

            var status = fileStore.CreateFile(
                out var fileHandle,
                out _,
                path,
                AccessMask.GENERIC_WRITE,
                FileAttributes.Directory,
                ShareAccess.None,
                CreateDisposition.FILE_CREATE,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            if (status == NTStatus.STATUS_SUCCESS)
            {
                fileStore.CloseFile(fileHandle);
                return new BaseResponse
                {
                    IsSuccess = true
                };
            }

            return new BaseResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Unable to create remote directory: {status}"
            };
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

    public async Task<BaseResponse> MoveFileAsync(UpdateFilePathRequest request, CancellationToken token)
    {
        try
        {
            var fileStore = await GetVerifiedFileStoreAsync(request.ConnectionUuid, token);

            var oldPath = request.RemotePath;
            var newPath = request.NewRemotePath;

            var status = fileStore.CreateFile(
                out var fileHandle,
                out _,
                oldPath,
                AccessMask.DELETE | AccessMask.GENERIC_WRITE,
                FileAttributes.Normal,
                ShareAccess.None,
                CreateDisposition.FILE_OPEN,
                request.IsDirectory ? CreateOptions.FILE_DIRECTORY_FILE : CreateOptions.FILE_NON_DIRECTORY_FILE,
                null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                return new BaseResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to open file for moving: {status}"
                };
            }

            var normalizedNewPath = newPath.StartsWith("\\") ? newPath : "\\" + newPath;

            var renameInfo = new FileRenameInformationType2
            {
                ReplaceIfExists = true,
                FileName = normalizedNewPath
            };

            status = fileStore.SetFileInformation(fileHandle, renameInfo);

            fileStore.CloseFile(fileHandle);

            return status == NTStatus.STATUS_SUCCESS
                ? new BaseResponse
                {
                    IsSuccess = true
                }
                : new BaseResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Move failed: {status}"
                };
        }
        catch (Exception ex)
        {
            return new BaseResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to move file: {ex.Message}"
            };
        }
    }
    
    public async Task<BaseResponse> DeleteFileAsync(FileRequest request, CancellationToken token)
    {
        var fileStore = await GetVerifiedFileStoreAsync(request.ConnectionUuid, token);
        var remoteFilePath = request.RemotePath;

        var status = fileStore.CreateFile(
            out var fileHandle,
            out _,
            remoteFilePath,
            AccessMask.DELETE,
            FileAttributes.Normal,
            ShareAccess.None,
            CreateDisposition.FILE_OPEN,
            request.IsDirectory ? CreateOptions.FILE_DIRECTORY_FILE : CreateOptions.FILE_NON_DIRECTORY_FILE,
            null);

        if (status != NTStatus.STATUS_SUCCESS)
            return new BaseResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to open file: {status}"
            };

        FileDispositionInformation fileDispositionInformation = new FileDispositionInformation();
        fileDispositionInformation.DeletePending = true;
        status = fileStore.SetFileInformation(fileHandle, fileDispositionInformation);
        bool deleteSucceeded = (status == NTStatus.STATUS_SUCCESS);
        status = fileStore.CloseFile(fileHandle);

        return status == NTStatus.STATUS_SUCCESS && deleteSucceeded
            ? new BaseResponse
            {
                IsSuccess = true
            }
            : new BaseResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to delete file: {status}"
            };
    }
}