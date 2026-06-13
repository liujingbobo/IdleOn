using UnityEngine;
using IdleOn.Save;

namespace IdleOn.Core
{
    // Runs before all other MonoBehaviours (execution order -100).
    // Fires OnSaveLoaded synchronously in Awake so all Start() callbacks
    // in other systems can safely read SaveManager.Instance.CurrentSave.
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private SaveManager saveManager;

        void Awake()
        {
            saveManager.CreateNewSave();
        }
    }
}
