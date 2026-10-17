using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Opt-in Editor physics audit. Inert unless the request file exists.</summary>
public sealed class MapPlaytest : MonoBehaviour
{
#if UNITY_EDITOR
    IEnumerator Start()
    {
        string request = Path.GetFullPath(Application.dataPath + "/../.codex-map-playtest.request");
        if (!File.Exists(request)) yield break;
        File.Delete(request);
        var report = new List<string>();
        var explorer = GetComponent<MapExplorer>(); explorer.enabled = false;
        var cc = GetComponent<CharacterController>();
        var all = FindObjectsByType<Transform>();
        Transform Named(string name) => all.First(t => t.name == name);
        Vector3 origin = Named("MARKER_CoordOrigin").position;
        Vector3 ax = Named("MARKER_CoordX").position - origin, ay = Named("MARKER_CoordY").position - origin, az = Named("MARKER_CoordZ").position - origin;
        Vector3 World(Vector3 p) => origin + p.x * ax + p.y * ay + p.z * az;
        int openables=FindObjectsByType<CabinetOpenable>().Length;
        report.Add((openables==31?"PASS":"FAIL")+" openable doors/drawers: "+openables+" (10 single cabinet doors, 16 drawers, 5 room/appliance doors)");
        report.Add((FindObjectsByType<Animator>().All(a=>!a.enabled)?"PASS":"FAIL")+" preview animators disabled");
        var routes = new Dictionary<string, Vector3[]> {
            {"ground_loop", new [] {new Vector3(7,1.2f,0),new Vector3(7,2.35f,0),new Vector3(4.7f,2.35f,0),new Vector3(4.4f,4.75f,0),new Vector3(3.1f,4.75f,0),new Vector3(3.1f,5.85f,0),new Vector3(4.65f,5.85f,0),new Vector3(4.65f,7.5f,0),new Vector3(7,7.5f,0),new Vector3(7,1.2f,0)}},
            {"upper_stair_and_rooms", new [] {new Vector3(9.7f,1.2f,0),new Vector3(9.7f,2.2f,0),new Vector3(9.7f,8.9f,3.3f),new Vector3(7,8.9f,3.3f),new Vector3(7,7.35f,3.3f),new Vector3(4.7f,7.35f,3.3f),new Vector3(7,7.35f,3.3f),new Vector3(7,2,3.3f),new Vector3(4.8f,2,3.3f),new Vector3(7,2,3.3f),new Vector3(7,1.12f,3.3f),new Vector3(10.25f,1.12f,3.3f)}},
            {"basement_stair_and_aisles", new [] {new Vector3(7,7.5f,0),new Vector3(7,9,0),new Vector3(9.7f,9,0),new Vector3(9.7f,8.7f,0),new Vector3(9.7f,1.55f,-3.2f),new Vector3(7,1.55f,-3.2f),new Vector3(7,3,-3.2f),new Vector3(5.6f,3,-3.2f),new Vector3(5.6f,8.3f,-3.2f),new Vector3(1.65f,8.3f,-3.2f),new Vector3(1.65f,6,-3.2f),new Vector3(5.6f,6,-3.2f),new Vector3(5.6f,3,-3.2f),new Vector3(3.45f,3,-3.2f),new Vector3(3.45f,1.7f,-3.2f)}}
        };
        foreach (var route in routes)
        {
            cc.enabled = false; transform.position = World(route.Value[0]) + Vector3.up * .08f; cc.enabled = true;
            bool pass = true;
            for (int i = 1; i < route.Value.Length; i++)
            {
                Vector3 goal = World(route.Value[i]);
                int step;
                for (step = 0; step < 500; step++)
                {
                    Vector3 delta = goal - transform.position; delta.y = 0;
                    if (delta.magnitude < .09f && Mathf.Abs(goal.y - transform.position.y) < .3f) break;
                    cc.Move(Vector3.ClampMagnitude(delta, .045f) + Vector3.down * .045f);
                    if (step % 24 == 0) yield return null;
                }
                if (step >= 500)
                {
                    string nearby = string.Join(",",Physics.OverlapSphere(transform.position + Vector3.up, .65f).Select(c=>c.name));
                    report.Add("FAIL " + route.Key + " waypoint " + i + " actual " + transform.position + " goal " + goal + " nearby " + nearby);pass=false;break;
                }
            }
            if (pass) report.Add("PASS " + route.Key + ": actual CharacterController.Move through " + route.Value.Length + " waypoints.");
        }
        File.WriteAllLines(Path.GetFullPath(Application.dataPath + "/../HouseMap-playtest-report.txt"),report);
        cc.enabled=false;transform.position=explorer.spawnPoint;cc.enabled=true;
        Debug.Log("HOUSE_MAP_PLAYTEST_COMPLETE\n"+string.Join("\n",report));
        UnityEditor.EditorApplication.isPlaying=false;
    }
#endif
}
