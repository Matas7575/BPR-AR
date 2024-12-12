using UnityEngine;
using Npgsql;
using System;

public class DatabaseConnection : MonoBehaviour
{
    private static string connectionString;

    void Awake()
    {
        connectionString = DatabaseConfig.ConnectionString();
        TestConnection();
    }

    private void TestConnection()
    {
        try
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                Debug.Log("Database connected successfully.");
            }
        }
        catch (Exception ex)
        {
            Debug.Log("connection string: " + connectionString);
            Debug.LogError("Database connection test failed: " + ex.Message);
        }
    }

    public static Texture2D GetItemTexture(string ean)
    {
        Texture2D texture = null;

        try
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT \"Picture_Blob\" FROM \"ProductImages\" WHERE \"Main_EAN\" = @EAN";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("EAN", ean);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            byte[] imageBlob = reader["Picture_Blob"] as byte[];
                            if (imageBlob != null)
                            {
                                texture = new Texture2D(2, 2); // Default size; LoadImage will resize
                                if (!texture.LoadImage(imageBlob))
                                {
                                    Debug.LogError("Failed to load image from blob.");
                                    texture = null;
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Image blob is null for EAN: " + ean);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("No image found for EAN: " + ean);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch image for EAN {ean}: {ex.Message}");
        }

        return texture;
    }
}

