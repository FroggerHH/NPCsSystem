using System;
using UnityEngine;

namespace Extensions;

public struct SimpleVector3 : IEquatable<SimpleVector3>
{
    public float x;
    public float y;
    public float z;

    public SimpleVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SimpleVector3()
    {
    }

    public bool Equals(SimpleVector3 other)
    {
        return x == other.x && y == other.y && z == other.z;
    }

    public override string ToString()
    {
        return $"X: {x}, Y: {y}, Z: {z}";
        //return $"{x}, {y}, {z}";
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

public static class SimpleVector3Ext
{
    public static SimpleVector3 ToSimpleVector3(this Vector3 vector3)
    {
        return new SimpleVector3(vector3.x, vector3.y, vector3.z);
    }
}