using UnityEngine;
using TMPro;
using IdleOn.Core;
using IdleOn.World;

namespace IdleOn.UI
{
    public class ObjectiveHelperUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text mainText;
        [SerializeField] private TMP_Text rewardText;

        void Awake()
        {
            GameEvents.OnMapChanged            += OnMapChanged;
            GameEvents.OnMapObjectiveProgress  += OnObjectiveProgress;
            GameEvents.OnMapObjectiveCompleted += OnObjectiveCompleted;
        }

        void OnDestroy()
        {
            GameEvents.OnMapChanged            -= OnMapChanged;
            GameEvents.OnMapObjectiveProgress  -= OnObjectiveProgress;
            GameEvents.OnMapObjectiveCompleted -= OnObjectiveCompleted;
        }

        void Start() => RefreshFromCurrentMap();

        private void RefreshFromCurrentMap()
        {
            if (MapSystem.Instance == null) return;

            var mapDef = MapSystem.Instance.CurrentMapDef;
            if (mapDef == null)
            {
                if (mainText  != null) mainText.text  = "";
                if (rewardText != null) rewardText.text = "";
                return;
            }

            var  prog       = MapSystem.Instance.GetProgress(MapSystem.Instance.CurrentMapId);
            bool isComplete = prog != null && prog.IsComplete;

            if (isComplete)
            {
                ShowComplete(mapDef);
            }
            else
            {
                int kills = prog?.KillCount ?? 0;
                if (mainText != null)
                    mainText.text = $"{mapDef.DisplayName}  ·  {mapDef.ObjectiveLabel}:  {kills} / {mapDef.KillObjective}";

                if (rewardText != null)
                {
                    string unlockLabel = string.IsNullOrEmpty(mapDef.UnlocksMapId)
                        ? "Demo Complete"
                        : GetMapName(mapDef.UnlocksMapId) + " Unlock";
                    rewardText.text = $"Reward: {mapDef.SilverReward} Silver + {unlockLabel}";
                }
            }
        }

        private void OnMapChanged(string mapId)                      => RefreshFromCurrentMap();
        private void OnObjectiveProgress(int current, int required)  => RefreshFromCurrentMap();

        private void OnObjectiveCompleted(string mapId)
        {
            var mapDef = GameDatabase.Instance?.Maps?.GetMap(mapId);
            ShowComplete(mapDef);
        }

        private void ShowComplete(MapDefinition mapDef)
        {
            if (mapDef == null) return;

            bool isEndOfContent = string.IsNullOrEmpty(mapDef.UnlocksMapId);

            if (mainText != null)
            {
                mainText.text = isEndOfContent
                    ? $"✓ {mapDef.DisplayName}  —  All areas cleared!"
                    : $"✓ {mapDef.DisplayName}  —  Complete!";
            }

            if (rewardText != null)
            {
                rewardText.text = isEndOfContent
                    ? "Demo Complete  —  Thanks for playing!"
                    : $"{GetMapName(mapDef.UnlocksMapId)} Unlocked!   +{mapDef.SilverReward} Silver";
            }
        }

        private string GetMapName(string mapId)
        {
            var def = GameDatabase.Instance?.Maps?.GetMap(mapId);
            return def != null ? def.DisplayName : mapId;
        }
    }
}
