using UnityEngine;

/// <summary>
/// Put on an invisible COL_* collision proxy so aiming at it uses the real object it stands in for
/// (e.g. COL_Basement_ShelterStash → Basement_ShelterStash).
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionProxy : MonoBehaviour, IInteractable
{
    [SerializeField] MonoBehaviour target;

    IInteractable Target => target as IInteractable;
    public void Configure(MonoBehaviour interactable) => target = interactable;

    public string Prompt => Target.Prompt;
    public float HoldSeconds => Target.HoldSeconds;
    public string Unavailable(MapExplorer user) => Target.Unavailable(user);
    public void Use(MapExplorer user) => Target.Use(user);
}
