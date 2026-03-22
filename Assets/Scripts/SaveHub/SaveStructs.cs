using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SaveRecordType
{
    floatNumber,
    dictionary,
    vector,
    boolean,
    integerNumber,
    quaternion
}

[System.Serializable]
public class SaveRecord
{
    public string recordName;
    public SaveRecordType recordType;
    public float floatValue;
    public Dictionary<string, float> dictValue;
    public Vector3 vecValue;
    public Quaternion quatValue;
    public bool boolValue;
    public int intValue;
}

[System.Serializable]
public class SaveRecordDict
{
    public List<string> dictKeys;
    public List<float> dictValues;
}

[System.Serializable]
public class SaveRecordVec
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class SaveRecordQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;
}

[System.Serializable]
public class SaveData
{
    public List<string> floatRecordNames = new List<string>();
    public List<float> floatRecords = new List<float>();
    public List<string> dictRecordNames = new List<string>();
    public List<SaveRecordDict> dictRecords = new List<SaveRecordDict>();
    public List<string> vecRecordNames = new List<string>();
    public List<SaveRecordVec> vecRecords = new List<SaveRecordVec>();
    public List<string> boolRecordNames = new List<string>();
    public List<float> boolRecords = new List<float>();
    public List<string> intRecordNames = new List<string>();
    public List<float> intRecords = new List<float>();
    public List<string> quatRecordNames = new List<string>();
    public List<SaveRecordQuaternion> quatRecords = new List<SaveRecordQuaternion>();

    public void AddData(SaveData other)
    {
        floatRecordNames.AddRange(other.floatRecordNames);
        floatRecords.AddRange(other.floatRecords);
        dictRecordNames.AddRange(other.dictRecordNames);
        dictRecords.AddRange(other.dictRecords);
        vecRecordNames.AddRange(other.vecRecordNames);
        vecRecords.AddRange(other.vecRecords);
        boolRecordNames.AddRange(other.boolRecordNames);
        boolRecords.AddRange(other.boolRecords);
        intRecordNames.AddRange(other.intRecordNames);
        intRecords.AddRange(other.intRecords);
        quatRecordNames.AddRange(other.quatRecordNames);
        quatRecords.AddRange(other.quatRecords);
    }

    public void Clear()
    {
        floatRecordNames.Clear();
        floatRecords.Clear();
        dictRecordNames.Clear();
        dictRecords.Clear();
        vecRecordNames.Clear();
        vecRecords.Clear();
        boolRecordNames.Clear();
        boolRecords.Clear();
        intRecordNames.Clear();
        intRecords.Clear();
        quatRecordNames.Clear();
        quatRecords.Clear();
    }
}

public class LoadedData
{
    public Dictionary<string, float> floatData;
    public Dictionary<string, Dictionary<string, float>> dictData;
    public Dictionary<string, Vector3> vecData;
    public Dictionary<string, Quaternion> quatData;
    public Dictionary<string, bool> boolData;
    public Dictionary<string, int> intData;

    public float GetData(string recordName, string id, float defaultVal)
    {
        string key = DataCompressor.GetRecordName(recordName, id);
        if (floatData.TryGetValue(key, out float value))
        {
            return value;
        }
        return defaultVal;
    }
    public int GetData(string recordName, string id, int defaultVal)
    {
        string key = DataCompressor.GetRecordName(recordName, id);
        if (intData.TryGetValue(key, out int value))
        {
            return value;
        }
        return defaultVal;
    }
    public bool GetData(string recordName, string id, bool defaultVal)
    {
        string key = DataCompressor.GetRecordName(recordName, id);
        if (boolData.TryGetValue(key, out bool value))
        {
            return value;
        }
        return defaultVal;
    }
    public Quaternion GetData(string recordName, string id, Quaternion defaultVal)
    {
        string key = DataCompressor.GetRecordName(recordName, id);
        if (quatData.TryGetValue(key, out Quaternion value))
        {
            return value;
        }
        return defaultVal;
    }
    public Vector3 GetData(string recordName, string id, Vector3 defaultVal)
    {
        string key = DataCompressor.GetRecordName(recordName, id);
        if (vecData.TryGetValue(key, out Vector3 value))
        {
            return value;
        }
        return defaultVal;
    }
    public Dictionary<string, float> GetData(string recordName, string id, Dictionary<string, float> defaultVal)
    {
        string key = DataCompressor.GetRecordName(recordName, id);
        if (dictData.TryGetValue(key, out Dictionary<string, float> value))
        {
            foreach (var item in value)
            {
                defaultVal[item.Key] = item.Value;
            }
            return defaultVal;
        }
        return defaultVal;
    }
}