using UnityEngine;
using UDA2.Audio;
using System;

namespace Game.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Location")]
    public class BattleLocationData : ScriptableObject
    {
        [Header("Meta")]
        [Tooltip("Stable identifier used for saves (runtime). If empty, will be auto-filled from asset name.")]
        public string id;

        [Header("Visuals")]
        public Sprite background;

        [Header("Audio")]
        [Tooltip("Optional primary music cue (legacy single track).")]
        public AudioCue musicCue;

        // Backward compatibility: older locations may still use a raw clip.
        [Tooltip("Optional primary music clip (legacy single track).")]
        public AudioClip music;

        [Header("Audio - Music Playlist")]
        [Tooltip("Optional multi-track battle music playlist via cues.")]
        public AudioCue[] musicPlaylist = Array.Empty<AudioCue>();

        [Tooltip("Optional multi-track battle music playlist via clips (fallback).")]
        public AudioClip[] musicPlaylistClips = Array.Empty<AudioClip>();

        [Tooltip("If true, playlist loops forever. If false, it stops after the last track.")]
        public bool loopMusicPlaylist = true;

        [Tooltip("If true, each playlist cycle starts from a random entry.")]
        public bool randomStartMusicPlaylist = true;

        [Tooltip("Optional delay range before next track starts after previous one ends.")]
        public Vector2 musicTrackStartDelaySeconds = Vector2.zero;

        [Header("Audio - Ambient Sounds")]
        [Tooltip("Optional ambient sound groups (water, birds, wind...) played during battle.")]
        public AmbientSoundGroup[] ambientSoundGroups = Array.Empty<AmbientSoundGroup>();

        [Serializable]
        public sealed class AmbientSoundGroup
        {
            [Tooltip("Optional label for readability.")]
            public string name;

            [Tooltip("If enabled, group keeps playing random sounds forever using interval range.")]
            public bool randomLoop = true;

            [Tooltip("Delay range between sound starts in seconds.")]
            public Vector2 intervalSeconds = new Vector2(3f, 5f);

            [Tooltip("Preferred source list (usually Category=Sound).")]
            public AudioCue[] cues = Array.Empty<AudioCue>();

            [Tooltip("Optional clip fallback list.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();

            [Range(0f, 1f)]
            public float clipVolume = 1f;

            public Vector2 clipPitchRange = new Vector2(1f, 1f);

            public bool HasAnyPlayable()
            {
                if (cues != null)
                {
                    for (int i = 0; i < cues.Length; i++)
                    {
                        if (cues[i] != null && cues[i].Clip != null)
                            return true;
                    }
                }

                if (clips != null)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (clips[i] != null)
                            return true;
                    }
                }

                return false;
            }
        }

        public bool HasMusicPlaylist()
        {
            if (musicPlaylist != null)
            {
                for (int i = 0; i < musicPlaylist.Length; i++)
                {
                    if (musicPlaylist[i] != null && musicPlaylist[i].Clip != null)
                        return true;
                }
            }

            if (musicPlaylistClips != null)
            {
                for (int i = 0; i < musicPlaylistClips.Length; i++)
                {
                    if (musicPlaylistClips[i] != null)
                        return true;
                }
            }

            return false;
        }

        private void OnEnable()
        {
            EnsureId();
        }

        private void OnValidate()
        {
            EnsureId();
        }

        private void EnsureId()
        {
            if (!string.IsNullOrEmpty(id))
                return;

            id = SanitizeId(name);
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "location";

            value = value.Trim().ToLowerInvariant();
            value = value.Replace(' ', '_');
            return value;
        }
    }
}
