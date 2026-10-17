using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Start-of-day snapshots as JSON files: persistentDataPath/saves/day{N}.json (Docs/Design/06).</summary>
public static class SaveSystem
{
    const int MaxDay = 3;
    static string Folder => Path.Combine(Application.persistentDataPath, "saves");
    public static string PathFor(int day) => Path.Combine(Folder, "day" + day + ".json");

    public static void Save(HouseSnapshot snapshot)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(PathFor(snapshot.day), JsonUtility.ToJson(snapshot, true));
    }

    public static HouseSnapshot Load(int day)
    {
        string path = PathFor(day);
        return File.Exists(path) ? JsonUtility.FromJson<HouseSnapshot>(File.ReadAllText(path)) : null;
    }

    /// <summary>Every saved day in order, for the restart cards.</summary>
    public static List<HouseSnapshot> All()
    {
        var list = new List<HouseSnapshot>();
        for (int day = 1; day <= MaxDay; day++) { var s = Load(day); if (s != null) list.Add(s); }
        return list;
    }

    /// <summary>Restarting from a day throws away the later days.</summary>
    public static void DeleteAfter(int day)
    {
        for (int later = day + 1; later <= MaxDay; later++) if (File.Exists(PathFor(later))) File.Delete(PathFor(later));
    }

    public static void DeleteAll() => DeleteAfter(0);
}
