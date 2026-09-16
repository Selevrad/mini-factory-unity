using System;
using System.IO;
using UnityEngine;
using MiniFactory.Domain;

namespace MiniFactory.Services.Save
{
    public class JsonFileSaveService : ISaveService
    {
        private readonly string _filePath;

        public JsonFileSaveService(string fileName = "mini_factory_save.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public FactorySaveData Load()
        {
            if (!File.Exists(_filePath)) return null;
            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonUtility.FromJson<FactorySaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MiniFactory] Failed to load save, starting fresh: {e.Message}");
                return null;
            }
        }

        public void Save(FactorySaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MiniFactory] Failed to save: {e.Message}");
            }
        }
    }
}
