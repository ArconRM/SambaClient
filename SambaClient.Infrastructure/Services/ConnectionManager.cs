using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using SambaClient.Core.DTOs;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.DTOs.Responses;
using SambaClient.Core.Entities;
using SambaClient.Core.Exceptions;
using SambaClient.Infrastructure.Services.Interfaces;
using SMBLibrary;
using SMBLibrary.Client;

namespace SambaClient.Infrastructure.Services;

public class ConnectionManager : ISmbConnectionManager
{
    private static readonly string ConnectionsFilePath = Path.Combine(
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
        var directory = Path.GetDirectoryName(ConnectionsFilePath);
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
            ShareName = request.ShareName,
            Username = request.Username,
            Password = request.Password
        };

        var connections = await LoadConnectionsAsync(token);
        connections.Add(smbServerConnection);

        await SaveConnectionsAsync(connections, token);
        return smbServerConnection;
    }

    public async Task<List<SmbServerConnection>> LoadConnectionsAsync(CancellationToken token)
    {
        if (!File.Exists(ConnectionsFilePath))
        {
            return [];
        }

        var fileInfo = new FileInfo(ConnectionsFilePath);
        if (fileInfo.Length == 0)
        {
            return [];
        }

        try
        {
            await using Stream connectionFileStream = File.OpenRead(ConnectionsFilePath);

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
        connectionToUpdate.ShareName = connection.ShareName;
        connectionToUpdate.Username = connection.Username;
        connectionToUpdate.Password = connection.Password;

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

    public async Task<ConnectionResponse> TestConnectionAsync(TestConnectionRequest request, CancellationToken token)
    {
        var (response, client) = await ConnectHelperAsync(
            request.Host,
            request.Username,
            request.Password,
            listShares: true,
            token);

        try
        {
            return response;
        }
        finally
        {
            client.Disconnect();
        }
    }

    public async Task<ConnectionResponse> ConnectAsync(Guid connectionUuid, CancellationToken token)
    {
        var connection = await GetConnectionAsync(connectionUuid, token);
        var (response, client) = await ConnectHelperAsync(
            connection.Host,
            connection.Username,
            connection.Password,
            listShares: false,
            token);

        if (!response.IsSuccess)
        {
            client.Disconnect();
        }

        return response;
    }


    private async Task SaveConnectionsAsync(List<SmbServerConnection> connections, CancellationToken token)
    {
        await using var fileStream = File.Create(ConnectionsFilePath);
        await JsonSerializer.SerializeAsync(fileStream, connections, _jsonSerializerOptions, token);
    }

    private async Task<(ConnectionResponse Response, SMB2Client Client)> ConnectHelperAsync(
        string host,
        string username,
        string password,
        bool listShares,
        CancellationToken token)
    {
        var client = _smbClientProvider.GetSambaClient();
        client.Disconnect();
        
        try
        {
            if (!IPAddress.TryParse(host, out var ipAddress))
            {
                var hostEntry = await Dns.GetHostEntryAsync(host, token);
                ipAddress = hostEntry.AddressList.FirstOrDefault();
                if (ipAddress == null)
                {
                    return (new ConnectionResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "Could not resolve hostname"
                    }, client);
                }
            }

            if (!client.Connect(ipAddress, SMBTransportType.DirectTCPTransport))
            {
                return (new ConnectionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to connect to SMB server"
                }, client);
            }

            var loginStatus = client.Login(string.Empty, username, password);
            if (loginStatus != NTStatus.STATUS_SUCCESS)
            {
                return (new ConnectionResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Authentication failed: {loginStatus}"
                }, client);
            }

            List<string>? shares = null;
            if (listShares)
            {
                shares = client.ListShares(out _);
            }

            return (new ConnectionResponse
            {
                IsSuccess = true,
                Shares = shares
            }, client);
        }
        catch (SocketException ex)
        {
            return (new ConnectionResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Network error: {ex.Message}"
            }, client);
        }
        catch (Exception ex)
        {
            return (new ConnectionResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            }, client);
        }
    }
}