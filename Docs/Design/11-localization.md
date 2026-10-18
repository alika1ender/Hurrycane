# 11 · Localization (English / Turkish)

> **Status:** Review · Files: `Assets/Scripts/Localization/`, `Assets/Scripts/UI/SettingsOverlay.cs`, `Assets/Resources/HurrycaneData/lang_tr.txt`

The player picks **English or Türkçe** in **Settings** (main menu and pause menu). The choice is saved in PlayerPrefs (`Hurrycane.Language`) and every label changes at once. The game starts in English.

## How it works
- The game's English text stays where it was (code and the JSON data files). Nothing was rewritten.
- `UiKit.Label` adds a `LocalizedLabel` to every UI `Text`. The game keeps writing English into the label; `LocalizedLabel` swaps in the translation just before the frame is drawn (`Loc.T`) and again when the language changes.
- `Loc` looks the text up in `lang_tr.txt`. No entry means the English shows, so nothing is ever blank.
- Strings with numbers or names use pattern lines. Text inside rich-text tags (`<color>`, `<size>`) is translated piece by piece.
- Artwork with text baked into the picture cannot be translated. The phone alert artwork is shown in English; in Turkish the drawn phone (real labels) is used instead (`LocalizedSwap`).

## `lang_tr.txt`
```
English text => Turkish text           exact match
~regex => replacement                   pattern; {1} = group 1 translated, {1!} = group 1 as is
```
`#` starts a comment, `\n` is a line break. The first matching pattern wins, so keep specific ones above general ones.

## Adding or changing text
1. Write the English text in code or JSON as usual.
2. Add a line to `lang_tr.txt` with the exact same English text.
3. In the editor you can reload the table while playing: `Loc.Reload()`.
4. To find gaps: in Play mode switch to Turkish, play through, then call `Loc.Gaps(s)` on the strings in `Loc.Seen` (editor only). Pieces it returns have no translation.

## Not translated on purpose
Names (BROSKI, HURRYCANE), key names (Tab, Esc, Shift, Space), debug and log messages, and the TV broadcast pictures.

## World signs (3D)

The signs in the house are meshes with modelled letters, so `Loc` cannot reach them. `LocalizedSign` (Assets/Scripts/Localization) handles them:
in Turkish the letter materials are painted over with the board's colour and real text is drawn in front; in English nothing is touched.
Each sign's lines, positions and sizes are listed in `LocalizedSign.Signs`; the Turkish text comes from `lang_tr.txt` like everything else.
To add a sign: add a `Layout` entry (object name, letter material slots, facing direction, lines) and the English => Turkish lines.
