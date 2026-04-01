// ============================================================================
// SaveFileHandler.cs — Работа с файлами сохранений
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

namespace CultivationGame.Save
{
    /// <summary>
    /// Обработчик файлов сохранений.
    /// Предоставляет методы для чтения, записи, сжатия и шифрования.
    /// </summary>
    public static class SaveFileHandler
    {
        // === Constants ===
        
        private const string FILE_EXTENSION = ".sav";
        private const string BACKUP_EXTENSION = ".bak";
        private const string TEMP_EXTENSION = ".tmp";
        private const int MAX_BACKUPS = 5;
        
        // === Write Operations ===
        
        /// <summary>
        /// Записать данные в файл.
        /// </summary>
        public static bool WriteToFile(string path, string content, bool compress = false, bool encrypt = false, string key = null)
        {
            try
            {
                // Подготовка данных
                byte[] data = Encoding.UTF8.GetBytes(content);
                
                // Сжатие
                if (compress)
                {
                    data = Compress(data);
                }
                
                // Шифрование
                if (encrypt && !string.IsNullOrEmpty(key))
                {
                    data = Encrypt(data, key);
                }
                
                // Запись во временный файл
                string tempPath = path + TEMP_EXTENSION;
                File.WriteAllBytes(tempPath, data);
                
                // Создание резервной копии существующего файла
                if (File.Exists(path))
                {
                    CreateBackup(path);
                }
                
                // Замена файла
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write file: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Записать объект в JSON файл.
        /// </summary>
        public static bool WriteJson<T>(string path, T data, bool prettyPrint = true)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint);
                return WriteToFile(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write JSON: {e.Message}");
                return false;
            }
        }
        
        // === Read Operations ===
        
        /// <summary>
        /// Прочитать данные из файла.
        /// </summary>
        public static string ReadFromFile(string path, bool decompress = false, bool decrypt = false, string key = null)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"File not found: {path}");
                    return null;
                }
                
                byte[] data = File.ReadAllBytes(path);
                
                // Расшифровка
                if (decrypt && !string.IsNullOrEmpty(key))
                {
                    data = Decrypt(data, key);
                }
                
                // Распаковка
                if (decompress)
                {
                    data = Decompress(data);
                }
                
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read file: {e.Message}");
                
                // Пробуем восстановить из резервной копии
                string backupPath = path + BACKUP_EXTENSION;
                if (File.Exists(backupPath))
                {
                    Debug.Log("Attempting to restore from backup...");
                    return ReadFromFile(backupPath, decompress, decrypt, key);
                }
                
                return null;
            }
        }
        
        /// <summary>
        /// Прочитать JSON из файла.
        /// </summary>
        public static T ReadJson<T>(string path)
        {
            string json = ReadFromFile(path);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonUtility.FromJson<T>(json);
            }
            return default;
        }
        
        // === File Management ===
        
        /// <summary>
        /// Проверить существование файла.
        /// </summary>
        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }
        
        /// <summary>
        /// Удалить файл.
        /// </summary>
        public static bool DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                
                // Удаляем также резервные копии
                string backupPath = path + BACKUP_EXTENSION;
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete file: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Скопировать файл.
        /// </summary>
        public static bool CopyFile(string sourcePath, string destPath, bool overwrite = true)
        {
            try
            {
                File.Copy(sourcePath, destPath, overwrite);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to copy file: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Получить размер файла.
        /// </summary>
        public static long GetFileSize(string path)
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            return 0;
        }
        
        /// <summary>
        /// Получить время последнего изменения.
        /// </summary>
        public static DateTime GetLastModified(string path)
        {
            if (File.Exists(path))
            {
                return File.GetLastWriteTime(path);
            }
            return DateTime.MinValue;
        }
        
        // === Backup Operations ===
        
        /// <summary>
        /// Создать резервную копию.
        /// </summary>
        public static bool CreateBackup(string path)
        {
            try
            {
                string backupPath = path + BACKUP_EXTENSION;
                
                // Удаляем старую резервную копию
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                
                // Создаём новую
                File.Copy(path, backupPath);
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create backup: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Восстановить из резервной копии.
        /// </summary>
        public static bool RestoreFromBackup(string path)
        {
            try
            {
                string backupPath = path + BACKUP_EXTENSION;
                
                if (!File.Exists(backupPath))
                {
                    Debug.LogWarning($"Backup not found: {backupPath}");
                    return false;
                }
                
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                
                File.Copy(backupPath, path);
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to restore from backup: {e.Message}");
                return false;
            }
        }
        
        // === Compression ===
        
        /// <summary>
        /// Сжать данные.
        /// </summary>
        public static byte[] Compress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }
        
        /// <summary>
        /// Распаковать данные.
        /// </summary>
        public static byte[] Decompress(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }
        
        // === Encryption ===
        
        /// <summary>
        /// Зашифровать данные.
        /// </summary>
        public static byte[] Encrypt(byte[] data, string key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKey(key);
                aes.IV = new byte[16]; // Простой IV для демо
                
                using (var encryptor = aes.CreateEncryptor())
                using (var output = new MemoryStream())
                {
                    using (var crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                    {
                        crypto.Write(data, 0, data.Length);
                    }
                    return output.ToArray();
                }
            }
        }
        
        /// <summary>
        /// Расшифровать данные.
        /// </summary>
        public static byte[] Decrypt(byte[] data, string key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKey(key);
                aes.IV = new byte[16];
                
                using (var decryptor = aes.CreateDecryptor())
                using (var input = new MemoryStream(data))
                using (var crypto = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
                using (var output = new MemoryStream())
                {
                    crypto.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
        
        private static byte[] DeriveKey(string password)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
        
        // === Hash ===
        
        /// <summary>
        /// Вычислить хеш файла.
        /// </summary>
        public static string CalculateFileHash(string path)
        {
            if (!File.Exists(path)) return null;
            
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        
        // === Directory Operations ===
        
        /// <summary>
        /// Убедиться, что директория существует.
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        /// <summary>
        /// Очистить директорию.
        /// </summary>
        public static void ClearDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }
                foreach (string dir in Directory.GetDirectories(path))
                {
                    Directory.Delete(dir, true);
                }
            }
        }
        
        /// <summary>
        /// Получить размер директории.
        /// </summary>
        public static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            
            long size = 0;
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                size += new FileInfo(file).Length;
            }
            return size;
        }
    }
}
