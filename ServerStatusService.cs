using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AktualizatorEME.Services
{
    public class ServerStatusService
    {
        private readonly string _ip;
        private readonly int _port;

        public ServerStatusService(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public async Task<bool> IsServerOnline()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // Próba połączenia z krótkim timeoutem (3 sekundy)
                    var connectTask = client.ConnectAsync(_ip, _port);
                    var timeoutTask = Task.Delay(3000);

                    if (await Task.WhenAny(connectTask, timeoutTask) == connectTask)
                    {
                        await connectTask; // rzuci wyjątek jeśli połączenie się nie udało
                        return true;
                    }
                    return false; // Timeout
                }
            }
            catch
            {
                return false;
            }
        }
    }
}