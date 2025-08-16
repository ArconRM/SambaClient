namespace SambaClient.App.Messages;

public class CreateNewFolderCloseMessage(string folderName)
{
    public string FolderName { get; set; } = folderName;
}