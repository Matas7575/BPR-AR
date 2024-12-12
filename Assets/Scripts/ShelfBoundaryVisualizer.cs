using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ShelfBoundaryVisualizer : MonoBehaviour
{
    public Color boundaryColor = Color.green; // Color of the wireframe

    private void OnDrawGizmos()
    {
        // Get the BoxCollider of the shelf
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) return;

        // Save the original Gizmos color
        Color originalColor = Gizmos.color;

        // Set the Gizmos color to the boundary color
        Gizmos.color = boundaryColor;

        // Get the center and size of the collider in world space
        Vector3 center = boxCollider.bounds.center;
        Vector3 size = boxCollider.bounds.size;

        // Draw the wireframe box
        Gizmos.DrawWireCube(center, size);

        // Restore the original Gizmos color
        Gizmos.color = originalColor;
    }
}