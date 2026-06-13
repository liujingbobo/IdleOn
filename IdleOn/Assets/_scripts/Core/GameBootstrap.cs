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
            saveManager.CreateNewSave();
        }
    }
}
