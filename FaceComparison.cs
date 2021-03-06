﻿using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace EncryptDecryptFilesByFace
{
    class FaceComparison
    {
        /// <summary>
        /// Создание FaceClient для работы с CognitiveServices
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="key"></param>
        /// <returns>FaceClient</returns>
        public IFaceClient Authenticate(string endpoint, string key)
        { return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint }; }


        public string FaceName { get; private set; }
        public byte[] FaceKey { get; private set; }
        public byte[] FaceIV { get; private set; }


        /// <summary>
        /// Метод для тестирования создаваемых Key, IV для каждого человека в БД
        /// Запись imageName, Key, IV происходит в текстовый файл
        /// </summary>
        /// <param name="Client"> FaceClient </param>
        /// <param name="PathToBase"> Папка с фотографиями </param>
        /// <param name="RecognitionModel"></param>
        /// <param name="File"> Файл в который происходит запись </param>
        /// <returns>Записанный текстовый файл</returns>
        /* 
        public async Task testKeyPairs(IFaceClient Client, string PathToBase, string RecognitionModel, string File)
        {
            Console.WriteLine("Start test scan Database");
            SHA512 Hash512 = new SHA512Managed();
            db DataBase = new db();
            IList<string> DBImageFileNames = DataBase.GetAllPhotos();
            IList<Guid?> TargetFaceIds = new List<Guid?>();
            foreach (var ImageFileName in DBImageFileNames) //Создание ID для каждой картинки из базы
            {
                var Faces = await DetectFaceRecognize(Client, $"{PathToBase}{ImageFileName}", RecognitionModel); //Запуск асинхр потока с поиском лиц
                if (Faces == null) { return; } //На случай, если лица не обнаружены
                TargetFaceIds.Add(Faces[0].FaceId.Value); //Засунуть в список человека
                DataBase.UpdateGUID(Faces[0].FaceId.Value, ImageFileName); //Обновить значение ID для человека в стобце GUID БД
                var testKey = await CalculateKey(Client, $"{PathToBase}{DataBase.FindPhotoNameByGUID(Faces[0].FaceId.Value)}", RecognitionModel);
                var testIV = await CalculateIV(Client, $"{PathToBase}{DataBase.FindPhotoNameByGUID(Faces[0].FaceId.Value)}", RecognitionModel);
                var tempKey = Hash512.ComputeHash(testKey);
                var tempIV = Hash512.ComputeHash(testIV);
                testKey = new byte[16];
                testIV = new byte[16];
                for (int i = 0; i < 16; i++) //Забираем первые 16 байт в SessionKey, SessionIV из их SHA512 хеша
                {
                    testKey[i] = tempKey[i];
                    testIV[i] = tempIV[i];
                }
                using (StreamWriter sw = new StreamWriter(File, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine($"{ImageFileName} \r\n Key: {BitConverter.ToString(testKey)} \r\n IV: {BitConverter.ToString(testIV)}");
                }
            }
        }
        */


        /// <summary>
        /// Находит совпадение лица входного фото с фотографиями из БД
        /// </summary>
        /// <param name="Client"> FaceClient </param>
        /// <param name="PathToBase"> Папка с фотографиями </param>
        /// <param name="RecognitionModel"></param>
        /// <param name="InputImageFileName"> Входное фото которое сравниваем с БД </param>
        /// <returns></returns>
        public async Task FindSimilar(IFaceClient Client, string PathToBase, string RecognitionModel, string InputImageFileName)
        {
            //Начинаем сканирование базы данных
            Console.WriteLine("Start scan Database");
            db DataBase = new db();
            IList<string> DBImageFileNames = DataBase.GetAllPhotos(); //База данных названий картинок
            IList<Guid?> TargetFaceIds = new List<Guid?>(); //Создание нового списка ID для людей
            foreach (var ImageFileName in DBImageFileNames) //Создание ID для каждой картинки из базы
            {
                var Faces = await DetectFaceRecognize(Client, $"{PathToBase}{ImageFileName}", RecognitionModel); //Запуск асинхр потока с поиском лиц
                if (Faces == null) { return; } //На случай, если лица не обнаружены
                TargetFaceIds.Add(Faces[0].FaceId.Value); //Засунуть в список человека
                DataBase.UpdateGUID(Faces[0].FaceId.Value, ImageFileName); //Обновить значение ID у данного человека в БД
            }
            Console.WriteLine("Database successfull scanned");


            //Сканирование входного фото
            IList<DetectedFace> DetectedFaces = await DetectFaceRecognize(Client, $"{PathToBase}{InputImageFileName}", RecognitionModel);
            if (DetectedFaces != null) //Если найдено 1 лицо (не больше и не меньше) (метод DetectFaceRecognize не вернул null) тогда работаем
            {
                IList<SimilarFace> SimilarResults = await Client.Face.FindSimilarAsync(DetectedFaces[0].FaceId.Value, null, null, TargetFaceIds); //Azure FaceApi - Поиск похожих лиц поступившего в программу фото и []БД
                if (SimilarResults.Count != 0) //Если лица на входном фото обнаружены
                {
                    foreach (var SimilarResult in SimilarResults)
                    {
                        if (SimilarResult.Confidence >= 0.9) //Если совпадение больше 90% (Поставил потому, что с моим братом по фото совпадение аж 70% !!!)
                        {
                            Console.WriteLine($"Face from {InputImageFileName} & {DataBase.FindPhotoNameByGUID(SimilarResult.FaceId.Value)} (ID:{SimilarResult.FaceId.Value}) are similar with confidence: {SimilarResult.Confidence}.");
                            //Человек найден, можно получить его Имя; Сделать SET для FaceKey, FaceIV
                            FaceName = DataBase.FindNameByGUID(SimilarResult.FaceId.Value); //Найдем имя человека по свежему ID из БД
                            FaceKey = await CalculateKey(Client, $"{PathToBase}{DataBase.FindPhotoNameByGUID(SimilarResult.FaceId.Value)}", RecognitionModel);
                            FaceIV = await CalculateIV(Client, $"{PathToBase}{DataBase.FindPhotoNameByGUID(SimilarResult.FaceId.Value)}", RecognitionModel);
                        }
                    }
                    if (FaceKey == null || FaceName == null || FaceIV == null) //Если подходящее лицо с совпадением 90% не найдено (вдруг на вход поступит слишком "плохое" фото), и совпадение составит лишь 50-90%
                    {
                        throw new Exception("[Error] We cant identify your face, please retry");
                    }
                }
                else //Если не найдено ни одно даже близко похожее совпадение с входным фото
                {
                    throw new Exception("[Error] no matches found");
                }
            }
            else
            {
                throw new Exception("[Error] Bad photo (We cant found face on your photo)");
            }
        }

        /// <summary>
        /// Поиск лиц на фото
        /// Метод вызывается из метода FindSimilar
        /// </summary>
        /// <param name="Client"> FaceClient </param>
        /// <param name="CurrentImageFileName"> Название текущей фотографии </param>
        /// <param name="RecognitionModel"></param>
        /// <returns>DetectedFaces.ToList()</returns>
        private async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient Client, string CurrentImageFileName, string RecognitionModel)
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


        /// <summary>
        /// На вход поступает название совпадающей фотографии из БД
        /// Вычисляются уникальные ориентиры лица, на их основе строится byte[] Key
        /// </summary>
        /// <param name="Client"> FaceClient </param>
        /// <param name="ResultImageFileName"> Подходящяя фотография из БД </param>
        /// <param name="RecognitionModel"></param>
        /// <returns>Byte[] Key который в дальнейшем обрабатывается в классе CryptographicOperations </returns>
        private async Task<byte[]> CalculateKey(IFaceClient Client, string ResultImageFileName, string RecognitionModel) //Метод для вычисления ключа у необходимого изображения из БД
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
                            DetectedFace.FaceLandmarks.EyeRightBottom.X,
                            DetectedFace.FaceLandmarks.UpperLipTop.X,
                            DetectedFace.FaceLandmarks.UpperLipBottom.X,
                            DetectedFace.FaceLandmarks.MouthLeft.X,
                            DetectedFace.FaceLandmarks.PupilRight.X
};
                    for (int i = 0; i < Attributes.Length; i++)
                    {
                        Key += Attributes[i];
                    }
                }
            }
            return BitConverter.GetBytes(Key);
        }


        /// <summary>
        /// На вход поступает название совпадающей фотографии из БД
        /// Вычисляются уникальные ориентиры лица, на их основе строится byte[] InitVector
        /// </summary>
        /// <param name="Client"> FaceClient </param>
        /// <param name="ResultImageFileName"> Подходящяя фотография из БД </param>
        /// <param name="RecognitionModel"></param>
        /// <returns>Byte[] InitVector который в дальнейшем обрабатывается в классе CryptographicOperations </returns>
        private async Task<byte[]> CalculateIV(IFaceClient Client, string ResultImageFileName, string RecognitionModel) //Метод для вычисления InititVector у необходимого изображения из БД
        {
            double IV = 0;
            using (FileStream stream = new FileStream(ResultImageFileName, FileMode.Open))
            {
                IList<DetectedFace> DetectedFaces = await Client.Face.DetectWithStreamAsync(stream, returnFaceLandmarks: true, recognitionModel: RecognitionModel);
                foreach (var DetectedFace in DetectedFaces)
                {
                    double[] Attributes = new double[] //Набор атрибутов, по которым в будущем вычисляется ключ
{
                            DetectedFace.FaceLandmarks.EyebrowLeftInner.Y,
                            DetectedFace.FaceLandmarks.EyebrowRightInner.Y,
                            DetectedFace.FaceLandmarks.EyeLeftBottom.Y,
                            DetectedFace.FaceLandmarks.EyeRightBottom.Y,
                            DetectedFace.FaceLandmarks.UpperLipTop.Y,
                            DetectedFace.FaceLandmarks.UpperLipBottom.Y,
                            DetectedFace.FaceLandmarks.MouthLeft.Y,
                            DetectedFace.FaceLandmarks.PupilRight.Y
};
                    for (int i = 0; i < Attributes.Length; i++)
                    {
                        IV += Attributes[i];
                    }
                }
            }
            return BitConverter.GetBytes(IV);
        }
    }
}
