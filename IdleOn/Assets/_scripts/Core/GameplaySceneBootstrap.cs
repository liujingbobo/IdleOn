using UnityEngine;
using IdleOn.Save;

namespace IdleOn.Core
{
    // If a character was already selected in BootScene, SaveManager persisted via
    // DontDestroyOnLoad, but TestCombat's own QuestSystem/FeatureUnlockSystem/EnemyKillTracker
    // singletons did not exist yet when SelectCharacter pushed ImportState the first time.
    // Re-running SelectCharacter here re-pushes that same import against the now-live instances.
    // No-op if no character is selected (e.g. TestCombat opened directly in the editor) — the
    // existing per-system Start()-time fresh-state defaults take over instead.
    //
    // Must run before QuestSystem/FeatureUnlockSystem/EnemyKillTracker's own Start() (default
    // order 0): those call PersistState()/write straight into CurrentSave on their first fresh
    // StartQuest(), which would clobber the saved data this script is restoring if it ran later.
    // -60 keeps it after SaveManager (-50) and GameBootstrap (-100), but before default-order scripts.
    [DefaultExecutionOrder(-60)]
    public class GameplaySceneBootstrap : MonoBehaviour
    {
        void Start()
        {
            var sm = SaveManager.Instance;
            if (sm != null && sm.IsLoaded && sm.CurrentSave != null)
                sm.SelectCharacter(sm.CurrentSave.PlayerId);
        }
    }
}
