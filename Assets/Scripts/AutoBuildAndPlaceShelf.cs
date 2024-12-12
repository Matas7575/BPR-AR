using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Npgsql;
public class AutoBuildAndPlaceShelf : MonoBehaviour
{
    public GameObject shelfPrefab;
    public GameObject sectionPrefab;
    public GameObject itemPrefab;

    private string jsonFilePath = "Assets/Data/shelfData.json";

    void Start()
    {
        // Load JSON file
        string jsonInput = LoadJsonFile(jsonFilePath);
        if (string.IsNullOrEmpty(jsonInput))
        {
            Debug.LogError("Failed to load JSON file. Ensure the file exists and is properly formatted.");
            return;
        }
        
        // Parse JSON into ShelfData object
        ShelfData shelfData = JsonUtility.FromJson<ShelfData>(jsonInput);

        // Create the shelf
        GameObject shelf = Instantiate(
            shelfPrefab,
            new Vector3(shelfData.position.x, shelfData.position.y, shelfData.position.z),
            Quaternion.Euler(0, shelfData.rotation.y, 0)
        );

        // Scale the shelf to match the JSON dimensions
        ScaleShelfToDimensions(shelf, new Vector3(shelfData.dimensions.Width, shelfData.dimensions.Height, shelfData.dimensions.Length));

        // Add shelf sections
        foreach (ShelfSection section in shelfData.shelfSections)
        {
            // Create the section
            GameObject sectionObj = Instantiate(sectionPrefab, shelf.transform);

            // Position the section relative to the shelf dimensions
            PositionShelfSection(sectionObj, section, shelfData);
            
            // Place items in the section
            PlaceItemsInSection(sectionObj, section);
        }
    }
    
    // Load JSON file from the specified path
    string LoadJsonFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reading JSON file: {ex.Message}");
            return null;
        }
    }

    // Scale the shelf prefab to match the JSON dimensions
    void ScaleShelfToDimensions(GameObject shelf, Vector3 targetDimensions)
    {
        Bounds prefabBounds = CalculatePrefabBounds(shelf);
        Vector3 currentSize = prefabBounds.size;

        Vector3 scaleFactor = new Vector3(
            targetDimensions.x / currentSize.x,
            targetDimensions.y / currentSize.y,
            targetDimensions.z / currentSize.z
        );

        shelf.transform.localScale = Vector3.Scale(shelf.transform.localScale, scaleFactor);
    }

    // Position the section relative to the shelf's dimensions
    void PositionShelfSection(GameObject section, ShelfSection shelfSection, ShelfData shelfData)
    {
        // Shelf dimensions
        float shelfHeight = shelfData.dimensions.Height / 4;

        // Section position relative to the shelf's center
        float sectionY = shelfSection.position.y;         // From bottom to top

        // Apply position
        section.transform.localPosition = new Vector3(0f, sectionY - (shelfHeight / 2), 0f);
    }
    
    // Place items in the section
 void PlaceItemsInSection(GameObject sectionObj, ShelfSection section)
{
    float offsetX = -0.5f; // Start placing items from the left edge
    float spacing = 0.01f; // Spacing between items

    foreach (string itemId in section.items)
    {
        // Fetch item dimensions from the database
        Vector3 itemDimensions = GetItemDimensionsFromDatabase(itemId);
        itemDimensions /= 100f; // Convert cm to meters

        // Fetch texture for the item
        Texture2D itemTexture = DatabaseConnection.GetItemTexture(itemId);

        if (itemDimensions != Vector3.zero)
        {
            // Instantiate the item
            GameObject itemObj = Instantiate(itemPrefab, sectionObj.transform);

            // Adjust item size
            itemObj.transform.localScale = itemDimensions / 2;

            // Apply the texture if available
            if (itemTexture != null)
            {
                Renderer renderer = itemObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.mainTexture = itemTexture;
                    Debug.Log($"Texture applied to item: {itemId}");
                }
                else
                {
                    Debug.LogWarning($"No Renderer found on {itemObj.name}. Texture application skipped.");
                }
            }

            // Adjust collider size
            BoxCollider boxCollider = itemObj.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size = Vector3.one;
                boxCollider.center = Vector3.zero;
            }
            else
            {
                Debug.LogWarning($"No BoxCollider found on {itemObj.name}. Collider size adjustment skipped.");
            }

            // Set item position within the section
            itemObj.transform.localPosition = new Vector3(
                offsetX + (itemDimensions.x / 2), // Center the item
                itemDimensions.y / 4,             // Align the item's bottom to the section
                0f                                // Align to the center depth-wise
            );

            // Update offsetX for the next item
            offsetX += itemDimensions.x / 2 + spacing;

            Debug.Log($"Item: {itemId}, Dimensions: {itemDimensions}, LocalScale: {itemObj.transform.localScale}, Collider Size: {boxCollider.size}");
        }
    }
}

    
    // Fetch item dimensions from the database
    Vector3 GetItemDimensionsFromDatabase(string ean)
    {
        string connectionString = DatabaseConfig.ConnectionString(); // Get the connection string
        Vector3 dimensions = Vector3.zero;

        try
        {
            using (var connection = new Npgsql.NpgsqlConnection(connectionString))
            {
                connection.Open();

                // Correctly reference case-sensitive columns with double quotes
                string query = @"SELECT ""Product_Width"", ""Product_Height"", ""Product_Depth"" 
                             FROM products 
                             WHERE ""Main_EAN"" = @EAN";

                using (var command = new Npgsql.NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("EAN", ean); // Add parameter to prevent SQL injection

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            double width = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                            double height = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                            double depth = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);

                            // Convert double to float (Unity uses float for Vector3)
                            dimensions = new Vector3((float)width, (float)height, (float)depth);
                        }
                        else
                        {
                            Debug.LogWarning($"No dimensions found for EAN: {ean}");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Database query failed for EAN {ean}: {ex.Message}");
        }

        return dimensions;
    }


    // Calculate the bounds of the prefab, including all its child objects
    Bounds CalculatePrefabBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }
}
