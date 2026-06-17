using UnityEngine;
using IdleOn.Characters;
using IdleOn.Combat;
using IdleOn.Save;

namespace IdleOn.UI
{
    public class SkillHotbarUI : MonoBehaviour
    {
        [SerializeField] private SkillSlotUI[] slots;
        [SerializeField] private DragHandler   dragHandler;
        [SerializeField] private PlayerCombatController combatController;

        void Start()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsLoaded)
                Initialize();
            else
                SaveManager.OnSaveLoaded += Initialize;
        }

        void OnDestroy() => SaveManager.OnSaveLoaded -= Initialize;

        private void Initialize()
        {
            if (combatController == null && PlayerStats.Instance != null)
                combatController = PlayerStats.Instance.GetComponent<PlayerCombatController>();

            var save = SaveManager.Instance?.CurrentSave;
            if (save != null)
            {
                while (save.HotbarSkillIds.Count < 3)
                    save.HotbarSkillIds.Add(string.Empty);
            }

            for (int i = 0; i < slots.Length; i++)
                slots[i]?.Initialize(i, dragHandler, combatController);
        }
    }
}
