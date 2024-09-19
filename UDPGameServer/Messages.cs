using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPGameServer;

public enum MessageType
{
    RequestAddClient,
    StartGame,
    HeartBeat,
    RequestMovePosition,
    ServerMsg,
    GameMsg,
    TurnMsg,
    UpdateGrid,
    StopGameMsg,
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

public class StartGameMsg : NetworkMessage
{
    [IgnoreMember]
    public override MessageType MessageType => MessageType.StartGame;
}

public class TurnMsg : NetworkMessage
{
    [Key(0)]
    public string Message;

    [Key(1)]
    public bool IsUsersTurn;

    [IgnoreMember]
    public override MessageType MessageType => MessageType.TurnMsg;
}

public class GameMsg : NetworkMessage
{
    [Key(0)]
    public string Message;

    [IgnoreMember]
    public override MessageType MessageType => MessageType.GameMsg;
}

public class ServerMsg : NetworkMessage
{
    [Key(0)]
    public string Message;

    [IgnoreMember]
    public override MessageType MessageType => MessageType.ServerMsg;
}

public class StopGameMsg : NetworkMessage
{
    [Key(0)]
    public string Message;

    [IgnoreMember]
    public override MessageType MessageType => MessageType.StopGameMsg;
}
public class RequestAddClientMsg : NetworkMessage
{
    [IgnoreMember]
    public override MessageType MessageType => MessageType.RequestAddClient;
}


public class HeartBeatMsg : NetworkMessage
{
    [IgnoreMember]
    public override MessageType MessageType => MessageType.HeartBeat;
}

public class RequestMovePosMsg : NetworkMessage
{
    [Key(0)]
    public Point PrevPos;

    [Key(1)]
    public Point NewTargetPos;

    [IgnoreMember]
    public override MessageType MessageType => MessageType.RequestMovePosition;
}

public class UpdateGridMsg : NetworkMessage
{
    [Key(0)]
    public Character[,] GameGridArray { get; set; }

    [Key(1)]
    public Point GridSize { get; set; }

    [IgnoreMember]
    public override MessageType MessageType => MessageType.UpdateGrid;
}

// Grid

// Update grid


