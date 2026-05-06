using UnityEngine;

public sealed class DesktopMockVRPhysicsProfile : MonoBehaviour
{
    [SerializeField] private string bodyLayerName = "desktop.body";
    [SerializeField] private string handLayerName = "desktop.hand";
    [SerializeField] private string interactableLayerName = "desktop.interactable";
    [SerializeField] private string worldLayerName = "desktop.world";

    private void Awake()
    {
        ApplyLayerCollisionMatrix();
    }

    private void ApplyLayerCollisionMatrix()
    {
        int bodyLayer = LayerMask.NameToLayer(bodyLayerName);
        int handLayer = LayerMask.NameToLayer(handLayerName);
        int interactableLayer = LayerMask.NameToLayer(interactableLayerName);
        int worldLayer = LayerMask.NameToLayer(worldLayerName);

        if (bodyLayer < 0 || handLayer < 0 || interactableLayer < 0 || worldLayer < 0)
        {
            return;
        }

        Physics.IgnoreLayerCollision(bodyLayer, handLayer, true);
        Physics.IgnoreLayerCollision(handLayer, handLayer, true);
        Physics.IgnoreLayerCollision(bodyLayer, worldLayer, false);
        Physics.IgnoreLayerCollision(bodyLayer, interactableLayer, false);
        Physics.IgnoreLayerCollision(handLayer, worldLayer, false);
        Physics.IgnoreLayerCollision(handLayer, interactableLayer, false);
        Physics.IgnoreLayerCollision(interactableLayer, worldLayer, false);
    }
}
