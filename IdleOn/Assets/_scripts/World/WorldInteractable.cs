using UnityEngine;

namespace IdleOn.World
{
    // Base for same-lane interactables. InteractionX defaults to this object's X (feet-root lane model).
    // Use a trigger/click collider on the same GameObject so it is clickable but never physically
    // blocks the player, enemies, Fireball, or drops.
    public abstract class WorldInteractable : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] protected float interactionRange = 0.4f;

        public virtual float InteractionX     => transform.position.x;
        public         float InteractionRange => interactionRange;

        public virtual bool CanInteract(GameObject player) => isActiveAndEnabled;

        public abstract void Interact(GameObject player);
    }
}
