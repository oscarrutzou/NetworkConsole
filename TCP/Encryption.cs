using MessagePack;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TCP;

public static class Encryption
{

    public static byte[] SerilizeChatMsg(TCPChatMsg chatMsg, byte[] key) // Program.AnswerKeyMsg.Key
    {
        byte[] messageBytes = new byte[1024];

        byte[] iv = new byte[16];
        RandomNumberGenerator.Fill(iv);
        chatMsg.IV = iv;
        byte[] textInBytes = Encoding.UTF8.GetBytes(chatMsg.Temp_Text);
        chatMsg.Cypher_Message = Encrypt(textInBytes, key, iv);

        messageBytes = MessagePackSerializer.Serialize(chatMsg);
        return messageBytes;
    }

    public static TCPChatMsg DeSerilizeChatMsg(byte[] messageBytes, byte[] key)
    {
        TCPChatMsg chatMes = MessagePackSerializer.Deserialize<TCPChatMsg>(messageBytes);
        byte[] decryptedChatMsg = Decrypt(chatMes.Cypher_Message, key, chatMes.IV);
        string chatMsg = Encoding.UTF8.GetString(decryptedChatMsg);
        chatMes.Temp_Text = chatMsg;

        return chatMes;
    }

    public static byte[] Encrypt(byte[] plainBytes, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (BinaryWriter bw = new BinaryWriter(cs))
                    {
                        bw.Write(plainBytes);
                    }
                    return ms.ToArray();
                }
            }
        }
    }

    public static byte[] Decrypt(byte[] cipherText, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream(cipherText))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (BinaryReader br = new BinaryReader(cs))
                    {
                        return br.ReadBytes(cipherText.Length);
                    }
                }
            }
        }
    }
}
