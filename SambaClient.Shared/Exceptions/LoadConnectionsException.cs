namespace SambaClient.Shared.Exceptions;

public class LoadConnectionsException : Exception
{
    public LoadConnectionsException(string? message) : base(message) { }
}