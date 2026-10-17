using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Opt-in Editor Play-mode test of every door/drawer. Inert unless .codex-interaction-test.request exists.
/// Stands the player in front of each part, presses the normal E interaction, and compares poses with Blender.
/// </summary>
public sealed class HouseInteractionTest : MonoBehaviour
{
    public TextAsset reference;
#if UNITY_EDITOR
    [System.Serializable] sealed class Motion { public Item[] items; }
    [System.Serializable] sealed class Item { public string name; public bool drawer; public Vector3[] closed, opened; }

    const float Tolerance = .002f;
    readonly List<string> report = new List<string>();
    int failures;
    void Log(bool pass, string line) { report.Add((pass ? "PASS " : "FAIL ") + line); if (!pass) failures++; }

    IEnumerator Start()
    {
        string root = Path.GetFullPath(Application.dataPath + "/..");
        string request = Path.Combine(root, ".codex-interaction-test.request");
        if (!File.Exists(request)) yield break;
        File.Delete(request);
        Application.runInBackground = true;
        var errors = new List<string>();
        Application.logMessageReceived += (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); };

        var explorer = GetComponent<MapExplorer>(); explorer.enabled = false;
        var cc = GetComponent<CharacterController>();
        var cam = explorer.viewCamera.transform;
        var all = FindObjectsByType<Transform>().GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
        Vector3 origin = all["MARKER_CoordOrigin"].position;
        Vector3 ax = all["MARKER_CoordX"].position - origin, ay = all["MARKER_CoordY"].position - origin, az = all["MARKER_CoordZ"].position - origin;
        Vector3 W(Vector3 v) => origin + ax * v.x + ay * v.y + az * v.z;
        var items = JsonUtility.FromJson<Motion>(reference.text).items;
        var controls = items.ToDictionary(i => i.name, i => all[i.name].GetComponent<CabinetOpenable>());
        Log(controls.Values.All(c => c), "every Blender control has a CabinetOpenable (" + controls.Values.Count(c => c) + "/" + items.Length + ")");
        Log(FindObjectsByType<CabinetOpenable>().Length == items.Length, "no extra openable components");
        Log(FindObjectsByType<Animator>().All(a => !a.enabled), "Blender preview animators disabled");

