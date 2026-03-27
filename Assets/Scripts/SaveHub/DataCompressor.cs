using UnityEngine;
using System.Collections.Generic;

public static class DataCompressor
{
    public static string GetRecordName(string recordName, string id)
    {
        return recordName + "_" + id;
    }
    public static SaveData CollectData(SaveRecord[] records, string id)
    {
        SaveData res = new SaveData();
        foreach (var record in records)
        {
            switch (record.recordType)
            {
                case SaveRecordType.dictionary:
                    res.dictRecordNames.Add(GetRecordName(record.recordName, id));
                    res.dictRecords.Add(CompressDict(record.dictValue));
                    break;
                case SaveRecordType.vector:
                    res.vecRecordNames.Add(GetRecordName(record.recordName, id));
                    res.vecRecords.Add(CompressVec(record.vecValue));
                    break;
                case SaveRecordType.quaternion:
                    res.quatRecordNames.Add(GetRecordName(record.recordName, id));
                    res.quatRecords.Add(CompressQuaternion(record.quatValue));
                    break;
                case SaveRecordType.floatNumber:
                    res.floatRecordNames.Add(GetRecordName(record.recordName, id));
                    res.floatRecords.Add(record.floatValue);
                    break;
                case SaveRecordType.boolean:
                    res.boolRecordNames.Add(GetRecordName(record.recordName, id));
                    res.boolRecords.Add(record.boolValue ? 1 : 0);
                    break;
                case SaveRecordType.integerNumber:
                    res.intRecordNames.Add(GetRecordName(record.recordName, id));
                    res.intRecords.Add(record.intValue);
                    break;
                case SaveRecordType.stringValue:
                    res.stringRecordNames.Add(GetRecordName(record.recordName, id));
                    res.stringRecords.Add(record.stringValue);
                    break;
            }
        }
        return res;
    }

    public static LoadedData DecompressAllData(SaveData data)
    {
        LoadedData res = new()
        {
            floatData = new Dictionary<string, float>(),
            dictData = new Dictionary<string, Dictionary<string, float>>(),
            vecData = new Dictionary<string, Vector3>(),
            quatData = new Dictionary<string, Quaternion>(),
            boolData = new Dictionary<string, bool>(),
            intData = new Dictionary<string, int>(),
            stringData = new Dictionary<string, string>()
        };
        for (int i = 0; i < data.floatRecordNames.Count; i++)
        {
            res.floatData.Add(data.floatRecordNames[i], data.floatRecords[i]);
        }
        for (int i = 0; i < data.dictRecordNames.Count; i++)
        {
            res.dictData.Add(data.dictRecordNames[i], DecompressDict(data.dictRecords[i]));
        }
        for (int i = 0; i < data.vecRecordNames.Count; i++)
        {
            res.vecData.Add(data.vecRecordNames[i], DecompressVec(data.vecRecords[i]));
        }
        for (int i = 0; i < data.quatRecordNames.Count; i++)
        {
            res.quatData.Add(data.quatRecordNames[i], DecompressQuaternion(data.quatRecords[i]));
        }
        for (int i = 0; i < data.boolRecordNames.Count; i++)
        {
            res.boolData.Add(data.boolRecordNames[i], DecompressBool(data.boolRecords[i]));
        }
        for (int i = 0; i < data.intRecordNames.Count; i++)
        {
            res.intData.Add(data.intRecordNames[i], DecompressInt(data.intRecords[i]));
        }
        for (int i = 0; i < data.stringRecordNames.Count; i++)
        {
            res.stringData.Add(data.stringRecordNames[i], data.stringRecords[i]);
        }
        return res;
    }

    private static SaveRecordDict CompressDict(Dictionary<string, float> dict)
    {
        SaveRecordDict res = new SaveRecordDict();
        res.dictKeys = new List<string>(dict.Keys);
        res.dictValues = new List<float>(dict.Values);
        return res;
    }

    private static SaveRecordVec CompressVec(Vector3 vec)
    {
        SaveRecordVec res = new SaveRecordVec();
        res.x = vec.x;
        res.y = vec.y;
        res.z = vec.z;
        return res;
    }

    private static SaveRecordQuaternion CompressQuaternion(Quaternion quat)
    {
        SaveRecordQuaternion res = new SaveRecordQuaternion();
        res.x = quat.x;
        res.y = quat.y;
        res.z = quat.z;
        res.w = quat.w;
        return res;
    }

    private static Dictionary<string, float> DecompressDict(SaveRecordDict dict)
    {
        Dictionary<string, float> res = new Dictionary<string, float>();
        for (int i = 0; i < dict.dictKeys.Count; i++)
        {
            res.Add(dict.dictKeys[i], dict.dictValues[i]);
        }
        return res;
    }

    private static Vector3 DecompressVec(SaveRecordVec vec)
    {
        return new Vector3(vec.x, vec.y, vec.z);
    }
    private static Quaternion DecompressQuaternion(SaveRecordQuaternion quat)
    {
        return new Quaternion(quat.x, quat.y, quat.z, quat.w);
    }
    private static float DecompressFloat(float value)
    {
        return value;
    }
    private static bool DecompressBool(float value)
    {
        return value > 0.5f;
    }
    private static int DecompressInt(float value)
    {
        return (int)value;
    }
}