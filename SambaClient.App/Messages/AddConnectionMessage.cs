using CommunityToolkit.Mvvm.Messaging.Messages;
using SambaClient.Core.Entities;

namespace SambaClient.App.Messages;

public class AddConnectionMessage: AsyncRequestMessage<SmbServerConnection?>;