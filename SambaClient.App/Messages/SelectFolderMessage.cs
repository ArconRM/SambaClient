using CommunityToolkit.Mvvm.Messaging.Messages;
using SambaClient.Core.Entities;

namespace SambaClient.App.Messages;

public class SelectFolderMessage(SmbServerConnection serverConnection) : AsyncRequestMessage<string?>
{
    public SmbServerConnection ServerConnection { get; init; } = serverConnection;
}