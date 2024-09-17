using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameClient;

static class GameClientMain
{
    #region UDP Parameters
    private static UdpClient _udpClient = new UdpClient();
    private static IPAddress _serverIP = IPAddress.Parse("127.0.0.1"); // Lokal IP, men kunne også være en IP på en server
    private static int _serverPort = 1234;
    private static IPEndPoint _endPoint = new IPEndPoint(_serverIP, _serverPort);
    #endregion

    static void Main(string[] args)
    {
        Thread heartbeatThread = new Thread(HeartBeatMessage);
        heartbeatThread.IsBackground = true;
        heartbeatThread.Start();


        Update();
    }
    
    static void Update()
    {
        while (true)
        {
            try
            {
                Console.WriteLine("Skriv en besked til serveren:");
                string message = Console.ReadLine();
                byte[] sendData = Encoding.ASCII.GetBytes(message);
                _udpClient.Send(sendData, _endPoint);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
                // Handle the exception (e.g., retry, log, etc.)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                // Handle other exceptions
            }
        }
    }

    #region UDP

    static void HeartBeatMessage()
    {
        string heartbeatMsg = "heartbeat";
        byte[] sendData = Encoding.ASCII.GetBytes(heartbeatMsg);
        while (true)
        {
            Thread.Sleep(500);
            _udpClient.Send(sendData, _endPoint);
        }
    }

    #endregion
}
/*  First open then wait for players
When 2 players have joined,
Open the new console    
We ask for the username
Join on the chat tcp server, is shown from the other console.
*/