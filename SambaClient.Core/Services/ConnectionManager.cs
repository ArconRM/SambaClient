using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using SambaClient.Core.DTOs;
using SambaClient.Core.Entities;
using SambaClient.Core.Services.Interfaces;
using SambaClient.Infrastructure.Services.Interfaces;
using SambaClient.Shared.Exceptions;
using SMBLibrary;

namespace SambaClient.Core.Services;

public class ConnectionManager : IConnectionManager
{
    private static readonly string _connectionsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SambaClient",
        "connections.json"
    );

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ISmbClientProvider _smbClientProvider;

    public ConnectionManager(ISmbClientProvider smbClientProvider)
    {
        _smbClientProvider = smbClientProvider;
        EnsureDirectoryExists();
    }

    private static void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_connectionsFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task<SmbServerConnection> AddNewConnectionAsync(
        CreateConnectionRequest request, CancellationToken token)
    {
        SmbServerConnection smbServerConnection = new SmbServerConnection()
        {
            Uuid = Guid.NewGuid(),
            Name = request.Name,
            Host = request.Host,
            Username = request.Username,
            Password = request.Password,
            IsConnected = false
        };

        var connections = await LoadConnectionsAsync(token);
        connections.Add(smbServerConnection);

        await SaveConnectionsAsync(connections, token);
        return smbServerConnection;
    }

    public async Task<List<SmbServerConnection>> LoadConnectionsAsync(CancellationToken token)
    {
        if (!File.Exists(_connectionsFilePath))
        {
            return [];
        }

        var fileInfo = new FileInfo(_connectionsFilePath);
        if (fileInfo.Length == 0)
        {
            return [];
        }

        try
        {
            await using Stream connectionFileStream = File.OpenRead(_connectionsFilePath);

            var loadedServerConnections = await JsonSerializer.DeserializeAsync<List<SmbServerConnection>>(
                connectionFileStream,
                _jsonSerializerOptions,
                token);

            return loadedServerConnections ?? [];
        }
        catch (JsonException ex)
        {
            throw new LoadConnectionsException($"Invalid JSON in connections file: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new LoadConnectionsException($"Failed to load connections: {ex.Message}");
        }
    }

    public async Task<SmbServerConnection> GetConnectionAsync(Guid connectionId, CancellationToken token)
    {
        var loadedServerConnections = await LoadConnectionsAsync(token);
        return loadedServerConnections.FirstOrDefault(x => x.Uuid == connectionId);
    }

    public async Task<SmbServerConnection> UpdateConnectionAsync(
        SmbServerConnection connection, CancellationToken token)
    {
        var connections = await LoadConnectionsAsync(token);

        var connectionToUpdate = connections.FirstOrDefault(x => x.Uuid == connection.Uuid);
        if (connectionToUpdate is null)
            return connection;

        connectionToUpdate.Name = connection.Name;
        connectionToUpdate.Host = connection.Host;
        connectionToUpdate.Username = connection.Username;
        connectionToUpdate.Password = connection.Password;
        connectionToUpdate.IsConnected = connection.IsConnected;

        await SaveConnectionsAsync(connections, token);

        return connectionToUpdate;
    }

    public async Task RemoveConnectionAsync(Guid connectionUuid, CancellationToken token)
    {
        var connections = await LoadConnectionsAsync(token);

        var connectionToRemove = connections.FirstOrDefault(x => x.Uuid == connectionUuid);
        if (connectionToRemove is null)
            return;

        connections.Remove(connectionToRemove);
        await SaveConnectionsAsync(connections, token);
    }

    public async Task<SmbConnectionResponse> TestConnectionAsync(TestConnectionRequest request, CancellationToken token)
    {
        var client = _smbClientProvider.GetSambaClient();

        try
        {
            if (!IPAddress.TryParse(request.Host, out var ipAddress))
            {
                var hostEntry = await Dns.GetHostEntryAsync(request.Host, token);
                ipAddress = hostEntry.AddressList.FirstOrDefault();
                if (ipAddress == null)
                {
                    return new SmbConnectionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "Could not resolve hostname"
                    };
                }
            }

            bool connected = client.Connect(ipAddress, SMBTransportType.DirectTCPTransport);
            if (!connected)
            {
                return new SmbConnectionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to connect to SMB server"
                };
            }

            NTStatus loginStatus = client.Login(string.Empty, request.Username, request.Password);
            if (loginStatus != NTStatus.STATUS_SUCCESS)
            {
                return new SmbConnectionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Authentication failed: {loginStatus}"
                };
            }

            List<string> shareNames = client.ListShares(out NTStatus shareStatus);

            return new SmbConnectionResponse
            {
                IsSuccess = true,
                Shares = shareNames,
            };
        }
        catch (SocketException ex)
        {
            return new SmbConnectionResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new SmbConnectionResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public Task<SmbConnectionResponse> ConnectAsync(Guid connectionUuid, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    private async Task SaveConnectionsAsync(List<SmbServerConnection> connections, CancellationToken token)
    {
        await using var fileStream = File.Create(_connectionsFilePath);
        await JsonSerializer.SerializeAsync(fileStream, connections, _jsonSerializerOptions, token);
    }
}