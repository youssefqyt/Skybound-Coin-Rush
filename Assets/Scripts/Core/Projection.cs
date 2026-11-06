using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Screen-space projection for the 2D pseudo-perspective skyway.
    /// z = 0 is the player's plane, larger z is further down the track.
    /// </summary>
    public static class Projection
    {
        public const float HorizonY = 1.10f;
        public const float NearY = -3.95f;
        public const float Focus = 32f;
        public const float LaneWidth = 2.55f;
        public const float NearHalfWidth = 4.75f;
        public const float ViewZ = 150f;

        public static float Scale(float z)
        {
            return Focus / (Focus + Mathf.Max(z, -Focus * 0.85f));
        }

        public static float Y(float z)
        {
            return HorizonY - (HorizonY - NearY) * Scale(z);
        }

        public static float X(int lane, float z)
        {
            return lane * LaneWidth * Scale(z);
        }

        public static Vector3 Position(int lane, float z, float heightOffset = 0f)
        {
            float k = Scale(z);
            return new Vector3(lane * LaneWidth * k, HorizonY - (HorizonY - NearY) * k + heightOffset * k, 0f);
        }

        public static Vector3 PositionAtX(float laneX, float z, float heightOffset = 0f)
        {
            float k = Scale(z);
            return new Vector3(laneX * k, HorizonY - (HorizonY - NearY) * k + heightOffset * k, 0f);
        }

        public static float Depth01(float z)
        {
            return Mathf.Clamp01(z / ViewZ);
        }

        /// <summary>Sorting order for an object at z. Near objects draw in front.</summary>
        public static int SortingOrder(float z, int layerBase = 400)
        {
            return layerBase - Mathf.Clamp(Mathf.RoundToInt(z * 2f), 0, 300);
        }

        public static Vector3 RoadPoint(float side, float z)
        {
            float k = Scale(z);
            return new Vector3(side * NearHalfWidth * k, HorizonY - (HorizonY - NearY) * k, 0f);
        }

        public static Mesh BuildTrapezoid(float nearY, float nearHalfWidth, float farY, float farHalfWidth)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Skybound Trapezoid";
            mesh.vertices = new[]
            {
                new Vector3(-nearHalfWidth, nearY, 0f),
                new Vector3(nearHalfWidth, nearY, 0f),
                new Vector3(farHalfWidth, farY, 0f),
                new Vector3(-farHalfWidth, farY, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
