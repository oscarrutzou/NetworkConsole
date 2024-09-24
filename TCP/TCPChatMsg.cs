using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace TCP
{
    public enum TCPMessagesTypes
    {
        C_RequestKey,
        S_AnswerKey,
        C_JoinServer,
        S_WelcomeNewUser,
        ChatMessage, // client to server
        S_ServerMessage,
        C_RequestListMsg,
        C_RequestLogMsg,
    }

    [MessagePackObject]
    public abstract class TCPNetworkMessage
    {
        //[Key(0)]
        //public byte[] IV { get; set; }

        [IgnoreMember]
        public abstract TCPMessagesTypes MessageType { get; }
        [IgnoreMember]
        public byte GetMessageTypeAsByte
        {
            get { return (byte)MessageType; }
        }
    }


    // Shared key
    #region Sets up the Key
    public class TCPRequestKeyMsg : TCPNetworkMessage
    {
        [IgnoreMember]
        public override TCPMessagesTypes MessageType => TCPMessagesTypes.C_RequestKey;
    }

    public class TCPAnswerKeyMsg : TCPNetworkMessage
    {
        [Key(0)]
        public byte[] Key { get; set; }
        [IgnoreMember]
        public override TCPMessagesTypes MessageType => TCPMessagesTypes.S_AnswerKey;
    }
    #endregion

    public class TCPWelcomeMsg : TCPNetworkMessage
    {
        [Key(0)]
        public string Message { get; set; }
        [IgnoreMember]
        public override TCPMessagesTypes MessageType => TCPMessagesTypes.S_WelcomeNewUser;
    }

    public class TCPJoinServerMsg : TCPNetworkMessage
    {
        [Key(0)]
        public string Name { get; set; }
        [IgnoreMember]
        public override TCPMessagesTypes MessageType => TCPMessagesTypes.C_JoinServer;
    }

    public class TCPChatMsg : TCPNetworkMessage
    {
        [Key(0)]
        public byte[] Cypher_Message { get; set; }

        [Key(1)]
        public byte[] IV { get; set; }
        
        [IgnoreMember]
        public string Temp_Text { get; set; }
        [IgnoreMember]
        public override TCPMessagesTypes MessageType => TCPMessagesTypes.ChatMessage;
    }

    public class TCPServerMsg : TCPNetworkMessage
    {
        [Key(0)]
        public string Message { get; set; }
        [IgnoreMember]
        public override TCPMessagesTypes MessageType => TCPMessagesTypes.S_ServerMessage;
    }
    
    public class TCPRequestListMsg : TCPNetworkMessage
    {
        [IgnoreMember]
        public override TCPMessagesTypes MessageType => TCPMessagesTypes.C_RequestListMsg;
    }
    public class TCPRequestLogMsg : TCPNetworkMessage
    {
        [IgnoreMember]
        public override TCPMessagesTypes MessageType => TCPMessagesTypes.C_RequestLogMsg;
    }
}
