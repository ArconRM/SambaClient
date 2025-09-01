namespace SambaClient.App.Messages;

public class SelectFolderCloseMessage(string newPath)
{
    public string NewPath { get; set; } = newPath;
}