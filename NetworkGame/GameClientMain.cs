using MessagePack;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using UDPGameServer;

namespace GameClient;

public static class GameClientMain
{
    #region UDP Parameters
    private static UdpClient _udpClient = new UdpClient();
    private static IPAddress _serverIP = IPAddress.Parse("127.0.0.1"); // Lokal IP, men kunne også være en IP på en server
    private static int _serverPort = 1234;
    private static IPEndPoint _endPoint;

    private static Grid _localGameGrid;
    private static string _turnMsg = "";

    private static object _writeLock = new object();
    #endregion

    public static void Main(string[] args)
    {
        // Send start msg
        _endPoint = new IPEndPoint(_serverIP, _serverPort);

        Thread heartbeatThread = new Thread(HeartBeatMessage);
        heartbeatThread.IsBackground = true;
        heartbeatThread.Start();

        Thread receiveMsg = new Thread(ReceiveMessages);
        receiveMsg.IsBackground = true;
        receiveMsg.Start();

        Update();
    }

    static void SendStartMsg()
    {
        RequestAddClientMsg requestAddClientMsg = new RequestAddClientMsg();
        SendMessage(requestAddClientMsg, _endPoint);
    }
    static void DrawGrid()
    {
        Console.Write("\x1b[3J"); // Clear the scrollback buffer
        Console.Clear();
        _localGameGrid.DrawGrid();
        Console.WriteLine("DRAWING GRID");

        Console.WriteLine(_turnMsg);
        Console.WriteLine("UPDATE Write the old input {X,Y} {NewX,NewY}:");
    }
    static void Update()
    {
        string pattern = @"^\d+\s+\d+\s+\d+\s+\d+$";

        while (true)
        {
            try
            {
                if (_localGameGrid == null) continue;

                lock (_writeLock)
                {
                    DrawGrid();
                }

                string input = Console.ReadLine();
                int[] numbers;

                // Write a pos
                if (!Regex.IsMatch(input, pattern)) continue;

                string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                numbers = parts.Select(int.Parse).ToArray();

                RequestMovePosMsg requestMovePosMsg = new RequestMovePosMsg() { PrevPos = new Point(numbers[0], numbers[1]), NewTargetPos = new Point(numbers[2], numbers[3]) };
                SendMessage(requestMovePosMsg, _endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }

    #region UDP

    static void HeartBeatMessage()
    {
        byte[] combinedBytes = null;
        HeartBeatMsg heartBeatMsg = new HeartBeatMsg();
        while (true)
        {
            Thread.Sleep(100);
            SendStartMsg();

            if (combinedBytes == null)
                combinedBytes = SendMessage(heartBeatMsg, _endPoint);
            else
                SendRepeatMessage(combinedBytes, _endPoint);
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
                    // Have the start grid
                    case MessageType.StartGame:
                        StartGameMsg startMsg = MessagePackSerializer.Deserialize<StartGameMsg>(dataToDeserialize);
                        break;

                    case MessageType.UpdateGrid:
                        UpdateGridMsg updateMsg = MessagePackSerializer.Deserialize<UpdateGridMsg>(dataToDeserialize);

                        lock (_writeLock)
                        {
                            _localGameGrid = new Grid(updateMsg.GridSize.X, updateMsg.GridSize.Y);
                            _localGameGrid.CharacterGrid = updateMsg.GameGridArray;
                            DrawGrid();
                        }

                        break;

                    case MessageType.TurnMsg:
                        TurnMsg turnMsg = MessagePackSerializer.Deserialize<TurnMsg>(dataToDeserialize);
                        _turnMsg = turnMsg.Message;

                        lock (_writeLock)
                        {
                            if (_localGameGrid == null) break;
                            DrawGrid();
                        }

                        break;

                    case MessageType.ServerMsg:
                        ServerMsg serverMsg = MessagePackSerializer.Deserialize<ServerMsg>(dataToDeserialize);

                        lock (_writeLock)
                        {
                            Console.WriteLine($"Server msg: {serverMsg.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Receive Exception: {ex.Message}");
                //lock (_writeLock)
                //{
                //}
            }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="endPoint"></param>
    /// <returns>A combined byte that can be used to copy </returns>
    static byte[] SendMessage(NetworkMessage message, IPEndPoint endPoint)
    {
        byte[] messageBytes = new byte[1024];
        byte messageTypeByte = message.GetMessageTypeAsByte;

        switch (message.MessageType)
        {
            case MessageType.RequestMovePosition:
                messageBytes = MessagePackSerializer.Serialize((RequestMovePosMsg)message);
                break;
            case MessageType.HeartBeat:
                messageBytes = MessagePackSerializer.Serialize((HeartBeatMsg)message);
                break;
            case MessageType.ServerMsg:
                messageBytes = MessagePackSerializer.Serialize((ServerMsg)message);
                break;
            default:
                break;
        }

        byte[] combinedBytes = new byte[1 + messageBytes.Length];
        combinedBytes[0] = messageTypeByte;
        Buffer.BlockCopy(messageBytes, 0, combinedBytes, 1, messageBytes.Length);
        _udpClient.Send(combinedBytes, endPoint);

        return combinedBytes;
    }

    static void SendRepeatMessage(byte[] combinedBytes, IPEndPoint endPoint)
    {
        _udpClient.Send(combinedBytes, endPoint);
    }

    #endregion
}
/*  First open then wait for players
When 2 players have joined,
Open the new console    
We ask for the username
Join on the chat tcp server, is shown from the other console.
*/