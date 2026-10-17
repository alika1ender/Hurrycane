/// <summary>Anything the player can use with E. Found by raycast from the camera (MapExplorer).</summary>
public interface IInteractable
{
    /// <summary>Verb phrase for the prompt, e.g. "Take water" or "Open drawer".</summary>
    string Prompt { get; }
    /// <summary>Seconds E must be held before use. 0 means a single tap.</summary>
    float HoldSeconds { get; }
    /// <summary>Why the player can't use this right now ("Need: toolkit"), or null when usable.</summary>
    string Unavailable(MapExplorer user);
    void Use(MapExplorer user);
}
