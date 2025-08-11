using SambaClient.Core.Entities;

namespace SambaClient.App.Messages;

public class AddConnectionCloseMessage(SmbServerConnection smbServerConnection)
{
    public SmbServerConnection SmbServerConnection { get; } = smbServerConnection;
}