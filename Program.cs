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
        #region appConst
        const string IMAGE_BASE = @"C:\Users\Roman\Desktop\images\"; //Примеры фото
        const string RECOGNITION_MODEL = RecognitionModel.Recognition02;
        static string sourcePersonGroup = null;
        public static IFaceClient Authenticate(string endpoint, string key) /*Аутентификация клиента 
            создание экземпляра клиента с использованием конечной точки и ключа*/
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }
        #endregion

        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string pathToBase, string RECOGNITION_MODEL1) //нахождение лиц
        {
            using (FileStream stream = new FileStream(pathToBase, FileMode.Open))
            {
                IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithStreamAsync(stream, recognitionModel: RECOGNITION_MODEL1);
                if (detectedFaces == null || detectedFaces.Count == 0)
                {
                    Console.WriteLine($"[Error] No face detected from image `{pathToBase}`.");
                    return null;
                }
                Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{Path.GetFileName(pathToBase)}`");
                return detectedFaces.ToList();
            }
        }

        public static async Task FindSimilar(IFaceClient client, string pathToBase, string RECOGNITION_MODEL1)
        {
            Console.WriteLine("========FIND SIMILAR========");

            List<string> targetImageFileNames = new List<string> //база картинок
                        {
                "example.jpg"
                        };

            string sourceImageFileName = "example2.jpg"; //картинка которую сравниваем



            IList<Guid?> targetFaceIds = new List<Guid?>(); //лист GUID для уник. идентификаторов каждого 


            foreach (var targetImageFileName in targetImageFileNames)
            {
                // запихиваем каждого человека из базы картинок в GUID
                var faces = await DetectFaceRecognize(client, $"{pathToBase}{targetImageFileName}", RECOGNITION_MODEL1);
                if (faces == null)
                {
                    return;
                }
                targetFaceIds.Add(faces[0].FaceId.Value);
            }


            // Обнаруживаем лица 
            IList<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{pathToBase}{sourceImageFileName}", RECOGNITION_MODEL1);
            Console.WriteLine();

            // Поиск похожих лиц в списке GUID
            IList<SimilarFace> similarResults = await client.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, null, null, targetFaceIds);
            foreach (var similarResult in similarResults)
            {
                Console.WriteLine($"Faces from {sourceImageFileName} & ID:{similarResult.FaceId} are similar with confidence: {similarResult.Confidence}.");
            }
            Console.WriteLine();
        }



        static void Main(string[] args)
        {
            //Azure Key+Endpoint
            string SUBSCRIPTION_KEY = Environment.GetEnvironmentVariable("FACE_SUBSCRIPTION_KEY");
            string ENDPOINT = Environment.GetEnvironmentVariable("FACE_ENDPOINT");

            //Создание экземпляра клиента с Key+Endpoint
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);            
            //Определение лиц на изображении
            FindSimilar(client, IMAGE_BASE, RECOGNITION_MODEL).Wait();
        }
    }
}
