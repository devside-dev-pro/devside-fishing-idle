using System;
using System.IO;
using Devside.FishingIdle.Core;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Sauvegarde JSON dans persistentDataPath. Écriture atomique (fichier temporaire puis
    /// remplacement) pour ne jamais corrompre la sauvegarde si l'appli est tuée en plein vol.
    /// </summary>
    public static class SaveSystem
    {
        static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static void Save(GameState state)
        {
            state.lastSeenUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(state));
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tmp, SavePath);
        }

        public static GameState LoadOrNew()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var loaded = JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
                    if (loaded != null) return loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Sauvegarde illisible, nouvelle partie : {e.Message}");
            }
            return new GameState { lastSeenUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        }

        public static void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
