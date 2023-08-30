using UnityEngine;

namespace Extensions;

public static class TransformExtension
{
    public static Transform FindChildByName(this Transform transform, string name)
    {
        return Utils.FindChild(transform, name);
    }

    public static float DistanceXZ(this Transform transform, Transform other)
    {
        return Utils.DistanceXZ(transform.position, other.position);
    }

    public static float DistanceXZ(this Transform transform, Component otherGameObject)
    {
        return Utils.DistanceXZ(transform.position, otherGameObject.transform.position);
    }

    public static float DistanceXZ(this Transform transform, GameObject otherGameObject)
    {
        return Utils.DistanceXZ(transform.position, otherGameObject.transform.position);
    }

    public static float DistanceXZ(this Transform transform, Vector3 otherPos)
    {
        return Utils.DistanceXZ(transform.position, otherPos);
    }
}