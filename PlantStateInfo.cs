using UnityEngine;

namespace NPCsSystem;

public class PlantStateInfo
{
    public readonly Vector3 position;
    public Plant plantObject;

    public PlantStateInfo(Vector3 position)
    {
        this.position = position;
    }

    public bool HavePlant => plantObject;


    public static implicit operator bool(PlantStateInfo exists)
    {
        return exists != null;
    }
}