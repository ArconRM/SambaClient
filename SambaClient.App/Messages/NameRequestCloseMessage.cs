namespace SambaClient.App.Messages;

public class NameRequestCloseMessage(string folderName)
{
    public string FolderName { get; set; } = folderName;
}