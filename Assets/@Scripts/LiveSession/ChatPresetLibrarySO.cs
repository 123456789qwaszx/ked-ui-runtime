using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatPresetLibrary", menuName = "Live/Chat Preset Library", order = 203)]
public sealed class ChatPresetLibrarySO : ScriptableObject
{
    [SerializeField] private ChatPresetSO[] presets;

    private Dictionary<ChatPreset, ChatPresetSO> map;

    private void OnEnable()
    {
        RebuildCache();
    }

    public void RebuildCache()
    {
        map = new Dictionary<ChatPreset, ChatPresetSO>(presets != null ? presets.Length : 0);

        if (presets == null || presets.Length == 0)
            return;

        for (int i = 0; i < presets.Length; i++)
        {
            var p = presets[i];
            if (!p) continue;

            var key = p.presetType;
            map[key] = p; // 마지막 wins
        }
    }

    public ChatPresetSO Get(ChatPreset preset)
    {
        if (map == null)
            RebuildCache();

        if (map != null && map.TryGetValue(preset, out var so) && so)
            return so;

        return null;
    }
}