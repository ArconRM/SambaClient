using SMBLibrary.Client;

namespace SambaClient.Infrastructure.Services.Interfaces;

public interface ISmbClientProvider
{
    SMB2Client GetSambaClient();
}