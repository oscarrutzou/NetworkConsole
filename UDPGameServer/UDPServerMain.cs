
using System.Net.Sockets;
using System.Net;
using System.Text;
using MessagePack;

namespace UDPGameServer;

public class ClientData
{
    public Guid ClientID {  get; set; } 
    public IPEndPoint IPEndPoint { get; set; }
    public DateTime LastHeartBeat { get; set; }
}

public static class UDPServerMain
{
    static int _gridX = 5;
    static int _gridY = 5;
    static int _maxAmountOfPlayers = 2;
    static Grid _gameGrid;

    static UdpClient udpServer = new UdpClient(1234);
    static Dictionary<int, ClientData> Clients;
    private static Random _rnd = new();
    private static int _turnNmb;
    private static ClientData _currentTurnClientData;

    static RequestMovePosMsg? _requestMovePosMsg;
    static readonly object _clientLock = new object();
    static readonly object _moveRequestLock = new object();

    static float TargetFPS = 60; // 

    public static void Main(string[] args)
    {
        Console.WriteLine("I'm the UDP Game server\n");
        Clients = new();
        // Thread that deletes clients if they have lost connection
        Thread heartBeatDeleteWhenOffline = new Thread(HeartBeatDeleteWhenOffline);
        heartBeatDeleteWhenOffline.IsBackground = true;
        heartBeatDeleteWhenOffline.Start();

        // Gameloop only starts when it has enough clients
        Thread gameLoop = new Thread(GameUpdate);
        gameLoop.IsBackground = true;
        gameLoop.Start();

        ReceiveMessages();
    }

