using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Npgsql; // Ensure you have the PostgreSQL .NET driver

// Ensure ShelfData, ShelfSection, DatabaseConfig, and DatabaseConnection are defined elsewhere.
// ShelfData should have at least:
// public Vector3 position;
// public Vector3 rotation; // e.g., {x,y,z}
// public ShelfSection[] shelfSections;
// ShelfSection should have at least:
// public string[] items;

public class ShelfSectionAnalyzer : MonoBehaviour
{
    [Header("OBJ and JSON Settings")]
    public string relativeObjFilePath = "shelf.obj"; // The file should be at Assets/shelf.obj
    public string jsonFilePath = "Assets/Data/shelfData.json"; // Path to your shelfData.json

    [Header("Materials")]
    public Material sectionMaterial;  // Material for highlighting shelf sections
    public Material shelfMaterial;    // Material for the entire shelf

    [Header("Item Prefab")]
    public GameObject itemPrefab;

    private List<Vector3> vertices;
    private List<int[]> faces;
    private ShelfData shelfData; // Loaded from JSON

    void Start()
    {
        // Load JSON data
        string jsonInput = LoadJsonFile(jsonFilePath);
        if (string.IsNullOrEmpty(jsonInput))
        {
            Debug.LogError("Failed to load JSON file. Ensure it exists and is properly formatted.");
            return;
        }
        shelfData = JsonUtility.FromJson<ShelfData>(jsonInput);
        if (shelfData == null || shelfData.shelfSections == null)
        {
            Debug.LogError("Invalid JSON data. Ensure shelfData.json has a shelfSections array.");
            return;
        }

        // Build full path to OBJ file
        string fullPath = Path.Combine(Application.dataPath, relativeObjFilePath);
        AnalyzeShelfSections(fullPath);
    }

