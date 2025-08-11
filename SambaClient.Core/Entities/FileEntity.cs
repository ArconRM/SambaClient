namespace SambaClient.Core.Entities;

public class FileEntity
{
    public string FileName { get; set; }

    public string FullPath { get; set; }

    public bool IsDirectory { get; set; }

    public long Size { get; set; }

    public DateTime ModifiedDate { get; set; }
}