    static void ReceiveMessages()
    {
        while (true)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpServer.Receive(ref remoteEndPoint);
                MessageType messageType = (MessageType)receivedData[0];
                byte[] dataToDeserialize = receivedData.Skip(1).ToArray();

                switch (messageType)
                {
                    case MessageType.RequestAddClient:
                        // Add endpoint to a List
                        if (!Clients.Any(c => c.Value.IPEndPoint.Equals(remoteEndPoint)) && Clients.Count < _maxAmountOfPlayers)
                        {
                            ClientData data = new ClientData { IPEndPoint = remoteEndPoint, LastHeartBeat = DateTime.Now, ClientID = Guid.NewGuid() };
                            Clients.Add(Clients.Count, data);
                            string welcome = $"NEW CLIENT: {remoteEndPoint}:: ID: {data.ClientID}";
                            Console.WriteLine(welcome);
                            
                            ServerMsg serverMsgWelcom = new ServerMsg() { Message = "Welcome to " + welcome};
                            RelayMessageToAllUser(serverMsgWelcom);
                        }
                        break;

                    case MessageType.HeartBeat:
                        // Check if the client is in the list
                        var client = Clients.FirstOrDefault(c => c.Value.IPEndPoint.Equals(remoteEndPoint));

                        if (client.Equals(default(KeyValuePair<int, ClientData>)))
                        {
                            Console.WriteLine($"HeartBeat ERROR: Client {remoteEndPoint} not found in the list.");
                            break;
                        }

                        int clientIndex = client.Key;
                        Clients[clientIndex].LastHeartBeat = DateTime.Now;
                        break;

                    case MessageType.RequestMovePosition:

                        var senderClient = Clients.FirstOrDefault(c => c.Value.IPEndPoint.Equals(remoteEndPoint));
                        if (senderClient.Equals(default(KeyValuePair<int, ClientData>)))
                        {
                            Console.WriteLine($"RequestMovePosition ERROR: Client {remoteEndPoint} not found in the list.");
                            break;
                        }

                        lock (_moveRequestLock)
                        {
                            if (senderClient.Value.ClientID == _currentTurnClientData.ClientID)
                            {
                                // Only take the request if its their turn
                                _requestMovePosMsg = MessagePackSerializer.Deserialize<RequestMovePosMsg>(dataToDeserialize);
                            }
                        }
                        break;

                    case MessageType.ServerMsg:
                        ServerMsg serverMsg = MessagePackSerializer.Deserialize<ServerMsg>(dataToDeserialize);
                        Console.WriteLine($"From {remoteEndPoint}: {serverMsg.Message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive Exception: {ex.Message}");
            }
        }
    }


    // Ask for turns
    // Give aceess so the first client can change pos
    // Say to the other client whos turn it is. 
    // Wait for answer
    static void GameUpdate()
    {
        bool first = false;
        while (true)
        {
            Thread.Sleep(50);
            if (!_gameIsRunning) Thread.Sleep(1000);
            // Take time for it to be done, then turn sleep time up or down.
        
            if (Clients.Count != _maxAmountOfPlayers) continue;
            if (!first)
            {
                first = true;
                _turnNmb = 0;
                _currentTurnClientData = Clients[_turnNmb];
                StartGame();
                continue;
            }

            bool haveMoved = false;
            TryMoveData tryMoveData;
            GameMsg gameMsg = null;

            while (!haveMoved)
            {
                // Lock to limit so we dont get spammed with request for new positions.
                lock (_moveRequestLock)
                {
                    if (_requestMovePosMsg == null)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    Point prevPos = _requestMovePosMsg.PrevPos;
                    Point newTargetPoint = _requestMovePosMsg.NewTargetPos;

                    tryMoveData = _gameGrid.TryMoveObject(prevPos, newTargetPoint, _turnNmb);

                    if (tryMoveData.HasDealtDamage || tryMoveData.HasMoved)
                    {
                        string msgToAllUsers = $"User {_turnNmb}: {tryMoveData.ReturnMsg}";

                        gameMsg = new GameMsg() { Message = msgToAllUsers };
                        // Write message to all users
                        Console.WriteLine(msgToAllUsers);
                        haveMoved = true;
                        _requestMovePosMsg = null;
                    }
                    else
                    {
                        GameMsg gameMsgFail = new GameMsg() { Message = tryMoveData.ReturnMsg };
                        // A msg like "Cant move there" to the current turn client
                        SendMessage(gameMsgFail, _currentTurnClientData.IPEndPoint);
                        _requestMovePosMsg = null;
                    }
                }
            }

            // Updates grid too, for the players
            ChangeTurn();

            if (gameMsg != null)
            {
                RelayMessageToAllUser(gameMsg);
            }
            
        }
    }
    
    private static bool _gameIsRunning = true;

    static byte[] SendMessage(NetworkMessage message, IPEndPoint endPoint)
    {
        byte[] messageBytes = new byte[1024];
        byte messageTypeByte = message.GetMessageTypeAsByte;

        switch (message.MessageType)
        {
            case MessageType.StartGame:
                messageBytes = MessagePackSerializer.Serialize((StartGameMsg)message);
                break;
            case MessageType.UpdateGrid:
                messageBytes = MessagePackSerializer.Serialize((UpdateGridMsg)message);
                break;
            case MessageType.TurnMsg:
                messageBytes = MessagePackSerializer.Serialize((TurnMsg)message);
                break;
            case MessageType.ServerMsg:
                messageBytes = MessagePackSerializer.Serialize((ServerMsg)message);
                break;
            case MessageType.GameMsg:
                messageBytes = MessagePackSerializer.Serialize((GameMsg)message);
                break;
            case MessageType.StopGameMsg:
                messageBytes = MessagePackSerializer.Serialize((StopGameMsg)message);
                break;
            default:
                break;
        }

        byte[] combinedBytes = new byte[1 + messageBytes.Length];
        combinedBytes[0] = messageTypeByte;
        Buffer.BlockCopy(messageBytes, 0, combinedBytes, 1, messageBytes.Length);
        udpServer.Send(combinedBytes, endPoint);

        return combinedBytes;
    }

    static void SendRepeatMessage(byte[] combinedBytes, IPEndPoint endPoint)
    {
        udpServer.Send(combinedBytes, endPoint);
    }

    static void RelayMessageToAllUser(NetworkMessage message)
    {
        byte[] combinedMsg = null;

        // Uses the keys, so we can e.g stop the 0 client, and it still sends to all clients
        foreach (int key in Clients.Keys)
        {
            IPEndPoint _clientEndPoint = Clients[key].IPEndPoint;
            if (combinedMsg == null)
                combinedMsg = SendMessage(message, _clientEndPoint);
            else
                SendRepeatMessage(combinedMsg, _clientEndPoint);
        }
    }
    private static int _amountOfCharactersPrPlayers = 2;
    static void StartGame()
    {
        //Console.Clear();
        Console.WriteLine($"Started game with {Clients.Count} players");

        _gameGrid = new Grid(_gridX, _gridY);

        int spawnTimes = 1;
        Random random = new Random();
        int minDmg = 5;
        int maxDmg = 15;
        int minHealth = 10;
        int maxHealth = 30;
        for (int i = 0; i < Clients.Count; i++)
        {
            for (int j = 0; j < _amountOfCharactersPrPlayers; j++)
            {
                bool hasFoundSpot = false;

                while (!hasFoundSpot)
                {
                    int x = _rnd.Next(0, _gridX);
                    int y = _rnd.Next(0, _gridY);
                    if (_gameGrid.CharacterGrid[x, y] != null) continue;
                    hasFoundSpot = true;
                    Character player = new Character(i, new Point(x, y), $"Bob {spawnTimes}", CharacterType.Warrior, random.Next(minDmg, maxDmg), random.Next(minHealth, maxHealth));
                    _gameGrid.AddObject(player, x, y);
                    spawnTimes++;
                }
            }
        }

        UpdateGrid();
    }

    static void UpdateGrid()
    {
        SendTurnMsg();

        UpdateGridMsg updateGrid = new UpdateGridMsg() { GameGridArray = _gameGrid.CharacterGrid, GridSize = _gameGrid.GridSize };

        RelayMessageToAllUser(updateGrid);
    }

    static void SendTurnMsg()
    {
        //Make a base response for the current turn user, and otherwise its a waiting turn.
        string othersResponseMessage = $"Its {_turnNmb} turn, please wait...";
        byte[] othersResponseData = Encoding.ASCII.GetBytes(othersResponseMessage);

        string responseMessage = $"Its your turn ({_turnNmb}...";
        byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);

        TurnMsg turnMsg;
        for (int i = 0; i < Clients.Count; i++)
        {
            if (i == _turnNmb)
                turnMsg = new TurnMsg() { Message = responseMessage, IsUsersTurn = true };
            else
                turnMsg = new TurnMsg() { Message = othersResponseMessage, IsUsersTurn = false };

            SendMessage(turnMsg, Clients[i].IPEndPoint);
        }
    }

    static void ChangeTurn()
    {
        _turnNmb = (_turnNmb + 1) % _maxAmountOfPlayers;
        _currentTurnClientData = Clients[_turnNmb];
        UpdateGrid();
    }

    /// <summary>
    /// <para>Deletes clients if the time from last heartbeat is too long</para>
    /// <para>Set up to stop the game if ANY client gets disconnected</para>
    /// </summary>
    static void HeartBeatDeleteWhenOffline()
    {
        List<ClientData> clientToDelete = new();
        while (true)
        {
            Thread.Sleep(500);
            clientToDelete.Clear();

            // Loops though the list. 
            foreach (ClientData data in Clients.Values)
            {
                TimeSpan timeSpan = DateTime.Now - data.LastHeartBeat;

                // If there has elapsed more than 2 seconds
                if (timeSpan.TotalMilliseconds >= 2000)
                {
                    clientToDelete.Add(data);
                }
            }
            
            foreach (var item in clientToDelete)
            {
                string stopMsg = $"Client IP {item.IPEndPoint} has disconnected from server...";
                Console.WriteLine(stopMsg);
                int clientIndex = Clients.First(c => c.Value.IPEndPoint.Equals(item.IPEndPoint)).Key;
                Clients.Remove(clientIndex);
                StopGame(stopMsg);
            }
        }
    }

    static void StopGame(string stopMsg)
    {
        _gameIsRunning = false;
        StopGameMsg stopGameMsg = new StopGameMsg() { Message = stopMsg };
        RelayMessageToAllUser(stopGameMsg);
        
    }
}