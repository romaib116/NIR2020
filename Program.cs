using System;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;


namespace FaceDetect
{
    class Program
    {
        static string SUBSCRIPTION_KEY = Environment.GetEnvironmentVariable("FACE_SUBSCRIPTION_KEY");
        static string ENDPOINT = Environment.GetEnvironmentVariable("FACE_ENDPOINT");
        const string RECOGNITION_MODEL = RecognitionModel.Recognition02; //Модель распознавания лиц (всего их 2, выбрал самую новую)
        static string IMAGE_BASE = @"C:\Users\Roman\Desktop\images\"; //Путь к фото
        


        static void Main(string[] args)
        {
            var InputImageFileName = "example2.jpg"; //Картинка поступающая на ВХОД для сравнения с базой
            var Face = new FaceComparison();
            var client = Face.Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            Face.FindSimilar(client, IMAGE_BASE, RECOGNITION_MODEL, InputImageFileName).Wait(); 

        }
    }
}
