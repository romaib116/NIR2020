using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using MySql.Data.MySqlClient;

namespace FaceDetect
{
    class Program
    {
        #region appConst
        const string IMAGE_BASE = @"C:\Users\Roman\Desktop\images\"; //Путь к фото
        const string RECOGNITION_MODEL = RecognitionModel.Recognition02;

        public static IFaceClient Authenticate(string endpoint, string key) /*Аутентификация клиента 
            создание экземпляра клиента с использованием конечной точки и ключа*/
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }

        #endregion


        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string pathToBase, string RECOGNITION_MODEL) //Подсчет
            //количества лиц на фото и вывод их ID
        {
            using (FileStream stream = new FileStream(pathToBase, FileMode.Open))
            {
                IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithStreamAsync(stream, recognitionModel: RECOGNITION_MODEL);
                if (detectedFaces == null || detectedFaces.Count == 0) //если на фото нет лиц - ошибка
                {
                    Console.WriteLine($"[Error] No face detected from image `{pathToBase}`.");
                    return null;
                }
                else if (detectedFaces.Count>1) //если на фото больше одного лица - их слишком много
                {
                    Console.WriteLine($"[Error] Too more faces from image`{pathToBase}`.");
                    return null;
                }
                else //благоприятный исход
                {
                    Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{Path.GetFileName(pathToBase)}`"); //вывести количество людей на фото
                    foreach (var detectedFace in detectedFaces) { Console.WriteLine(detectedFace.FaceId.Value); } //вывести guid человека
                    return detectedFaces.ToList();
                }
            }
        }

        public static async Task FindSimilar(IFaceClient client, string pathToBase, string RECOGNITION_MODEL)
        {
            Console.WriteLine("========FIND SIMILAR========");
            db DataBase = new db();
            List<string> targetImageFileNames = DataBase.GetPhotosFromDB(); //База картинок

            string sourceImageFileName = "example2.jpg"; //картинка которую сравниваем

            IList<Guid?> targetFaceIds = new List<Guid?>(); //Создание нового списка GUID для людей
            foreach (var targetImageFileName in targetImageFileNames) //Создание GUID для каждой картинки из базы
            {
                var faces = await DetectFaceRecognize(client, $"{pathToBase}{targetImageFileName}", RECOGNITION_MODEL); //Запуск асинхр потока с поиском лиц
                if (faces == null) //Если лиц нет то return
                {
                    return;
                }
                targetFaceIds.Add(faces[0].FaceId.Value); //Засунуть в список человека
                DataBase.SetGUIDForDB(faces[0].FaceId.Value, targetImageFileName);
            }

            //Запуск асинхр потока с обнаружением лиц на ВХОДНОМ фото
            IList<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{pathToBase}{sourceImageFileName}", RECOGNITION_MODEL);

            // Поиск похожих лиц в списке GUID. Метод FindSimilarAsync (поиск похожих в асинхронном потоке. В метод передаем обнаруженное лицо
            //на ВХОДНОМ фото, и сравниваем с базой всех лиц)
            IList<SimilarFace> similarResults = await client.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, null, null, targetFaceIds);
            
            foreach (var similarResult in similarResults)
            {

                Console.WriteLine($"Face from {sourceImageFileName} & {DataBase.FindSimilarPhotoFromDB(similarResult.FaceId.Value)}(ID:{similarResult.FaceId.Value}) are similar with confidence: {similarResult.Confidence}.");
            }
            Console.WriteLine();
        }



        static void Main(string[] args)
        {
            //Azure Key+Endpoint
            string SUBSCRIPTION_KEY = Environment.GetEnvironmentVariable("FACE_SUBSCRIPTION_KEY");
            string ENDPOINT = Environment.GetEnvironmentVariable("FACE_ENDPOINT");

            //Создание типа client с парой КЛЮЧ+КОНЕЧНАЯ ТОЧКА
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            //Поиск похожих лиц на фотках
            FindSimilar(client, IMAGE_BASE, RECOGNITION_MODEL).Wait();

        }
    }
}
