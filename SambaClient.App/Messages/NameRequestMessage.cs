using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SambaClient.App.Messages;

public class NameRequestMessage(string defaultName = "") : AsyncRequestMessage<string?>
{
    public string DefaultName { get; init; } = defaultName;
}