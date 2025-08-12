namespace SambaClient.Core.Entities;

public class FileEntity
{
    public uint FileIndex { get; set; }
    public string FileName { get; set; }

    public bool IsDirectory { get; set; }

    public long Size { get; set; }

    public DateTime ModifiedDate { get; set; }
}