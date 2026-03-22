using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;

public class SaveHub : MonoBehaviour
{
    public static SaveHub Instance { get; private set; }

    public Action<Action<SaveRecord[], string>> OnSave;
    public Action<LoadedData> OnLoad;
    private LoadedData loadedData = new LoadedData();
    private SaveData accumulatedSaveData = new SaveData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void AddSaveData(SaveRecord[] records, string id)
    {
        accumulatedSaveData.AddData(DataCompressor.CollectData(records, id));
    }

    public void MakeSave(string fileName)
    {
        accumulatedSaveData.Clear();
        string path = Path.Combine(Application.persistentDataPath, fileName);
        // Debug.Log($"Saving data to {path}");
        var bf = new BinaryFormatter();
        OnSave?.Invoke(AddSaveData);
        using (var stream = new FileStream(path, FileMode.Create))
        {
            bf.Serialize(stream, accumulatedSaveData);
        }
    }

    public void LoadAllData(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"File {path} does not exist");
            return;
        }

        var bf = new BinaryFormatter();
        SaveData saveData;
        using (var stream = new FileStream(path, FileMode.Open))
        {
            saveData = bf.Deserialize(stream) as SaveData;
        }
        if (saveData == null)
        {
            Debug.LogError($"File {path} is empty");
            return;
        }

        loadedData = DataCompressor.DecompressAllData(saveData);
        OnLoad?.Invoke(loadedData);
        loadedData = null;
    }

    public void ShowLastSavedData()
    {
        string data = "Last saved data:\n\n";

        data += "Float data:\n";
        for (int i = 0; i < accumulatedSaveData.floatRecordNames.Count; i++)
        {
            data += $"\t{accumulatedSaveData.floatRecordNames[i]}: {accumulatedSaveData.floatRecords[i]}\n";
        }

        data += "Dict data:\n";
        for (int i = 0; i < accumulatedSaveData.dictRecordNames.Count; i++)
        {
            // data += $"{accumulatedSaveData.dictRecordNames[i]}: {accumulatedSaveData.dictRecords[i]}\n";
            data += $"\t{accumulatedSaveData.dictRecordNames[i]}:\n";
            for (int j = 0; j < accumulatedSaveData.dictRecords[i].dictKeys.Count; j++)
            {
                data += $"\t\t{accumulatedSaveData.dictRecords[i].dictKeys[j]}: {accumulatedSaveData.dictRecords[i].dictValues[j]}\n";
            }
        }

        data += "Vector data:\n";
        for (int i = 0; i < accumulatedSaveData.vecRecordNames.Count; i++)
        {
            data += $"\t{accumulatedSaveData.vecRecordNames[i]}: {accumulatedSaveData.vecRecords[i].x}, {accumulatedSaveData.vecRecords[i].y}, {accumulatedSaveData.vecRecords[i].z}\n";
        }

        data += "Quaternion data:\n";
        for (int i = 0; i < accumulatedSaveData.quatRecordNames.Count; i++)
        {
            data += $"\t{accumulatedSaveData.quatRecordNames[i]}: {accumulatedSaveData.quatRecords[i].x}, {accumulatedSaveData.quatRecords[i].y}, {accumulatedSaveData.quatRecords[i].z}, {accumulatedSaveData.quatRecords[i].w}\n";
        }

        data += "Bool data:\n";
        for (int i = 0; i < accumulatedSaveData.boolRecordNames.Count; i++)
        {
            data += $"\t{accumulatedSaveData.boolRecordNames[i]}: {accumulatedSaveData.boolRecords[i]}\n";
        }

        data += "Int data:\n";
        for (int i = 0; i < accumulatedSaveData.intRecordNames.Count; i++)
        {
            data += $"\t{accumulatedSaveData.intRecordNames[i]}: {accumulatedSaveData.intRecords[i]}\n";
        }
        Debug.Log(data);
    }

}