        var saved = items.ToDictionary(i => i.name, i => controls[i.name].IsOpen);
        // Probe points rigidly attached to each pivot, taken from the Blender pose the part starts in.
        var probes = new Dictionary<string, Vector3[]>();
        var attached = new Dictionary<string, Dictionary<Transform, Matrix4x4>>();
        var pivotInverse = new Dictionary<string, Matrix4x4>();
        foreach (var item in items)
        {
            var pivot = all[item.name]; var control = controls[item.name];
            var start = control.IsOpen ? item.opened : item.closed;
            probes[item.name] = start.Select(p => pivot.InverseTransformPoint(W(p))).ToArray();
            float startError = start.Select((p, i) => Vector3.Distance(pivot.TransformPoint(probes[item.name][i]), W(p))).Max();
            Log(Vector3.Distance(pivot.position, W(start[0])) < Tolerance, item.name + " starts at Blender saved pose (" + (control.IsOpen ? "open" : "closed") + ")");
            attached[item.name] = pivot.GetComponentsInChildren<Transform>(true).Where(t => t != pivot).ToDictionary(t => t, t => t.localToWorldMatrix);
            pivotInverse[item.name] = pivot.worldToLocalMatrix;
        }
        float PoseError(Item item, bool open)
        {
            var pivot = all[item.name]; var target = open ? item.opened : item.closed;
            return Enumerable.Range(0, 4).Max(i => Vector3.Distance(pivot.TransformPoint(probes[item.name][i]), W(target[i])));
        }
        float AttachedError(Item item)
        {
            var pivot = all[item.name];
            return attached[item.name].Count == 0 ? 0 : attached[item.name].Max(kv => Vector3.Distance(kv.Key.position, (pivot.localToWorldMatrix * pivotInverse[item.name] * kv.Value).MultiplyPoint3x4(Vector3.zero)));
        }
        IEnumerator Settle(CabinetOpenable control) { for (float t = 0; t < 2.5f && control.IsMoving; t += Time.deltaTime) yield return null; yield return null; }
        Collider Face(CabinetOpenable control)
        {
            var colliders = control.GetComponentsInChildren<Collider>();
            return colliders.FirstOrDefault(c => c.name.Contains("Front") || c.name.Contains("BenchDrawer")) ?? colliders.OrderByDescending(c => c.bounds.size.sqrMagnitude).FirstOrDefault();
        }
        // Stand on the floor near the face, like a player would, and look at it. Prefers the given side.
        string aimNote = "";
        bool StandAndAim(CabinetOpenable control, Vector3 normal, float preferredSide)
        {
            var face = Face(control); if (!face) { aimNote = "no collider"; return false; }
            Vector3 center = face.bounds.center;
            var reasons = new List<string>();
            // Approach around the closed-face normal first, then around the face's current facing (e.g. an open door).
            Vector3 current = face.bounds.size.x < face.bounds.size.z ? Vector3.right : Vector3.forward;
            var directions = new[] { normal * preferredSide, -normal * preferredSide, current, -current };
            foreach (var direction in directions)
            foreach (float distance in new[] { .65f, .85f, 1.05f, 1.15f, 1.3f, 1.6f })
            foreach (float angle in new[] { 0f, 10f, -10f, 25f, -25f, 45f, -45f, 65f, -65f })
            {
                Vector3 stand = center + Quaternion.AngleAxis(angle, Vector3.up) * direction * distance;
                if (!Physics.Raycast(new Vector3(stand.x, center.y + .3f, stand.z), Vector3.down, out var floor, 3.5f, ~0, QueryTriggerInteraction.Ignore)) { reasons.Add("nofloor"); continue; }
                if (center.y - floor.point.y > 2.3f) { reasons.Add("toohigh"); continue; }
                cc.enabled = false;
                Vector3 feet = floor.point + Vector3.up * .03f;
                var blockers = Physics.OverlapCapsule(feet + Vector3.up * (cc.radius + .02f), feet + Vector3.up * (cc.height - cc.radius), cc.radius - .01f, ~0, QueryTriggerInteraction.Ignore);
                if (blockers.Length > 0) { reasons.Add("in:" + blockers[0].name); continue; }
                Vector3 flat = center - feet; flat.y = 0;
                transform.SetPositionAndRotation(feet, Quaternion.LookRotation(flat.normalized, Vector3.up));
                cc.enabled = true; Physics.SyncTransforms();
                // Aim at the middle of the face, then at its upper part and both ends, like a player would.
                var e = face.bounds.extents; Vector3 along = e.x > e.z ? new Vector3(e.x, 0, 0) : new Vector3(0, 0, e.z);
                RaycastHit hit = default;
                foreach (var point in new[] { center, center + Vector3.up * e.y * .6f, center + along * .6f, center - along * .6f })
                {
                    cam.LookAt(point);
                    if (Physics.Raycast(cam.position, cam.forward, out hit, explorer.reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<CabinetOpenable>() == control) { aimNote = ""; return true; }
                }
                reasons.Add("ray:" + (hit.collider ? hit.collider.name : "none"));
            }
            aimNote = string.Join(" ", reasons.GroupBy(r => r).Select(g => g.Key + "x" + g.Count()).Take(6));
            return false;
        }
        // Horizontal face normal, pointing the way the face moves when it opens (Blender probes).
        Vector3 Normal(Item item, CabinetOpenable control)
        {
            var face = Face(control); var pivot = all[item.name];
            Vector3 c0 = W(item.closed[0]), o0 = W(item.opened[0]);
            var delta = Quaternion.LookRotation(W(item.opened[1]) - o0, W(item.opened[2]) - o0) * Quaternion.Inverse(Quaternion.LookRotation(W(item.closed[1]) - c0, W(item.closed[2]) - c0));
            Vector3 currentRelative = face.bounds.center - pivot.position;
            Vector3 closedRelative = control.IsOpen ? Quaternion.Inverse(delta) * currentRelative : currentRelative;
            Vector3 travel = (o0 + delta * closedRelative) - (c0 + closedRelative);
            // Thin horizontal axis of the face (faces are axis-aligned), expressed in the closed pose.
            Vector3 n = face.bounds.size.x < face.bounds.size.z ? Vector3.right : Vector3.forward;
            if (control.IsOpen) n = Quaternion.Inverse(delta) * n;
            n.y = 0; n.Normalize();
            return Vector3.Dot(n, travel) >= 0 ? n : -n;
        }
        // Press E; if the part stops against the pinned player, step back like a player would.
        IEnumerator PressE(CabinetOpenable control, List<string> notes)
        {
            Vector3 before = transform.position;
            explorer.Interact();
            for (float t = 0; t < 2.5f && control.IsMoving && !control.Blocked; t += Time.deltaTime) yield return null;
            float pushed = Vector3.Distance(before, transform.position);
            if (pushed > .01f) notes.Add("pushed player back " + pushed.ToString("F2") + "m");
            for (int attempt = 0; attempt < 4; attempt++)
            {
                for (float t = 0; t < 2.5f && control.IsMoving && !control.Blocked; t += Time.deltaTime) yield return null;
                if (!control.Blocked) break;
                notes.Add("stopped at player, stepped back");
                Vector3 away = transform.position - Face(control).bounds.center; away.y = 0;
                cc.Move(away.normalized * .45f);
                yield return new WaitForSeconds(.2f);
            }
            yield return Settle(control);
        }

        // 0. Sweep every part through its whole motion: nothing it carries may pass through other geometry.
        //    Collision proxies are boxes, so box copies give exact overlap depths.
        {
            var boxRoot = new GameObject("SweepBoxes");
            var boxes = new Dictionary<MeshCollider, BoxCollider>();
            foreach (var mesh in FindObjectsByType<MeshCollider>())
            {
                if (!mesh.sharedMesh || mesh.transform.IsChildOf(transform)) continue;
                var go = new GameObject(mesh.name); go.transform.SetParent(boxRoot.transform, false);
                var box = go.AddComponent<BoxCollider>(); box.center = mesh.sharedMesh.bounds.center; box.size = mesh.sharedMesh.bounds.size;
                boxes[mesh] = box;
            }
            foreach (var item in items)
            {
                var control = controls[item.name]; bool start = control.IsOpen;
                var own = control.GetComponentsInChildren<MeshCollider>().Where(boxes.ContainsKey).ToArray();
                float worst = 0; string worstPair = "";
                for (int step = 0; step <= 20; step++)
                {
                    control.Snap(step / 20f); Physics.SyncTransforms();
                    foreach (var m in own)
                    {
                        Bounds mb = m.bounds;
                        foreach (var other in boxes)
                        {
                            if (other.Key.transform.IsChildOf(control.transform) || !mb.Intersects(other.Key.bounds)) continue;
                            var a = boxes[m]; var b = other.Value;
                            a.transform.localScale = m.transform.lossyScale; b.transform.localScale = other.Key.transform.lossyScale;
                            if (!Physics.ComputePenetration(a, m.transform.position, m.transform.rotation, b, other.Key.transform.position, other.Key.transform.rotation, out _, out float depth)) continue;
                            // A leaf resting in its own jamb at the hinge (inside the casing) is how a hinge works.
                            Vector3 pivotPos = control.transform.position, nearest = other.Key.bounds.ClosestPoint(pivotPos);
                            bool hingeJamb = !control.IsDrawer && depth < .035f && new Vector2(nearest.x - pivotPos.x, nearest.z - pivotPos.z).magnitude < .15f;
                            if (!hingeJamb && depth > worst) { worst = depth; worstPair = m.name + " x " + other.Key.name + " at " + (step * 5) + "% open"; }
                        }
                    }
                }
                control.Snap(start ? 1 : 0);
                // 1 cm allows for touching hinge-side casings/carcass edges.
                Log(worst < .01f, item.name + ": whole motion clear of other geometry (deepest " + worst.ToString("F3") + "m" + (worstPair.Length > 0 ? ", " + worstPair : "") + ")");
            }
            DestroyImmediate(boxRoot);
            Physics.SyncTransforms();
        }

        // 1. Every part through the real E-key raycast, standing in front of it.
        foreach (var item in items)
        {
            var control = controls[item.name];
            bool startOpen = control.IsOpen;
            var normal = Normal(item, control);
            var notes = new List<string>();
            // Cabinets open toward the player in front; a room door is opened from the side it swings away from.
            float openSide = control.IsRoomDoor ? -1 : 1;
            if (!StandAndAim(control, normal, startOpen ? 1 : openSide)) { Log(false, item.name + ": could not stand near it and aim (" + aimNote + ")"); continue; }
            if (FindTargetName() != control) { Log(false, item.name + ": E raycast does not select this part"); continue; }
            yield return PressE(control, notes);
            bool firstOk = control.IsOpen != startOpen && !control.IsMoving;
            float e1 = PoseError(item, control.IsOpen), a1 = AttachedError(item);
            if (!StandAndAim(control, normal, startOpen ? openSide : 1)) { Log(false, item.name + ": could not re-aim after first toggle (" + aimNote + ")"); continue; }
            bool selected = FindTargetName() == control;
            yield return PressE(control, notes);
            float e2 = PoseError(item, control.IsOpen), a2 = AttachedError(item);
            bool ok = firstOk && selected && control.IsOpen == startOpen && !control.IsMoving && Mathf.Max(e1, e2) < Tolerance && Mathf.Max(a1, a2) < Tolerance;
            Log(ok, item.name + ": " + (startOpen ? "close+open" : "open+close") + " via E; pose error " + Mathf.Max(e1, e2).ToString("F6") + "m; attached parts error " + Mathf.Max(a1, a2).ToString("F6") + "m" + (notes.Count > 0 ? " (" + string.Join("; ", notes.Distinct()) + ")" : ""));
        }
        foreach (var item in items) if (controls[item.name].IsOpen != (item.name.StartsWith("UF_"))) controls[item.name].SetOpen(item.name.StartsWith("UF_"));
        yield return new WaitForSeconds(1f);
        CabinetOpenable FindTargetName() => Physics.Raycast(cam.position, cam.forward, out var h, explorer.reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.collider.GetComponentInParent<CabinetOpenable>() : null;

        // 2. Reverse in mid-motion, then three complete cycles without drift.
        cc.enabled = false; transform.position = explorer.spawnPoint; cc.enabled = true; Physics.SyncTransforms();
        var initial = items.ToDictionary(i => i.name, i => controls[i.name].IsOpen);
        foreach (var item in items) controls[item.name].Toggle();
        for (int f = 0; f < 6; f++) yield return null;
        foreach (var item in items) controls[item.name].Toggle();
        yield return new WaitForSeconds(1f);
        Log(items.All(i => controls[i.name].IsOpen == initial[i.name] && !controls[i.name].IsMoving), "mid-motion reversal returns every part to its start");
        for (int cycle = 0; cycle < 3; cycle++)
        {
            foreach (var item in items) controls[item.name].SetOpen(true);
            yield return new WaitForSeconds(1f);
            float openError = items.Max(i => PoseError(i, true));
            foreach (var item in items) controls[item.name].SetOpen(false);
            yield return new WaitForSeconds(1f);
            float closedError = items.Max(i => PoseError(i, false));
            float attachedError = items.Max(AttachedError);
            Log(Mathf.Max(openError, closedError, attachedError) < Tolerance, "cycle " + cycle + ": open error " + openError.ToString("F6") + "m, closed error " + closedError.ToString("F6") + "m, attached drift " + attachedError.ToString("F6") + "m");
        }

        foreach (var item in items) controls[item.name].SetOpen(saved[item.name]);
        yield return new WaitForSeconds(1.2f);

        // 3. A door swinging into the player pushes them aside (or stops if pinned) instead of passing through.
        var door = controls["UF_MasterDoor_DoorPivot"]; var doorItem = items.First(i => i.name == door.name);
        door.SetOpen(true); yield return Settle(door);
        var leaf = Face(door); var pivotT = door.transform;
        Vector3 openCenter = leaf.bounds.center; Vector3 hinge = pivotT.position;
        door.SetOpen(false); yield return Settle(door); Vector3 closedCenter = leaf.bounds.center;
        door.SetOpen(true); yield return Settle(door);
        Vector3 mid = hinge + (((openCenter - hinge) + (closedCenter - hinge)) * .5f).normalized * Vector3.Distance(hinge, openCenter);
        Physics.Raycast(mid + Vector3.up * .3f, Vector3.down, out var midFloor, 3, ~0, QueryTriggerInteraction.Ignore);
        cc.enabled = false; transform.position = new Vector3(mid.x, midFloor.point.y + .03f, mid.z); cc.enabled = true; Physics.SyncTransforms();
        bool Penetrates()
        {
            Physics.SyncTransforms();
            Vector3 c = transform.TransformPoint(cc.center); float half = cc.height * .5f - cc.radius;
            return Physics.OverlapCapsule(c + Vector3.up * half, c - Vector3.up * half, cc.radius - .005f, ~0, QueryTriggerInteraction.Ignore).Any(h => h.transform.IsChildOf(pivotT));
        }
        Vector3 standing = transform.position;
        door.SetOpen(false);
        bool everPenetrated = false;
        for (float t = 0; t < 2.5f && door.IsMoving && !door.Blocked; t += Time.deltaTime) { everPenetrated |= Penetrates(); yield return null; }
        float moved = Vector3.Distance(standing, transform.position);
        Log(!everPenetrated && (!door.IsMoving || door.Blocked), "room door closing onto a player in its swing never passes through them (" + (door.Blocked ? "player pinned, door stopped" : "player pushed " + moved.ToString("F2") + "m, door closed") + ")");
        cc.enabled = false; transform.position = explorer.spawnPoint; cc.enabled = true; Physics.SyncTransforms();
        yield return Settle(door);
        Log(!door.IsMoving && !door.IsOpen && PoseError(doorItem, false) < Tolerance, "door finishes closing exactly at the Blender closed pose");
        door.SetOpen(true); yield return Settle(door);

        // 4. Everything is back in the saved Blender pose, and nothing logged errors.
        yield return new WaitForSeconds(.2f);
        var notHome = items.Where(i => controls[i.name].IsOpen != saved[i.name] || PoseError(i, saved[i.name]) >= Tolerance).Select(i => i.name + (controls[i.name].IsMoving ? " (moving)" : "") + " err " + PoseError(i, saved[i.name]).ToString("F4")).ToList();
        Log(notHome.Count == 0, "all parts returned to the Blender saved pose" + (notHome.Count > 0 ? ": " + string.Join(", ", notHome) : ""));
        Log(errors.Count == 0, "no errors or exceptions logged during the test" + (errors.Count > 0 ? ": " + errors[0] : ""));
        report.Insert(0, (failures == 0 ? "ALL PASSED" : failures + " FAILED") + " — " + items.Length + " doors/drawers, " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        File.WriteAllLines(Path.Combine(root, "HouseMap-interaction-test-report.txt"), report);
        Debug.Log("HOUSE_INTERACTION_TEST_COMPLETE\n" + string.Join("\n", report));
        cc.enabled = false; transform.position = explorer.spawnPoint; cc.enabled = true;
        UnityEditor.EditorApplication.isPlaying = false;
    }
#endif
}
