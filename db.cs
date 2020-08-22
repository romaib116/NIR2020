using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace EncryptDecryptFilesByFace
{
    /// <summary>
    /// DB structure photonamesbase
    /// Id (primary key)
    /// PhotoNames (name of photo with expansion)
    /// GUID (Program auto set this num) can be null
    /// Name (name of person)
    /// </summary>

    class db
    {
        /// <summary>
        /// Создание подключения к ДБ
        /// Localhost:3306
        /// root: root
        /// БД - photonamesbase
        /// </summary>
        MySqlConnection connection = new MySqlConnection("server=localhost;port=3306;username=root;password=root;database=photonamesbase");
    
        /// <summary>
        /// Открытие подключения
        /// </summary>
        private void OpenConnection ()
        {
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
        }


        /// <summary>
        /// Закрытие подключения
        /// </summary>
        private void CloseConnection()
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Подключение к БД
        /// </summary>
        /// <returns> connection </returns>
        private MySqlConnection GetConnection ()
        {
            return connection;
        }

        /// <summary>
        /// Метод получающий названия всех фотографий из БД
        /// </summary>
        /// <returns> Список(List) названий фотографий</returns>
        public IList<string> GetAllPhotos()
        {
            OpenConnection();
            MySqlCommand cmd = new MySqlCommand("SELECT PhotoNames FROM `photonamestable`", GetConnection());
            var PhotosFromDataBase = new List<string>();
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                PhotosFromDataBase.Add(reader[0].ToString());
            }
            reader.Close();
            CloseConnection();
            return PhotosFromDataBase;
        }


        /// <summary>
        /// Обновление ID для фотографии
        /// </summary>
        /// <param name="GUIDString"> ID </param>
        /// <param name="PhotoName"> Название фотографии</param>
        public void UpdateGUID(Guid GUIDString, string PhotoName)
        {
            OpenConnection();
            MySqlCommand cmd = new MySqlCommand($"UPDATE photonamestable SET GUID ='{GUIDString}' WHERE PhotoNames = '{PhotoName}'", GetConnection());
            cmd.ExecuteNonQuery();
            CloseConnection();
        }

        /// <summary>
        /// Поиск фотографии по ID
        /// </summary>
        /// <param name="GUIDString"> ID </param>
        /// <returns> Название фотографии </returns>
        public string FindPhotoNameByGUID(Guid GUIDString) 
        {
            OpenConnection();
            MySqlCommand cmd = new MySqlCommand($"SELECT PhotoNames FROM `photonamestable` WHERE GUID = '{GUIDString}'", GetConnection());
            string PhotoName = cmd.ExecuteScalar().ToString();
            CloseConnection();
            return PhotoName;
        }

        /// <summary>
        /// Поиск имени человека по его ID
        /// </summary>
        /// <param name="GUIDString">ID</param>
        /// <returns></returns>
        public string FindNameByGUID(Guid GUIDString)
        {
            OpenConnection();
            MySqlCommand cmd = new MySqlCommand($"SELECT Name FROM `photonamestable` WHERE GUID = '{GUIDString}'", GetConnection());
            string Name = cmd.ExecuteScalar().ToString();
            CloseConnection();
            return Name;
        }

        /// <summary>
        /// Вставка в БД входных в метод данных
        /// </summary>
        /// <param name="PhotoName"> Название фотографии </param>
        /// <param name="Name"> Имя человека </param>
        /// <param name="GUID"> ID </param>
        /*
           public void ParsePhotoToDB(string PhotoName, string Name, int GUID)
         {
             OpenConnection();
             MySqlCommand cmd = new MySqlCommand($"INSERT photonamestable(PhotoNames, Name, GUID) VALUES ('{PhotoName}','{Name}',{GUID})", GetConnection());
             cmd.ExecuteNonQuery();
             CloseConnection();
         }
        */
    }
}
