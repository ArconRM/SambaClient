using System.Collections.ObjectModel;
using SambaClient.Core.DTOs;
using SambaClient.Core.Entities;

namespace SambaClient.Core.Services.Interfaces;

public interface IConnectionManager
{
    Task<SmbServerConnection> AddNewConnectionAsync(CreateConnectionRequest request, CancellationToken token);
    Task<List<SmbServerConnection>> LoadConnectionsAsync(CancellationToken token);
    Task<SmbServerConnection> GetConnectionAsync(Guid connectionId, CancellationToken token);
    Task<SmbServerConnection> UpdateConnectionAsync(SmbServerConnection connection, CancellationToken token);
    Task RemoveConnectionAsync(Guid connectionUuid, CancellationToken token);

    Task<SmbConnectionResponse> TestConnectionAsync(TestConnectionRequest request, CancellationToken token);
    Task<SmbConnectionResponse> ConnectAsync(Guid connectionUuid, CancellationToken token);
}