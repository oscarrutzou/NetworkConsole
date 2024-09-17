using MessagePack;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using UDPGameServer;

namespace GameClient;

static class GameClientMain
{
    #region UDP Parameters
    private static UdpClient _udpClient = new UdpClient();
    private static IPAddress _serverIP = IPAddress.Parse("127.0.0.1"); // Lokal IP, men kunne også være en IP på en server
    private static int _serverPort = 1234;
    private static IPEndPoint _endPoint = new IPEndPoint(_serverIP, _serverPort);
    private static IPEndPoint _serverEndPoint = new IPEndPoint(_serverIP, _serverPort);
    #endregion

    static void Main(string[] args)
    {
        Thread heartbeatThread = new Thread(HeartBeatMessage);
        heartbeatThread.IsBackground = true;
        heartbeatThread.Start();

        Thread receiveMsg = new Thread(ReceiveMessages);
        receiveMsg.IsBackground = true;
        receiveMsg.Start();

        Update();
    }
    
    static void Update()
    {
        string pattern = @"^\d+\s+\d+\s+\d+\s+\d+$";

        while (true)
        {
            try
            {
                Console.Clear();
                Console.WriteLine("Skriv en besked til serveren:");

                string input = Console.ReadLine();
                //int[] numbers;

                //// Write a pos
                //if (!Regex.IsMatch(input, pattern)) continue;

                //string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //numbers = parts.Select(int.Parse).ToArray();

                // Send
                //grid.MoveObject(numbers[0], numbers[1], numbers[2], numbers[3]);

                byte[] sendData = Encoding.ASCII.GetBytes(input);
                _udpClient.Send(sendData, _endPoint);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
                Thread.Sleep(1000);

                // Handle the exception (e.g., retry, log, etc.)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Thread.Sleep(1000);
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
            Thread.Sleep(100);
            _udpClient.Send(sendData, _endPoint);
        }
    }

    static void ReceiveMessages()
    {
        while (true)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = _udpClient.Receive(ref remoteEndPoint);
                MessageType messageType = (MessageType)receivedData[0];
                byte[] dataToDeserialize = receivedData.Skip(1).ToArray();

                switch (messageType)
                {
                    case MessageType.StartGame:
                        break;
                    
                    case MessageType.HeartBeat:
                        break;
                    
                    case MessageType.MovePosition:
                        break;

                    case MessageType.ServerMsg:
                        ServerMsg serverMsg = MessagePackSerializer.Deserialize<ServerMsg>(dataToDeserialize);
                        Console.WriteLine($"Receive Serveren: {serverMsg.Message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Receive Exception: {ex.Message}");
            }
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