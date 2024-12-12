using UnityEngine;

[System.Serializable]
public class ShelfData
{
    public int shelftype;
    public Position position;
    public Rotation rotation;
    public Dimensions dimensions;
    public ShelfSection[] shelfSections;
}

[System.Serializable]
public class Position
{
    public float x, y, z;
}

[System.Serializable]
public class Rotation
{
    public float y;
}

[System.Serializable]
public class Dimensions
{
    public float Width, Height, Length;
}

[System.Serializable]
public class ShelfSection
{
    public int id;
    public Position position;
    public string[] items;
}