    string LoadJsonFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading JSON file: {ex.Message}");
            return null;
        }
    }

    void AnalyzeShelfSections(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        vertices = new List<Vector3>();
        faces = new List<int[]>();

        // Parse OBJ file
        string[] lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) 
                continue;

            var split = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 0)
                continue;

            if (split[0] == "v" && split.Length >= 4)
            {
                if (float.TryParse(split[1], out float vx) &&
                    float.TryParse(split[2], out float vy) &&
                    float.TryParse(split[3], out float vz))
                {
                    vertices.Add(new Vector3(vx, vy, vz));
                }
            }
            else if (split[0] == "f" && split.Length >= 4)
            {
                int[] faceIndices = new int[split.Length - 1];
                bool faceValid = true;
                for (int i = 1; i < split.Length; i++)
                {
                    var part = split[i].Split('/');
                    if (!int.TryParse(part[0], out int vertIndex))
                    {
                        faceValid = false;
                        break;
                    }
                    faceIndices[i - 1] = vertIndex - 1; // OBJ indices start at 1
                }

                // Expect triangles
                if (faceValid && faceIndices.Length == 3)
                {
                    faces.Add(faceIndices);
                }
            }
            // Ignore other lines (e.g., usemtl, mtllib, etc.)
        }

        // Identify horizontal sections (top + bottom)
        List<List<int>> sections = GroupHorizontalFaces(faces, vertices);

        // Remove top/bottom/empty sections
        sections = RemoveTopBottomAndEmptySections(sections, vertices, faces);

        Debug.Log("Sections after removing top, bottom, and empty ones: " + sections.Count);

        // Filter only upward-facing top surfaces
        sections = sections
            .Select(s => s.Where(faceIndex => IsUpwardFacingFace(faces[faceIndex], vertices)).ToList())
            .Where(s => s.Count > 0)
            .ToList();

        Debug.Log("Sections after filtering only top faces: " + sections.Count);

        // Spawn the full shelf
        GameObject shelfObject = SpawnShelf(vertices, faces);

        // Spawn and add sections as children of the shelf object, and place items
        SpawnHighlightAndPlaceItems(sections, shelfObject);

        // Position and rotate the entire shelf from the JSON
        shelfObject.transform.position = new Vector3(shelfData.position.x, shelfData.position.y, shelfData.position.z);
        shelfObject.transform.rotation = Quaternion.Euler(0, shelfData.rotation.y, 0);
    }

    List<List<int>> GroupHorizontalFaces(List<int[]> faces, List<Vector3> vertices)
    {
        // Consider any face with |normal.y| > 0.9f as horizontal
        List<List<int>> sections = new List<List<int>>();
        HashSet<int> processedFaces = new HashSet<int>();

        for (int i = 0; i < faces.Count; i++)
        {
            if (processedFaces.Contains(i) || !IsHorizontalFace(faces[i], vertices)) 
                continue;

            List<int> section = new List<int>();
            Queue<int> toProcess = new Queue<int>();
            toProcess.Enqueue(i);

            while (toProcess.Count > 0)
            {
                int currentFaceIndex = toProcess.Dequeue();
                if (processedFaces.Contains(currentFaceIndex)) continue;

                section.Add(currentFaceIndex);
                processedFaces.Add(currentFaceIndex);

                foreach (int neighborIndex in FindNeighborFaces(currentFaceIndex, faces))
                {
                    if (!processedFaces.Contains(neighborIndex) && IsHorizontalFace(faces[neighborIndex], vertices))
                    {
                        toProcess.Enqueue(neighborIndex);
                    }
                }
            }

            sections.Add(section);
        }

        return sections;
    }

    List<List<int>> RemoveTopBottomAndEmptySections(List<List<int>> sections, List<Vector3> vertices, List<int[]> faces)
    {
        // Remove empty sections
        sections = sections.Where(s => s.Count > 0).ToList();

        // Calculate the center height of each section
        Dictionary<List<int>, float> sectionHeights = sections.ToDictionary(
            section => section,
            section => section.Average(faceIndex => CalculateFaceCenterHeight(faces[faceIndex], vertices))
        );

        // Order by height ascending
        var orderedSections = sectionHeights.OrderBy(pair => pair.Value).ToList();

        // Remove top 3 (highest)
        for (int i = 0; i < 3 && orderedSections.Count > 0; i++)
        {
            var topSection = orderedSections[orderedSections.Count - 1].Key;
            sections.Remove(topSection);
            orderedSections.RemoveAt(orderedSections.Count - 1);
        }

        // Remove bottom 3 (lowest)
        for (int i = 0; i < 3 && orderedSections.Count > 0; i++)
        {
            var bottomSection = orderedSections[0].Key;
            sections.Remove(bottomSection);
            orderedSections.RemoveAt(0);
        }

        return sections;
    }

    float CalculateFaceCenterHeight(int[] face, List<Vector3> vertices)
    {
        Vector3 v0 = vertices[face[0]];
        Vector3 v1 = vertices[face[1]];
        Vector3 v2 = vertices[face[2]];
        return (v0.y + v1.y + v2.y) / 3f;
    }

    bool IsHorizontalFace(int[] face, List<Vector3> vertices)
    {
        Vector3 normal = CalculateFaceNormal(face, vertices);
        return Mathf.Abs(normal.y) > 0.9f; 
    }

    bool IsUpwardFacingFace(int[] face, List<Vector3> vertices)
    {
        Vector3 normal = CalculateFaceNormal(face, vertices);
        return normal.y > 0.9f;
    }

    Vector3 CalculateFaceNormal(int[] face, List<Vector3> vertices)
    {
        Vector3 v0 = vertices[face[0]];
        Vector3 v1 = vertices[face[1]];
        Vector3 v2 = vertices[face[2]];
        return Vector3.Cross(v1 - v0, v2 - v0).normalized;
    }

    List<int> FindNeighborFaces(int faceIndex, List<int[]> faces)
    {
        List<int> neighbors = new List<int>();
        int[] face = faces[faceIndex];

        for (int candidateIndex = 0; candidateIndex < faces.Count; candidateIndex++)
        {
            if (candidateIndex == faceIndex) continue;

            int sharedVertices = faces[candidateIndex].Intersect(face).Count();
            if (sharedVertices >= 2)
            {
                neighbors.Add(candidateIndex);
            }
        }

        return neighbors;
    }

    GameObject SpawnShelf(List<Vector3> vertices, List<int[]> faces)
    {
        GameObject shelfObject = new GameObject("ShelfRoot");

        Mesh mesh = new Mesh();
        List<Vector3> meshVertices = new List<Vector3>();
        List<int> meshTriangles = new List<int>();

        Dictionary<int, int> vertexMap = new Dictionary<int, int>();

        foreach (int[] face in faces)
        {
            for (int j = 0; j < face.Length; j++)
            {
                if (!vertexMap.ContainsKey(face[j]))
                {
                    vertexMap[face[j]] = meshVertices.Count;
                    meshVertices.Add(vertices[face[j]]);
                }
                meshTriangles.Add(vertexMap[face[j]]);
            }
        }

        mesh.vertices = meshVertices.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.RecalculateNormals();

        MeshFilter meshFilter = shelfObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = shelfObject.AddComponent<MeshRenderer>();
        meshRenderer.material = shelfMaterial ?? new Material(Shader.Find("Standard")) { color = Color.gray };

        return shelfObject;
    }

    void SpawnHighlightAndPlaceItems(List<List<int>> sections, GameObject shelfObject)
    {
        // No separate parent, sections become children of the shelfObject
        int count = Mathf.Min(sections.Count, shelfData.shelfSections.Length);

        for (int i = 0; i < count; i++)
        {
            var section = sections[i];
            GameObject sectionObject = new GameObject("Shelf Section");
            sectionObject.transform.SetParent(shelfObject.transform, false);

            Mesh mesh = new Mesh();
            List<Vector3> sectionVertices = new List<Vector3>();
            List<int> sectionTriangles = new List<int>();

            Dictionary<int, int> vertexMap = new Dictionary<int, int>();

            foreach (int faceIndex in section)
            {
                int[] face = faces[faceIndex];
                for (int j = 0; j < face.Length; j++)
                {
                    if (!vertexMap.ContainsKey(face[j]))
                    {
                        vertexMap[face[j]] = sectionVertices.Count;
                        sectionVertices.Add(vertices[face[j]]);
                    }
                    sectionTriangles.Add(vertexMap[face[j]]);
                }
            }

            mesh.vertices = sectionVertices.ToArray();
            mesh.triangles = sectionTriangles.ToArray();
            mesh.RecalculateNormals();

            MeshFilter meshFilter = sectionObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshRenderer meshRenderer = sectionObject.AddComponent<MeshRenderer>();
            meshRenderer.material = sectionMaterial ?? new Material(Shader.Find("Standard")) { color = Color.green };

            // Place items from shelfData.shelfSections[i].items onto this section
            PlaceItemsInSection(sectionObject, shelfData.shelfSections[i]);
        }
    }

    void PlaceItemsInSection(GameObject sectionObj, ShelfSection section)
    {
        float offsetX = -0.5f; // Starting X offset for items
        float spacing = 0.01f; // Spacing between items

        Bounds sectionBounds = CalculatePrefabBounds(sectionObj);
        float startX = sectionBounds.min.x;
        offsetX = startX;

        foreach (string itemId in section.items)
        {
            Vector3 itemDimensions = GetItemDimensionsFromDatabase(itemId);
            itemDimensions /= 100f; // Convert cm to meters

            Texture2D itemTexture = DatabaseConnection.GetItemTexture(itemId);

            if (itemDimensions != Vector3.zero && itemPrefab != null)
            {
                GameObject itemObj = Instantiate(itemPrefab, sectionObj.transform);

                // Adjust item size
                itemObj.transform.localScale = itemDimensions / 2f;

                if (itemTexture != null)
                {
                    Renderer renderer = itemObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.mainTexture = itemTexture;
                        Debug.Log($"Texture applied to item: {itemId}");
                    }
                }

                BoxCollider boxCollider = itemObj.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    boxCollider.size = Vector3.one;
                    boxCollider.center = Vector3.zero;
                }

                float halfWidth = itemDimensions.x / 2f;

                // Position item on top of the section
                itemObj.transform.localPosition = new Vector3(
                    offsetX + halfWidth, 
                    sectionBounds.min.y + (itemDimensions.y / 4f), 
                    (sectionBounds.min.z + sectionBounds.max.z) / 2f
                );

                offsetX += halfWidth + spacing + halfWidth;
            }
        }
    }

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

    Vector3 GetItemDimensionsFromDatabase(string ean)
    {
        string connectionString = DatabaseConfig.ConnectionString();
        Vector3 dimensions = Vector3.zero;

        try
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT ""Product_Width"", ""Product_Height"", ""Product_Depth"" 
                                 FROM products 
                                 WHERE ""Main_EAN"" = @EAN";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("EAN", ean);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            double width = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                            double height = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                            double depth = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);

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
        catch (Exception ex)
        {
            Debug.LogError($"Database query failed for EAN {ean}: {ex.Message}");
        }

        return dimensions;
    }
}
