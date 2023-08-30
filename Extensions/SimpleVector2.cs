using System;
using UnityEngine;

namespace Extensions;

public struct SimpleVector2 : IEquatable<SimpleVector2>
{
    public float x;
    public float y;

    public SimpleVector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public SimpleVector2()
    {
    }

    public bool Equals(SimpleVector2 other)
    {
        return x == other.x && y == other.y;
    }

    public override string ToString()
    {
        return $"X: {x}, Y: {y}";
    }

    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}

public static class SimpleVector2Ext
{
    public static SimpleVector2 ToSimpleVector2(this Vector2 vector2)
    {
        return new SimpleVector2(vector2.x, vector2.y);
    }
}