using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FaceDetect
{
    class db
    {
        MySqlConnection connection = new MySqlConnection("server=localhost;port=3306;username=root;password=root;database=photonamesbase");
    
        private void OpenConnection ()
        {
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
        }
        private void CloseConnection()
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        private MySqlConnection GetConnection ()
        {
            return connection;
        }


        public List<string> GetAllPhotos() //Выгрузить из базы данных список названий фотографий
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

        public void UpdateGUID(Guid GUIDString, string PhotoName) //Обновление GUID для каждого пользователя
        {
            OpenConnection();
            MySqlCommand cmd = new MySqlCommand($"UPDATE photonamestable SET GUID ='{GUIDString}' WHERE PhotoNames = '{PhotoName}'", GetConnection());
            cmd.ExecuteNonQuery();
            CloseConnection();
        }

        public string FindPhotoNameByGUID(Guid GUIDString) //Найти в базе данных название фото по GUID номеру
        {
            OpenConnection();
            MySqlCommand cmd = new MySqlCommand($"SELECT PhotoNames FROM `photonamestable` WHERE GUID = '{GUIDString}'", GetConnection());
            string PhotoName = cmd.ExecuteScalar().ToString();
            CloseConnection();
            return PhotoName;
        }

        public string FindNameByGUID(Guid GUIDString) //Найти в базе данных название фото по GUID номеру
        {
            OpenConnection();
            MySqlCommand cmd = new MySqlCommand($"SELECT Name FROM `photonamestable` WHERE GUID = '{GUIDString}'", GetConnection());
            string Name = cmd.ExecuteScalar().ToString();
            CloseConnection();
            return Name;
        }
    }
}
