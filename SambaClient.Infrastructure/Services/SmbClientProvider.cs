using SambaClient.Infrastructure.Services.Interfaces;
using SMBLibrary.Client;

namespace SambaClient.Infrastructure.Services;

public class SmbClientProvider : ISmbClientProvider, IDisposable
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private SMB2Client? _smbClient;

    public SMB2Client GetSambaClient()
    {
        try
        {
            _lock.Wait();
            if (_smbClient is null)
            {
                _smbClient = new SMB2Client();
            }
            return _smbClient;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (_smbClient is not null)
        {
            _smbClient.Disconnect();
            _smbClient.Logoff();
        }
    }
}