using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public enum Lang { English = 0, Turkish = 1 }

/// <summary>
/// English / Turkish text. The game's English strings stay in the code and the data files; every UI label passes through
/// <see cref="T"/> (see LocalizedLabel), which swaps in the Turkish version from Resources/HurrycaneData/lang_tr.txt
/// when Turkish is chosen in Settings. Unknown strings fall back to English, so nothing can show up blank.
///
/// lang_tr.txt has two kinds of lines ("=>" or a tab between the parts, # starts a comment, \n means a line break):
///   English text => Turkish text             exact match
///   ~regex => replacement                    pattern match for strings with numbers or names in them;
///                                            {1} is group 1 translated, {1!} is group 1 untouched
/// Rich-text tags (&lt;color&gt; etc.) are kept: when a whole string has no entry, each piece between tags is translated alone.
/// </summary>
public static class Loc
{
    const string PrefKey = "Hurrycane.Language";
    const int CacheLimit = 4000;

    public static Lang Current { get; private set; }
    /// <summary>Changes whenever the language does; labels compare it to know when to redo their text.</summary>
    public static int Version { get; private set; }
    public static event Action Changed;
    public static bool IsTurkish => Current == Lang.Turkish;

    sealed class Rule { public Regex pattern; public string replacement; }

    static readonly Dictionary<string, string> exact = new Dictionary<string, string>();
    static readonly List<Rule> rules = new List<Rule>();
    static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
    static bool loaded;
    static readonly Regex tagSplit = new Regex("(<[^>]+>)", RegexOptions.Compiled);
    static readonly Regex groupRef = new Regex(@"\{(\d)(!?)\}", RegexOptions.Compiled);

    /// <summary>Editor only: every English source string a label has shown, so the table can be checked for gaps.</summary>
    public static readonly HashSet<string> Seen = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Current = (Lang)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, 1);
        Load();
    }

    /// <summary>Re-reads lang_tr.txt (after editing it while the game runs in the editor).</summary>
    public static void Reload() { loaded = false; Load(); Version++; }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        exact.Clear(); rules.Clear(); cache.Clear();
        var file = Resources.Load<TextAsset>("HurrycaneData/lang_tr");
        if (!file) { Debug.LogWarning("Loc: lang_tr.txt missing from Resources/HurrycaneData"); return; }
        foreach (var raw in file.text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#') continue;
            // "English => Turkish" (or a tab between them).
            int sepLength = 4, tab = line.IndexOf(" => ", StringComparison.Ordinal);
            if (tab <= 0) { tab = line.IndexOf('\t'); sepLength = 1; }
            if (tab <= 0) continue;
            string left = Unescape(line.Substring(0, tab)), right = Unescape(line.Substring(tab + sepLength));
            if (left[0] == '~')
            {
                try { rules.Add(new Rule { pattern = new Regex("^" + left.Substring(1) + "$", RegexOptions.Compiled | RegexOptions.Singleline), replacement = right }); }
                catch (ArgumentException e) { Debug.LogWarning("Loc: bad rule " + left + ": " + e.Message); }
            }
            else exact[left] = right;
        }
    }

    static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t");

    public static void Set(Lang language)
    {
        if (language == Current) return;
        Current = language;
        PlayerPrefs.SetInt(PrefKey, (int)language);
        PlayerPrefs.Save();
        cache.Clear();
        Version++;
        Changed?.Invoke();
    }

    /// <summary>Records a string that was shown (editor only).</summary>
    public static void Observe(string source)
    {
        if (Application.isEditor && !string.IsNullOrWhiteSpace(source) && Seen.Count < 20000) Seen.Add(source);
    }

    /// <summary>Editor helper: the text pieces of a string (split at line breaks and tags) that have no translation.</summary>
    public static List<string> Gaps(string s)
    {
        var gaps = new List<string>();
        if (!loaded) Load();
        foreach (var line in s.Split('\n'))
            foreach (var part in tagSplit.Split(line))
            {
                if (part.Length == 0 || part[0] == '<') continue;
                string core = part.Trim();
                if (core.Length == 0 || !Regex.IsMatch(core, "[A-Za-z]{3}")) continue;
                if (!TryWhole(core, out _)) gaps.Add(core);
            }
        return gaps;
    }

    /// <summary>The string in the chosen language, or the original when there is no translation.</summary>
    public static string T(string source)
    {
        if (Current == Lang.English || string.IsNullOrEmpty(source)) return source;
        if (!loaded) Load();
        if (cache.TryGetValue(source, out var hit)) return hit;
        string result = Translate(source);
        if (cache.Count > CacheLimit) cache.Clear();
        cache[source] = result;
        return result;
    }

    /// <summary>Formats a translated template: Loc.F("Day {0}", 2).</summary>
    public static string F(string template, params object[] args) => string.Format(T(template), args);

    static string Translate(string s)
    {
        if (TryWhole(s, out var whole)) return whole;
        if (s.IndexOf('\n') >= 0)
        {
            var lines = s.Split('\n');
            bool changed = false;
            for (int i = 0; i < lines.Length; i++) { var t = TranslateTagged(lines[i]); if (t != lines[i]) { lines[i] = t; changed = true; } }
            if (changed) return string.Join("\n", lines);
        }
        return TranslateTagged(s);
    }

    /// <summary>Translates the text pieces between rich-text tags one by one.</summary>
    static string TranslateTagged(string s)
    {
        if (TryWhole(s, out var whole)) return whole;
        if (s.IndexOf('<') < 0) return s;
        var parts = tagSplit.Split(s);
        bool changed = false;
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0 || part[0] == '<') { sb.Append(part); continue; }
            string lead = part.Substring(0, part.Length - part.TrimStart().Length);
            string tail = part.Substring(part.TrimEnd().Length);
            string core = part.Trim();
            if (core.Length > 0 && TryWhole(core, out var translated)) { sb.Append(lead).Append(translated).Append(tail); changed = true; }
            else sb.Append(part);
        }
        return changed ? sb.ToString() : s;
    }

    static bool TryWhole(string s, out string result)
    {
        if (exact.TryGetValue(s, out result)) return true;
        foreach (var rule in rules)
        {
            var m = rule.pattern.Match(s);
            if (!m.Success) continue;
            string produced = groupRef.Replace(rule.replacement, g =>
            {
                int index = g.Groups[1].Value[0] - '0';
                string value = index < m.Groups.Count ? m.Groups[index].Value : "";
                return g.Groups[2].Length > 0 ? value : TranslateTagged(value);
            });
            if (produced == s) continue; // a pattern that changed nothing is not a translation
            result = produced;
            return true;
        }
        result = null;
        return false;
    }
}
