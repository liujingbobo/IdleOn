using UnityEngine;
using IdleOn.Save;

namespace IdleOn.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private SaveManager  saveManager;
        [SerializeField] private GameDatabase gameDatabase;

        void Awake()
        {
            GameDatabase.Register(gameDatabase);
            // Save/character creation is now driven by the StartupMenu UI flow.
            // Gameplay systems stay uninitialised until a character is selected
            // (they wait on SaveManager.OnSaveLoaded).
        }
    }
}
