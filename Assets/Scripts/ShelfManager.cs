using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfManager : MonoBehaviour
{
    public GameObject shelfPrefab;
    public GameObject sectionPrefab;
    public GameObject itemPrefab;

    private string jsonInput = @"
    {
        ""shelftype"": 1,
        ""position"": { ""x"": 0, ""y"": 0, ""z"": 0 },
        ""rotation"": { ""y"": 0 },
        ""dimensions"": { ""Width"": 2, ""Height"": 2, ""Length"": 2 },
        ""shelfSections"": [
            {
                ""id"": 1,
                ""position"": { ""x"": 0.5, ""y"": 1, ""z"": 0.5 },
                ""items"": [""Main-EAN-1"", ""Main-EAN-2""]
            }
        ]
    }";

    private Dictionary<string, Vector3> mockDatabase = new Dictionary<string, Vector3>
    {
        { "Main-EAN-1", new Vector3(0.2f, 0.4f, 0.2f) },
        { "Main-EAN-2", new Vector3(0.3f, 0.6f, 0.3f) }
    };

    void Start()
    {
        // Parse JSON
        ShelfData shelfData = JsonUtility.FromJson<ShelfData>(jsonInput);

        // Create Shelf
        GameObject shelf = Instantiate(shelfPrefab);
        shelf.transform.position = new Vector3(shelfData.position.x, shelfData.position.y, shelfData.position.z);
        shelf.transform.localScale = new Vector3(shelfData.dimensions.Width, shelfData.dimensions.Height, shelfData.dimensions.Length);
        shelf.transform.rotation = Quaternion.Euler(0, shelfData.rotation.y, 0);

        // Add Sections
        foreach (var section in shelfData.shelfSections)
        {
            GameObject sectionObj = Instantiate(sectionPrefab, shelf.transform);
            sectionObj.transform.localPosition = new Vector3(section.position.x, section.position.y, section.position.z);
            sectionObj.transform.localScale = new Vector3(shelfData.dimensions.Width, 1, shelfData.dimensions.Length);

            // Place Items in Section
            float offsetX = 0;
            foreach (string itemId in section.items)
            {
                if (mockDatabase.TryGetValue(itemId, out Vector3 itemDimensions))
                {
                    GameObject itemObj = Instantiate(itemPrefab, sectionObj.transform);
                    itemObj.transform.localScale = itemDimensions;
                    itemObj.transform.localPosition = new Vector3(offsetX, itemDimensions.y / 2, 0);
                    offsetX += itemDimensions.x;
                }
            }
        }
    }
}
