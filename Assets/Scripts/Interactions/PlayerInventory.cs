using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemKind { Water, Food, FirstAid, Flashlight, Batteries, Radio, Backpack, Blanket, Toolkit, RepairTape, Towel, PlankSet }

/// <summary>How an item is carried (Docs/Design/04): pockets are unlimited; the armful holds one group at a time.</summary>
public enum CarryGroup { Pocket, Supplies, Planks, Towels, Blanket }

/// <summary>
/// Cane's pockets and armful. Taken pickups are deactivated, not destroyed, so dropping or storing them
/// puts the same scene objects back.
/// </summary>
public sealed class PlayerInventory : MonoBehaviour
{
    readonly List<Pickup> pockets = new List<Pickup>(), armful = new List<Pickup>();

    public event Action Changed;
    public IReadOnlyList<Pickup> Pockets => pockets;
    public IReadOnlyList<Pickup> Armful => armful;
    public CarryGroup? ArmfulGroup => armful.Count > 0 ? GroupOf(armful[0].Kind) : (CarryGroup?)null;
    /// <summary>Carrying anything in the arms stops sprinting.</summary>
    public bool Encumbered => armful.Count > 0;

    public static CarryGroup GroupOf(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.Water: case ItemKind.Food: return CarryGroup.Supplies;
            case ItemKind.PlankSet: return CarryGroup.Planks;
            case ItemKind.Towel: return CarryGroup.Towels;
            case ItemKind.Blanket: return CarryGroup.Blanket;
            default: return CarryGroup.Pocket;
        }
    }
    public static int Capacity(CarryGroup group)
    {
        switch (group)
        {
            case CarryGroup.Supplies: return 6;
            case CarryGroup.Planks: return 8;
            case CarryGroup.Towels: return 4;
            case CarryGroup.Blanket: return 1;
            default: return int.MaxValue;
        }
    }

    public int Count(ItemKind kind) => pockets.Count(p => p.Kind == kind) + armful.Count(p => p.Kind == kind);
    public bool Has(ItemKind kind) => Count(kind) > 0;
    /// <summary>Uses left across all carried items of a multi-use kind (repair tape rolls).</summary>
    public int Uses(ItemKind kind) => pockets.Where(p => p.Kind == kind).Sum(p => p.UsesLeft);

    /// <summary>Spends uses from the emptiest rolls first; used-up rolls are destroyed. False (and nothing spent) if short.</summary>
    public bool ConsumeUses(ItemKind kind, int count)
    {
        if (Uses(kind) < count) return false;
        foreach (var item in pockets.Where(p => p.Kind == kind && p.UsesLeft > 0).OrderBy(p => p.UsesLeft).ToList())
        {
            int take = Mathf.Min(count, item.UsesLeft);
            item.UsesLeft -= take; count -= take;
            if (item.UsesLeft == 0) { pockets.Remove(item); Destroy(item.gameObject); }
            if (count == 0) break;
        }
        Changed?.Invoke();
        return true;
    }

    /// <summary>Uses up one carried item of this kind from the arms (e.g. a plank set nailed to a window).</summary>
    public bool ConsumeArmful(ItemKind kind)
    {
        var item = armful.FirstOrDefault(p => p.Kind == kind);
        if (!item) return false;
        armful.Remove(item);
        Destroy(item.gameObject);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Why this pickup can't be taken right now, or null.</summary>
    public string CannotTake(Pickup item)
    {
        var group = GroupOf(item.Kind);
        if (group == CarryGroup.Pocket) return null;
        if (armful.Count > 0 && ArmfulGroup != group) return "Hands full [G] drop";
        if (armful.Count >= Capacity(group)) return "Can't carry more";
        return null;
    }

    public void Take(Pickup item)
    {
        (GroupOf(item.Kind) == CarryGroup.Pocket ? pockets : armful).Add(item);
        item.gameObject.SetActive(false);
        Changed?.Invoke();
    }

    /// <summary>Empties pockets and arms without putting anything back in the world (snapshot restore).</summary>
    public void Clear()
    {
        pockets.Clear(); armful.Clear();
        Changed?.Invoke();
    }

    /// <summary>Hands over everything in the arms (still inactive) and empties them.</summary>
    public List<Pickup> TakeArmful()
    {
        var items = new List<Pickup>(armful);
        armful.Clear();
        if (items.Count > 0) Changed?.Invoke();
        return items;
    }

    /// <summary>Puts the armful back in the world on the floor in front of the player.</summary>
    public void DropArmful(Transform player)
    {
        var items = TakeArmful();
        for (int i = 0; i < items.Count; i++)
        {
            Vector3 spot = player.position + player.forward * .6f + player.right * ((i % 3) - 1) * .22f + player.forward * (i / 3) * .22f;
            items[i].PlaceAt(spot + Vector3.up * .5f, null, true);
        }
    }

    /// <summary>One line for the debug HUD, e.g. "Pockets: Toolkit, Flashlight · Arms: Water x3 (3/6)".</summary>
    public string Summary()
    {
        string List(IEnumerable<Pickup> items) => string.Join(", ", items.GroupBy(p => p.Kind).Select(g => Pickup.DisplayName(g.Key) + (g.Count() > 1 ? " x" + g.Count() : "")
            + (g.Sum(p => p.UsesLeft) > 0 ? " (" + g.Sum(p => p.UsesLeft) + " uses)" : "")));
        string arms = armful.Count == 0 ? "empty" : List(armful) + " (" + armful.Count + "/" + Capacity(ArmfulGroup.Value) + ")";
        return "Pockets: " + (pockets.Count == 0 ? "empty" : List(pockets)) + "   ·   Arms: " + arms;
    }
}
