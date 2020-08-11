using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FaceDetect
{
    class FaceComparison
    {
        public IFaceClient Authenticate(string endpoint, string key) /*Аутентификация клиента 
            создание экземпляра клиента с использованием конечной точки и ключа*/
        { return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint }; }

        public int FaceKey { get; private set; }
        public string FaceName { get; private set; }


        public async Task FindSimilar(IFaceClient Client, string PathToBase, string RecognitionModel, string InputImageFileName) //Найти похожие лица
        {
            Console.WriteLine("Start scan Database");
            db DataBase = new db();
            List<string> DBImageFileNames = DataBase.GetAllPhotos(); //База данных названий картинок
            IList<Guid?> TargetFaceIds = new List<Guid?>(); //Создание нового списка ID для людей
            foreach (var ImageFileName in DBImageFileNames) //Создание ID для каждой картинки из базы
            {
                var Faces = await DetectFaceRecognize(Client, $"{PathToBase}{ImageFileName}", RecognitionModel); //Запуск асинхр потока с поиском лиц
                if (Faces == null) { return; } //На случай, если лица не обнаружены
                TargetFaceIds.Add(Faces[0].FaceId.Value); //Засунуть в список человека
                DataBase.UpdateGUID(Faces[0].FaceId.Value, ImageFileName); //Обновить значение ID для человека в стобце GUID БД
            }
            Console.WriteLine("Database successfull scanned");
            //Запуск асинхр потока с обнаружением лиц на ВХОДНОМ фото
            IList<DetectedFace> DetectedFaces = await DetectFaceRecognize(Client, $"{PathToBase}{InputImageFileName}", RecognitionModel);
            if (DetectedFaces != null) //Если найдено 1 лицо (не больше и не меньше) (метод DetectFaceRecognize не вернул null) тогда работаем
            {
                IList<SimilarFace> SimilarResults = await Client.Face.FindSimilarAsync(DetectedFaces[0].FaceId.Value, null, null, TargetFaceIds); //Azure FaceApi - Поиск похожих лиц поступившего в программу фото и []БД
                if (SimilarResults.Count != 0)
                {
                    foreach (var SimilarResult in SimilarResults)
                    {
                        Console.WriteLine($"Face from {InputImageFileName} & {DataBase.FindPhotoNameByGUID(SimilarResult.FaceId.Value)} (ID:{SimilarResult.FaceId.Value}) are similar with confidence: {SimilarResult.Confidence}.");
                        //Человек найден, можно сделать SET для FaceKey ---- Получаем ключ для человека из БД
                        FaceKey = await CalculateKey(Client, $"{PathToBase}{DataBase.FindPhotoNameByGUID(SimilarResult.FaceId.Value)}", RecognitionModel);
                        FaceName = DataBase.FindNameByGUID(SimilarResult.FaceId.Value);
                    }
                }
                else
                {
                    Console.WriteLine("[Error] no matches found");
                }
            }
            else
            {
                Console.WriteLine("[Return error]");
            }
        }


        private async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient Client, string CurrentImageFileName, string RecognitionModel) //Распознать лицо
        {
            using (FileStream stream = new FileStream(CurrentImageFileName, FileMode.Open))
            {
                IList<DetectedFace> DetectedFaces = await Client.Face.DetectWithStreamAsync(stream, recognitionModel: RecognitionModel); //Вызов метода, который на Azure FaceApi ищет лицо с текущего фото
                if (DetectedFaces == null || DetectedFaces.Count == 0) //Если на фото нет лиц - ошибка
                {
                    Console.WriteLine($"[Error] No face detected from image `{Path.GetFileName(CurrentImageFileName)}`.");
                    return null;
                }
                else if (DetectedFaces.Count > 1) //Если на фото больше одного лица - их слишком много
                {
                    Console.WriteLine($"[Error] Too more faces from image`{Path.GetFileName(CurrentImageFileName)}`.");
                    return null;
                }
                else //Благоприятный исход - найдено 1 лицо
                {
                    Console.WriteLine($"Face detected on `{Path.GetFileName(CurrentImageFileName)}`"); //Человек найден на /Изображение/
                //  foreach (var detectedFace in detectedFaces) {Console.WriteLine($"Assigned ID = {detectedFace.FaceId.Value}"); } //вывести guid человека
                    return DetectedFaces.ToList();
                }
            }
        }

        private async Task<int> CalculateKey(IFaceClient Client, string ResultImageFileName, string RecognitionModel) //Метод для вычисления ключа у необходимого изображения из БД
        {
            double Key = 0;
            using (FileStream stream = new FileStream(ResultImageFileName, FileMode.Open))
            {
                IList<DetectedFace> DetectedFaces = await Client.Face.DetectWithStreamAsync(stream, returnFaceLandmarks: true, recognitionModel: RecognitionModel);
                foreach (var DetectedFace in DetectedFaces)
                {
                    double[] Attributes  = new double[] //Набор атрибутов, по которым в будущем вычисляется ключ
{
                            DetectedFace.FaceLandmarks.EyebrowLeftInner.X,
                            DetectedFace.FaceLandmarks.EyebrowRightInner.X,
                            DetectedFace.FaceLandmarks.EyeLeftBottom.X,
                            DetectedFace.FaceLandmarks.EyeRightBottom.X
};
                    for (int i = 0; i < Attributes.Length; i++)
                    {
                        Key += Attributes[i];
                    }
                }
            }
            return Key.GetHashCode();
        }
    }
}
