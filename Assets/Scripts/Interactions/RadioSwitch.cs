using UnityEngine;

/// <summary>The ground-floor radio. It stays where it is and is switched on with E ("Turn on the radio" task).</summary>
[DisallowMultipleComponent]
public sealed class RadioSwitch : MonoBehaviour, IInteractable
{
    public bool IsOn { get; private set; }

    public string Prompt => IsOn ? "Turn off radio" : "Turn on radio";
    public float HoldSeconds => 0;
    public string Unavailable(MapExplorer user) => null;
    public void Use(MapExplorer user) => IsOn = !IsOn;
}
