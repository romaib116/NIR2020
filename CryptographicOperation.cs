using System;
using System.IO;
using System.Security.Cryptography;

namespace EncryptDecryptFilesByFace
{
    class CryptographicOperation
    {
        private string FilePath { get; set; }

        private byte[] SessionKey { get; set; }

        private byte[] SessionIV { get; set; }

        public CryptographicOperation(string file, byte[] key, byte[] iv)
        {
            SHA512 Hash512 = new SHA512Managed();
            FilePath = file;
            var tempKey = Hash512.ComputeHash(key);
            var tempIV = Hash512.ComputeHash(iv);
            //Определяем размеры Key и InitVector, т.к. AES чувствителен к размеру этих данных
            SessionKey = new byte[16]; 
            SessionIV = new byte[16];
            for (int i = 0; i < 16; i++) //Забираем первые 16 байт в SessionKey, SessionIV из их SHA512 хеша
            {
                SessionKey[i] = tempKey[i];
                SessionIV[i] = tempIV[i];
            }
        }

        public void Encrypt ()
        {
            try
            {
                FileStream myStream = new FileStream(FilePath, FileMode.Open);
                Aes aes = Aes.Create();
                CryptoStream cryptStream = new CryptoStream(myStream, aes.CreateEncryptor(SessionKey, SessionIV), CryptoStreamMode.Write);
                cryptStream.Close();
                myStream.Close();

            }
            catch
            {
                Console.WriteLine("The encryption failed.");
                throw;
            }
        }

    }
}
