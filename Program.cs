using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;


namespace FaceDetect
{
    class Program
    {
        static string SUBSCRIPTION_KEY = Environment.GetEnvironmentVariable("FACE_SUBSCRIPTION_KEY");
        static string ENDPOINT = Environment.GetEnvironmentVariable("FACE_ENDPOINT");
        static string RECOGNITION_MODEL = RecognitionModel.Recognition02; //Модель распознавания лиц (всего их 2, выбрал самую новую)
        const string IMAGE_BASE = @"C:\Users\Roman\Desktop\images\"; //Путь к фото



        static void Main(string[] args)
        {
            var Face = new FaceComparison();
            var client = Face.Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            Face.FindSimilar(client, IMAGE_BASE, RECOGNITION_MODEL).Wait();

        }
    }
}
