using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPGameServer;

public enum MessageType
{
    StartGame,
    HeartBeat,
    MovePosition,
    ServerMsg,
}


[MessagePackObject]
public abstract class NetworkMessage
{
    [IgnoreMember]
    public abstract MessageType MessageType { get; }
    [IgnoreMember]
    public byte GetMessageTypeAsByte
    {
        get { return (byte)MessageType; }
    }
}

public class StartGame : NetworkMessage
{
    [IgnoreMember]
    public override MessageType MessageType => MessageType.StartGame;
}

public class ServerMsg : NetworkMessage
{
    [Key(0)]
    public string Message;

    [IgnoreMember]
    public override MessageType MessageType => MessageType.ServerMsg;
}

// Grid

// Update grid


