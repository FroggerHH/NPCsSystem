using UnityEngine;

namespace Extensions;

public static class VectorExtension
{
    public static Vector2 ToV2(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }

    public static Vector3 ToV3(this Vector2 vector2)
    {
        return new Vector3(vector2.x, 0, vector2.y);
    }

    public static Vector3 RoundCords(this Vector3 vector3)
    {
        return new Vector3((int)vector3.x, (int)vector3.y, (int)vector3.z);
    }

    public static Vector2 RoundCords(this Vector2 vector3)
    {
        return new Vector2((int)vector3.x, (int)vector3.y);
    }

    public static Vector3 WithY(this Vector3 vector3, float y)
    {
        return new Vector3(vector3.x, y, vector3.z);
    }

    public static Vector2 WithY(this Vector2 vector3, float y)
    {
        return new Vector2(vector3.x, y);
    }


    public static float DistanceXZ(this Vector3 pos, Vector3 otherPos)
    {
        return Utils.DistanceXZ(pos, otherPos);
    }

    public static float DistanceXZ(this Vector3 pos, Transform otherPos)
    {
        return Utils.DistanceXZ(pos, otherPos.position);
    }

    public static float DistanceXZ(this Vector3 pos, Component otherPos)
    {
        return Utils.DistanceXZ(pos, otherPos.transform.position);
    }

    public static float DistanceXZ(this Vector3 pos, GameObject otherPos)
    {
        return Utils.DistanceXZ(pos, otherPos.transform.position);
    }
}