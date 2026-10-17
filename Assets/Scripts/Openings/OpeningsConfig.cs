using System;
using UnityEngine;

/// <summary>Tuning and the list of windows/door, read from Resources/HurrycaneData/openings.json (Docs/Design/02, 03).</summary>
[Serializable]
public sealed class OpeningsConfig
{
    /// <summary>Which day's openings are in play when the scene starts. Temporary until HouseState runs the days.</summary>
    public int startDay = 1;
    public float reinforceWindowSeconds = 5, braceDoorSeconds = 8, repairWindowSeconds = 8, repairDoorSeconds = 12, upgradeWindowSeconds = 5;
    public int tapeUsesPerRoll = 4, doorRepairTapeUses = 2, plankSetsInStack = 18;
    public float warningSeconds = 3;
    public OpeningDefinition[] openings = new OpeningDefinition[0];

    public static OpeningsConfig Load()
    {
        var file = Resources.Load<TextAsset>("HurrycaneData/openings");
        if (!file) { Debug.LogError("openings.json missing from Resources/HurrycaneData"); return new OpeningsConfig(); }
        return JsonUtility.FromJson<OpeningsConfig>(file.text);
    }
}

[Serializable]
public sealed class OpeningDefinition
{
    public string id, collider, room;
    public int firstDay = 1;
    public bool isDoor;
}
