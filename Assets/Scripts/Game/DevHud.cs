using Devside.FishingIdle.Core;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// HUD de développement en OnGUI : rend le jeu jouable sans aucune UI câblée en scène.
    /// Outil jetable — sera remplacé par la vraie UI ; ses libellés en dur sont assumés
    /// (exception documentée dans AGENTS.md).
    /// </summary>
    [RequireComponent(typeof(GameBootstrap))]
    public class DevHud : MonoBehaviour
    {
        Vector2 _scroll;
        CatchResult _lastCatch;

        void OnGUI()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.State == null) return;
            var config = boot.Config;
            var state = boot.State;

            GUILayout.BeginArea(new Rect(10, 10, 380, Screen.height - 20), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label($"Argent : {Numbers.Format(state.money)}");
            GUILayout.Label($"Brut : {Numbers.Format(state.rawFish)}  |  Découpé : {Numbers.Format(state.cutFish)}  |  Filets : {Numbers.Format(state.fillet)}");
            GUILayout.Label($"Prestige : {state.prestigePoints} pts (en attente : {Prestige.PendingPoints(config, state)})");
            GUILayout.Label($"Profondeur : {Catching.DepthLevel(config, state)}  |  Poissodex : {state.discoveredSpecies.Count}/{config.species.Count}");
            if (_lastCatch != null && _lastCatch.speciesId != null)
                GUILayout.Label($"Dernière prise : {_lastCatch.speciesId} (+{Numbers.Format(_lastCatch.amount)}){(_lastCatch.newDiscovery ? "  ★ DÉCOUVERTE !" : "")}");
            if (boot.LastOffline != null && boot.LastOffline.simulatedSeconds > 0)
                GUILayout.Label($"Hors-ligne : +{Numbers.Format(boot.LastOffline.moneyGained)} en {(int)(boot.LastOffline.simulatedSeconds / 60)} min");

            GUILayout.Space(8);
            if (GUILayout.Button("Pêcher !", GUILayout.Height(48)))
                _lastCatch = Simulation.CastLine(config, state, Random.value);
            if (!state.autoSellUnlocked && GUILayout.Button("Tout vendre", GUILayout.Height(32)))
                Economy.SellAll(config, state);

            GUILayout.Space(8);
            GUILayout.Label("— Producteurs —");
            foreach (var def in config.producers)
            {
                int owned = state.ProducerCount(def.id);
                double cost = Economy.ProducerCost(def, owned);
                GUI.enabled = state.money >= cost;
                if (GUILayout.Button($"{def.id}  (x{owned})  —  {Numbers.Format(cost)}"))
                    Economy.TryBuyProducer(config, state, def.id);
                GUI.enabled = true;
            }

            GUILayout.Space(8);
            GUILayout.Label("— Améliorations —");
            foreach (var def in config.upgrades)
            {
                int level = state.UpgradeLevel(def.id);
                if (level >= def.maxLevel) continue;
                double cost = Economy.UpgradeCost(def, level);
                GUI.enabled = state.money >= cost;
                if (GUILayout.Button($"{def.id}  (niv. {level})  —  {Numbers.Format(cost)}"))
                    Economy.TryBuyUpgrade(config, state, def.id);
                GUI.enabled = true;
            }

            GUILayout.Space(8);
            if (Prestige.PendingPoints(config, state) > 0 && GUILayout.Button("★ PRESTIGE ★", GUILayout.Height(40)))
                Prestige.Execute(config, state);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
