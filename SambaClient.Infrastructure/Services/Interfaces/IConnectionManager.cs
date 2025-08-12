using SambaClient.Core.DTOs;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.DTOs.Responses;
using SambaClient.Core.Entities;

namespace SambaClient.Infrastructure.Services.Interfaces;

public interface IConnectionManager<T> where T: ServerConnection
{
    Task<T> AddNewConnectionAsync(CreateConnectionRequest request, CancellationToken token);
    Task<List<T>> LoadConnectionsAsync(CancellationToken token);
    Task<T> GetConnectionAsync(Guid connectionId, CancellationToken token);
    Task<T> UpdateConnectionAsync(T connection, CancellationToken token);
    Task RemoveConnectionAsync(Guid connectionUuid, CancellationToken token);

    Task<ConnectionResponse> TestConnectionAsync(TestConnectionRequest request, CancellationToken token);
    Task<ConnectionResponse> ConnectAsync(Guid connectionUuid, CancellationToken token);
}