using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ChatClient
{
    public static class ChatClientMain
    {
        public static void Main(string[] args)
        {

            //byte[] key = new byte[16]; 
            //byte[] iv = new byte[16]; 
            //using (var rng = new RNGCryptoServiceProvider())
            //{
            //    rng.GetBytes(key);
            //    rng.GetBytes(iv);
            //}
            TcpClient client = new TcpClient();
            client.Connect("localhost", 12345);
            Console.WriteLine("Connected to the server.");
            Console.WriteLine("What is your desired username?");

            // Start a thread to receive messages
            Thread receiverThread = new Thread(() => ReceiveMessages(client, /*key, iv*/));
            receiverThread.IsBackground = true;
            receiverThread.Start();

            // Send messages
            using (NetworkStream networkStream = client.GetStream())
            using (StreamWriter writer = new StreamWriter(networkStream))
            {
                while (true)
                {
                    string message = Console.ReadLine();

                    try
                    {
                        //byte[] encryptedMessageBytes = Encryption.Encrypt(message, /*key, iv*/);
                        //string encryptedMessageBase64 = Convert.ToBase64String(encryptedMessageBytes);
                        writer.WriteLine(message/*encryptedMessageBase64*/);
                        writer.Flush();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while sending the message: {ex.Message}");
                    }
                }
            }
        }

        static void ReceiveMessages(TcpClient client, /*byte[] key, byte[] iv*/)
        {
            using (NetworkStream networkStream = client.GetStream())
            using (StreamReader reader = new StreamReader(networkStream))
            {
                while (client.Connected)
                {
                    try
                    {   string message = reader.ReadLine();
                        if (message != null)
                        //string encryptedMessageBase64 = reader.ReadLine();
                        if (encryptedMessageBase64 != null)
                        {
                            //byte[] encryptedMessageBytes = Convert.FromBase64String(encryptedMessageBase64);
                            //string decryptedMessage = Encryption.Decrypt(encryptedMessageBytes, key, iv);
                            Console.WriteLine($"{message}");
                            //Console.WriteLine(decryptedMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while receiving or decrypting the message: {ex.Message}");
                    }
                }
            }
        }
    }
}
