using System;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;



namespace EncryptDecryptFilesByFace
{
    class Program
    {
        static string SUBSCRIPTION_KEY = Environment.GetEnvironmentVariable("FACE_SUBSCRIPTION_KEY");
        static string ENDPOINT = Environment.GetEnvironmentVariable("FACE_ENDPOINT");
        const string RECOGNITION_MODEL = RecognitionModel.Recognition02; //Модель распознавания лиц (всего их 2, выбрал самую новую)
        const string IMAGE_BASE = @"C:\Users\Roman\Desktop\images\"; //Путь к фото


        static void Main(string[] args)
        {
            var InputImageFileName = "example2.jpg"; //Картинка поступающая на ВХОД для сравнения с базой
            var InputFile = @"C:\Users\Roman\Desktop\files\test.txt"; //Файл который надо шифровать/расшифровывать
            var Face = new FaceComparison();
            var Client = Face.Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            Face.FindSimilar(Client, IMAGE_BASE, RECOGNITION_MODEL, InputImageFileName).Wait();
            Console.WriteLine($"Hello {Face.FaceName}!");

            //Начинаем работу с шифрованием
            if (InputFile != null && Face.FaceKey != null && Face.FaceIV != null)
            {
                var File = new CryptographicOperation(InputFile, Face.FaceKey, Face.FaceIV);
                File.Encrypt();
                File.Decrypt();
            }
            else
            {
                Console.WriteLine("Return error, check input data for encrypt/decrypt file");
            }

        }
    }
}
