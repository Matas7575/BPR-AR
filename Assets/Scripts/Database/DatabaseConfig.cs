using UnityEngine;
using System.IO;
using DotNetEnv;

public class DatabaseConfig : MonoBehaviour
{
    public static string Server { get; private set; }
    public static string Port { get; private set; }
    public static string Name { get; private set; }
    public static string Username { get; private set; }
    public static string Password { get; private set; }
    public static string Schema { get; private set; }

    void Awake()
    {
        LoadEnvVariables();
    }

    private void LoadEnvVariables()
    {
        string envPath = Path.Combine(Application.dataPath, "env", ".env");
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);

            Server = DotNetEnv.Env.GetString("DB_SERVER");
            Port = DotNetEnv.Env.GetString("DB_PORT");
            Name = DotNetEnv.Env.GetString("DB_NAME");
            Username = DotNetEnv.Env.GetString("DB_USERNAME");
            Password = DotNetEnv.Env.GetString("DB_PASSWORD");
            Schema = DotNetEnv.Env.GetString("DB_SCHEMA");

            Debug.Log("Environment variables loaded successfully.");
        }
        else
        {
            Debug.LogError(".env file not found at: " + envPath);
        }
    }

    public static string ConnectionString()
    {
        return
            $"Host={Server};Port={Port};Database={Name};Username={Username};Password={Password};SearchPath={Schema};" +
            $"Persist Security Info=False;TrustServerCertificate=False;";
    }
}