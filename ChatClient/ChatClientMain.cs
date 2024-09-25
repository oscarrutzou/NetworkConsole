using MessagePack;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TCP;

namespace ChatClient;

public static class ChatClientMain
{
    static BinaryWriter _bw = null;
    static TcpClient server;
    private static byte[] _clientKey;
    private static byte[] _serverKey;

    public static void Main(string[] args)
    {
        Console.WriteLine("I'm the Chat Client\n");
        _clientKey = Encryption.GetRandomKey();

        TcpClient server = new TcpClient();
        server.Connect("localhost", 12000);
        _bw = new BinaryWriter(server.GetStream());

        // Start a thread to receive messages
        Thread receiveThread = new Thread(() => ReceiveMessages(server));
        receiveThread.Start();

        SendMessage(new TCPRequestKeyMsg() { ClientKey = _clientKey});

        Console.WriteLine("Connected to server...\nWrite userName");
        bool userNameNotAdded = true;
        string userName = "";

        while (userNameNotAdded)
        {
            userName = Console.ReadLine();
            if (userName.Contains(' '))
            {
                Console.WriteLine("user name cant contain spaces!");
            }
            else
            {
                userNameNotAdded = false;
                TCPJoinServerMsg joinMsg = new TCPJoinServerMsg() { Name = userName};
                SendMessage(joinMsg);
            }
        }

        MainUpdate();
    }

    private static void MainUpdate()
    {
        while (true)
        {
            string message = Console.ReadLine();
            var splitMessage = message.Split(' ', 2);
            TCPNetworkMessage mes = null;
            if (message == "!list")
            {
                mes = new TCPRequestListMsg();
            }
            else if (message == "!log")
            {
                mes = new TCPRequestLogMsg();   
            } 
            else
            {
                mes = new TCPChatMsg() { Temp_Text = message };
            }
            
            SendMessage(mes);
        }
    }
    // Join game
    // Starts a game server if there isnt one running.
    // Also starts a game client
    //else if (message.StartsWith("d "))
    //{
    //    var split = message.Split(" ", 3);
    //    if (split.Count() <= 2)
    //    {
    //        Console.WriteLine("d argument must have at least 2 parameters");
    //        continue;
    //    }
    //    mes = new DirectMessage() { Message = split[2], recipient = split[1] };
    //}

    private static void SendMessage(TCPNetworkMessage message)
    {
        byte[] messageBytes = new byte[1024];

        switch (message.MessageType)
        {
            case TCPMessagesTypes.C_RequestKey:
                messageBytes = MessagePackSerializer.Serialize((TCPRequestKeyMsg)message);
                break;

            case TCPMessagesTypes.ChatMessage:
                messageBytes = Encryption.SerilizeChatMsg((TCPChatMsg)message, _clientKey);
                break;
            case TCPMessagesTypes.C_JoinServer:
                messageBytes = MessagePackSerializer.Serialize((TCPJoinServerMsg)message);
                break;
            case TCPMessagesTypes.C_RequestListMsg:
                messageBytes = MessagePackSerializer.Serialize((TCPRequestListMsg)message);
                break;
            case TCPMessagesTypes.C_RequestLogMsg:
                messageBytes = MessagePackSerializer.Serialize((TCPRequestLogMsg)message);
                break;
        }
        
        _bw.Write(messageBytes.Length);
        _bw.Write(message.GetMessageTypeAsByte);
        _bw.Write(messageBytes);
        _bw.Flush();
    }
    

    private static void ReceiveMessages(TcpClient client)
    {
        BinaryReader bwr = new BinaryReader(client.GetStream());
        while (client.Connected)
        {
            int messageLength = bwr.ReadInt32();
            byte messageType = bwr.ReadByte();
            // Read the message data
            byte[] messageBytes = bwr.ReadBytes(messageLength);

            TCPMessagesTypes receivedType = (TCPMessagesTypes)messageType;
            switch (receivedType)
            {
                case TCPMessagesTypes.S_AnswerKey:
                    TCPAnswerKeyMsg keyMsg = MessagePackSerializer.Deserialize<TCPAnswerKeyMsg>(messageBytes);
                    _serverKey = keyMsg.ServerKey;
                    break;

                case TCPMessagesTypes.S_WelcomeNewUser:
                    TCPWelcomeMsg welcomeMsg = MessagePackSerializer.Deserialize<TCPWelcomeMsg>(messageBytes);
                    Console.WriteLine(welcomeMsg.Message);
                    break;

                case TCPMessagesTypes.ChatMessage:
                    TCPChatMsg chatMes = Encryption.DeSerilizeChatMsg(messageBytes, _serverKey);
                    Console.WriteLine(chatMes.Temp_Text);
                    break;

                case TCPMessagesTypes.S_ServerMessage:
                    TCPServerMsg serverMes = MessagePackSerializer.Deserialize<TCPServerMsg>(messageBytes);
                    Console.WriteLine(serverMes.Message);
                    break;
            }
        }
    }

}
