using System;
using Devside.FishingIdle.Core;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Point d'entrée du jeu : charge la sauvegarde, applique la progression hors-ligne,
    /// fait tourner la simulation et sauvegarde périodiquement.
    /// À poser sur un GameObject vide de la scène (avec DevHud pour jouer sans UI).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        public BalanceConfig Config { get; private set; }
        public GameState State { get; private set; }

        /// <summary>Résumé de la dernière progression hors-ligne, à afficher au joueur (ou null).</summary>
        public OfflineResult LastOffline { get; private set; }

        const float SaveIntervalSeconds = 10f;
        float _saveTimer;

        void Awake()
        {
            Instance = this;
            Config = BalanceConfig.Default();
            State = SaveSystem.LoadOrNew();

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double elapsed = Math.Max(0, now - State.lastSeenUnixSeconds);
            if (elapsed > 5) LastOffline = OfflineProgress.Apply(Config, State, elapsed);
        }

        void Update()
        {
            Simulation.Tick(Config, State, Time.deltaTime);

            _saveTimer += Time.deltaTime;
            if (_saveTimer >= SaveIntervalSeconds)
            {
                _saveTimer = 0;
                SaveSystem.Save(State);
            }
        }

        void OnApplicationPause(bool paused)
        {
            // Sur mobile, c'est ici que l'appli part en arrière-plan : on fige l'horodatage.
            if (paused) SaveSystem.Save(State);
        }

        void OnApplicationQuit() => SaveSystem.Save(State);
    }
}
