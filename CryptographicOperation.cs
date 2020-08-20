using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace EncryptDecryptFilesByFace
{
    class CryptographicOperation
    {
        private string FilePath { get; set; }

        private byte[] SessionKey { get; set; }

        private byte[] SessionIV { get; set; }

        private byte[] EncryptedBytes { get; set; }

        private byte[] DecryptedBytes { get; set; }


        /// <summary>
        /// Конструктор который вычисляет пару Key,IV; Получает название файла который надо зашифровать/расшифровать
        /// </summary>
        /// <param name="file"> Файл который надо зашифровать/расшифровать </param>
        /// <param name="key"> Ключ </param>
        /// <param name="iv"> InitializationVector </param>
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


        /// <summary>
        /// Шифрование
        /// </summary>
        public void Encrypt()
        {
            Aes aes = Aes.Create();
            ICryptoTransform crypt = aes.CreateEncryptor(SessionKey, SessionIV);
            var textFromFile = "";

            //Получение набора байтов обычного текста в [] DecryptedBytes
            using (FileStream fs = File.OpenRead($"{FilePath}")) 
            {
                DecryptedBytes = new byte[fs.Length];
                fs.Read(DecryptedBytes, 0, DecryptedBytes.Length);
                textFromFile = Encoding.UTF8.GetString(DecryptedBytes); //Получили текст из файла
            }

            //Зашифровываем файл в [] EncryptedBytes
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, crypt, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(textFromFile);
                    }
                }
                EncryptedBytes = ms.ToArray();
            }

            //Создаем файл с зашифрованным содержимым .crypt
            using (FileStream fs = new FileStream($"{FilePath}.crypt", FileMode.OpenOrCreate))
            {
                fs.Write(EncryptedBytes, 0, EncryptedBytes.Length);
            }
        }

        /// <summary>
        /// Расшифрование
        /// </summary>
        public void Decrypt()
        {
            Aes aes = Aes.Create();
            ICryptoTransform decrypt = aes.CreateDecryptor(SessionKey, SessionIV);

            //Получение набора шифрованных байтов в [] EncryptedBytes
            using (FileStream fs = File.OpenRead($"{FilePath}.crypt")) 
            {
                EncryptedBytes = new byte[fs.Length];
                fs.Read(EncryptedBytes, 0, EncryptedBytes.Length);
            }

            //Расшифровываем файл в [] DecryptedBytes
            using (MemoryStream ms = new MemoryStream(EncryptedBytes))
            {
                using (CryptoStream cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Read))
                {
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        //Расшифрованный набор байтов записываем в [] DecryptedBytes
                        DecryptedBytes = Encoding.UTF8.GetBytes(sr.ReadToEnd());
                    }                 
                }
            }

            //Создаем файл с расшифрованным содержимым .decrypt
            using (FileStream fs = new FileStream($"{FilePath}.decrypt", FileMode.OpenOrCreate))
            {
                fs.Write(DecryptedBytes, 0, DecryptedBytes.Length);
            }
        }

    }
}
