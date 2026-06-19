using UnityEngine;
using IdleOn.UI;

namespace IdleOn.World
{
    // Walk-up crafting station. Opens the Crafting window through MainHUD's open-only entry so the
    // central window switching + HUD button sprites stay in sync (deliberately NOT a direct
    // CraftingWindow.Open(), which would bypass MainHUD and desync the button state).
    public class CraftingStationInteractable : WorldInteractable
    {
        [SerializeField] private MainHUD mainHUD;

        public override void Interact(GameObject player)
        {
            if (mainHUD == null)
            {
                Debug.LogWarning("[CraftingStation] MainHUD reference not set — cannot open Crafting.", this);
                return;
            }
            mainHUD.OpenCraftingWindow();
        }
    }
}
