using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DynamicColliderAdjuster : MonoBehaviour
{
    void Start()
    {
        AdjustColliderToFitChildren();
    }

    public void AdjustColliderToFitChildren()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) return;

        // Calculate bounds of all child objects
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        // Set the collider size and center to match the bounds
        boxCollider.center = bounds.center - transform.position;
        boxCollider.size = bounds.size;
    }
}