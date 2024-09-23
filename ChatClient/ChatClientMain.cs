using MessagePack;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TCP;

namespace ChatClient
{
    public static class ChatClientMain
    {
        static BinaryWriter _bw = null;
        static TcpClient server;
        public static void Main(string[] args)
        {
            TcpClient server = new TcpClient();
            server.Connect("localhost", 12000);
            _bw = new BinaryWriter(server.GetStream());

            // Start a thread to receive messages
            Thread receiveThread = new Thread(() => ReceiveMessages(server));
            receiveThread.Start();

            SendMessage(new TCPRequestKeyMsg());

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
                else
                {
                    mes = new TCPChatMsg() { Message = message };
                }
                SendMessage(mes);
            }
        }

        private static void SendStartMsg()
        {

        }

        private static void SendMessage(TCPNetworkMessage message)
        {
            byte[] messageBytes = new byte[1024];
            bool encrypted = false;
            switch (message.MessageType)
            {
                case TCPMessagesTypes.C_RequestKey:
                    messageBytes = MessagePackSerializer.Serialize((TCPRequestKeyMsg)message);
                    //encrypted = false;
                    break;
                case TCPMessagesTypes.ChatMessage:
                    messageBytes = MessagePackSerializer.Serialize((TCPChatMsg)message);
                    break;
                case TCPMessagesTypes.C_JoinServer:
                    messageBytes = MessagePackSerializer.Serialize((TCPJoinServerMsg)message);
                    break;
                case TCPMessagesTypes.C_RequestListMsg:
                    messageBytes = MessagePackSerializer.Serialize((TCPRequestListMsg)message);
                    break;
            }

            byte[] finalMsg = messageBytes;

            //if (encrypted)
            //{
            //    // En bool hvis den er krypteret.
            //    byte[] iv = new byte[16];
            //    RandomNumberGenerator.Fill(iv);

            //    byte[] cypherText = Encryption.Encrypt(messageBytes, _key, iv);
            //    finalMsg = cypherText;
            //}

            _bw.Write(finalMsg.Length);
            _bw.Write(message.GetMessageTypeAsByte);
            _bw.Write(encrypted);
            _bw.Write(finalMsg);
           
            _bw.Flush();
        }
        
        private static byte[] _key;

        private static void ReceiveMessages(TcpClient client)
        {
            BinaryReader bwr = new BinaryReader(client.GetStream());
            while (client.Connected)
            {
                int  messageLength = bwr.ReadInt32();
                byte messageType = bwr.ReadByte();
                bool encrypted = bwr.ReadBoolean();
                // Read the message data
                byte[] messageBytes = bwr.ReadBytes(messageLength);

                TCPMessagesTypes receivedType = (TCPMessagesTypes)messageType;
                switch (receivedType)
                {
                    case TCPMessagesTypes.S_AnswerKey:
                        TCPAnswerKeyMsg keyMsg = MessagePackSerializer.Deserialize<TCPAnswerKeyMsg>(messageBytes);
                        _key = keyMsg.Key;
                        break;

                    case TCPMessagesTypes.ChatMessage:
                        TCPChatMsg chatMes = MessagePackSerializer.Deserialize<TCPChatMsg>(messageBytes);
                        Console.WriteLine(chatMes.Message);
                        break;

                    case TCPMessagesTypes.S_ServerMessage:
                        TCPServerMsg serverMes = MessagePackSerializer.Deserialize<TCPServerMsg>(messageBytes);
                        Console.WriteLine(serverMes.Message);
                        break;

                    case TCPMessagesTypes.S_WelcomeNewUser:
                        TCPWelcomeMsg welcomeMsg = MessagePackSerializer.Deserialize<TCPWelcomeMsg>(messageBytes);
                        Console.WriteLine(welcomeMsg.Message);
                        break;
 
                    case TCPMessagesTypes.S_ListMsg:
                        TCPListMsg listMsg = MessagePackSerializer.Deserialize<TCPListMsg>(messageBytes);
                        Console.WriteLine(listMsg.List);
                        break;
                }
            }
        }

    }
    //public static class ChatClientMain
    //{
    //    private static byte[] KEY;
    //    private static byte[] IV;
    //    private static byte[] CIPHER_TEXT;

    //    public static void Main(string[] args)
    //    {

    //        //byte[] key = new byte[16]; 
    //        //byte[] iv = new byte[16]; 
    //        //using (var rng = new RNGCryptoServiceProvider())
    //        //{
    //        //    rng.GetBytes(key);
    //        //    rng.GetBytes(iv);
    //        //}


    //        TcpClient client = new TcpClient();
    //        client.Connect("localhost", 12345);
    //        Console.WriteLine("Connected to the server.");
    //        Console.WriteLine("What is your desired username?");

    //        // Start a thread to receive messages
    //        Thread receiverThread = new Thread(() => ReceiveMessages(client /*, key, iv*/));
    //        receiverThread.IsBackground = true;
    //        receiverThread.Start();

    //        SHA256 sHA = SHA256.Create();
    //        byte[] key = sHA.ComputeHash(Encoding.UTF8.GetBytes("something lol")); // A random key
    //        var rng = new RNGCryptoServiceProvider();


    //        // Send messages
    //        using (NetworkStream networkStream = client.GetStream())
    //        using (StreamWriter writer = new StreamWriter(networkStream))
    //        {
    //            while (true)
    //            {
    //                string message = Console.ReadLine();

    //                try
    //                {
    //                    byte[] iv = new byte[16];
    //                    rng.GetBytes(iv);

    //                    // Save IV in message and send it with cipher text

    //                    CIPHER_TEXT = Encryption.Encrypt(message, KEY, iv);
    //                    TCPChatMsg msg = new TCPChatMsg() { Message = CIPHER_TEXT, IV = iv};

    //                    //writer.WriteLine(CIPHER_TEXT.ToString());
    //                    writer.WriteLine(message);
    //                    writer.Flush();
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine($"An error occurred while sending the message: {ex.Message}");
    //                }
    //            }
    //        }
    //    }

    //    static void ReceiveMessages(TcpClient client)
    //    {
    //        using (NetworkStream networkStream = client.GetStream())
    //        using (StreamReader reader = new StreamReader(networkStream))
    //        {
    //            while (client.Connected)
    //            {
    //                try
    //                {   string message = reader.ReadLine();
    //                    Console.WriteLine($"{message}");

    //                    //if (message != null)
    //                    //// string encryptedMessageBase64 = reader.ReadLine();
    //                    //if (encryptedMessageBase64 != null)
    //                    //{
    //                    //    //byte[] encryptedMessageBytes = Convert.FromBase64String(encryptedMessageBase64);
    //                    //    //string decryptedMessage = Encryption.Decrypt(encryptedMessageBytes, key, iv);
    //                    //    //Console.WriteLine(decryptedMessage);
    //                    //}
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine($"An error occurred while receiving or decrypting the message: {ex.Message}");
    //                }
    //            }
    //        }
    //    }
    //}
}
