//using System;
//using System.IO;
//using System.Security.Cryptography;
//using System.Text;

//namespace ChatClient
//{
//    public class Encryption
//    {
//        public static byte[] Encrypt(string plainText, byte[] key, byte[] iv)
//        {
//            using (Aes aes = Aes.Create())
//            {
//                aes.Key = key;
//                aes.IV = iv;

//                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

//                using (MemoryStream ms = new MemoryStream())
//                {
//                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
//                    {
//                        using (StreamWriter sw = new StreamWriter(cs))
//                        {
//                            sw.Write(plainText);
//                        }
//                        return ms.ToArray();
//                    }
//                }
//            }
//        }

//        public static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
//        {
//            using (Aes aes = Aes.Create())
//            {
//                aes.Key = key;
//                aes.IV = iv;

//                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

//                using (MemoryStream ms = new MemoryStream(cipherText))
//                {
//                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
//                    {
//                        using (StreamReader sr = new StreamReader(cs))
//                        {
//                            return sr.ReadToEnd();
//                        }
//                    }
//                }
//            }
//        }
//    }
//}
