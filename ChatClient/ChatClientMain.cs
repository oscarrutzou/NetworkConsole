using System.Net.Sockets;
using System.Text.Json;
using System.Text;
namespace ChatClient;

public static class ChatClientMain
{
    public static void Main(string[] args)
    {
        TcpClient client = new TcpClient();
        client.Connect("localhost", 12345);
        Console.WriteLine("Connected to the server.");
        Console.WriteLine("What is your desired username?");

        //Starts a thread to receive messages
        Thread receiverThread = new Thread(() => ReceiveMessages(client));
        receiverThread.IsBackground = true;
        receiverThread.Start();




        //Send messages with the via the following
        StreamWriter writer = new StreamWriter(client.GetStream());
        while (true)
        {
            string message = Console.ReadLine();
            writer.WriteLine(message);
            writer.Flush();

        }
    }

    static void ReceiveMessages(TcpClient client)
    {
        StreamReader reader = new StreamReader(client.GetStream());
        while (client.Connected)
        {
            string message = reader.ReadLine();
            if (message != null)
            {
                Console.WriteLine(message);
            }
        }
    } 
}



    