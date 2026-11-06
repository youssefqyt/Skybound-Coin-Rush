using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    public class RoadRenderer : MonoBehaviour
    {
        private Material roadMaterial;
        private Material glowMaterial;
        private MeshRenderer roadMesh;
        private SpriteRenderer horizonGlow;
        private SpriteRenderer wetGlow;
        private readonly List<Dash> dashes = new List<Dash>();
        private readonly List<Post> posts = new List<Post>();
        private LineRenderer leftEdge;
        private LineRenderer rightEdge;
        private LineRenderer leftEdgeGlow;
        private LineRenderer rightEdgeGlow;
        private float dashSpan = 126f;
        private float postSpan = 96f;
        private float wetness;

        public static RoadRenderer Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("Road", parent);
            RoadRenderer road = go.AddComponent<RoadRenderer>();
            road.Build();
            return road;
        }

        private void Build()
        {
            roadMaterial = new Material(Art.SpriteMaterial);
            roadMaterial.mainTexture = Art.RoadTexture();
            roadMaterial.mainTextureScale = new Vector2(4f, 18f);
            roadMaterial.color = new Color(0.32f, 0.36f, 0.44f, 1f);

            Mesh road = Projection.BuildTrapezoid(-6.4f, 11.2f, Projection.HorizonY + 0.02f, 1.35f);
            roadMesh = RuntimeUtil.CreateMesh("RoadSurface", transform, road, roadMaterial, -60);

            glowMaterial = new Material(Art.AdditiveMaterial);

            // Neon edges
            leftEdge = CreateLine("LeftEdge", new Vector3(-10.8f, -6.4f, 0f),
                new Vector3(-1.38f, Projection.HorizonY + 0.02f, 0f), 0.075f, new Color(0f, 0.9f, 1f, 0.9f), 60);
            rightEdge = CreateLine("RightEdge", new Vector3(10.8f, -6.4f, 0f),
                new Vector3(1.38f, Projection.HorizonY + 0.02f, 0f), 0.075f, new Color(0f, 0.9f, 1f, 0.9f), 60);
            leftEdgeGlow = CreateLine("LeftEdgeGlow", new Vector3(-10.8f, -6.4f, 0f),
                new Vector3(-1.38f, Projection.HorizonY + 0.02f, 0f), 0.55f, new Color(0f, 0.8f, 1f, 0.09f), 58);
            rightEdgeGlow = CreateLine("RightEdgeGlow", new Vector3(10.8f, -6.4f, 0f),
                new Vector3(1.38f, Projection.HorizonY + 0.02f, 0f), 0.55f, new Color(0.6f, 0.2f, 1f, 0.09f), 58);

            horizonGlow = RuntimeUtil.CreateGlow("HorizonGlow", transform, -35,
                new Color(0.1f, 0.8f, 1f, 0.22f), new Vector3(0f, Projection.HorizonY - 0.1f, 0f), 11f);
            horizonGlow.transform.localScale = new Vector3(17f, 1.8f, 1f);

            wetGlow = RuntimeUtil.CreateSprite("WetSheen", transform, Art.Glow(), -52,
                new Color(0.2f, 0.8f, 1f, 0f), new Vector3(0f, -2.2f, 0f), new Vector3(9f, 3.8f, 1f), true);

            // Lane divider dashes.
            Sprite dashSprite = Art.RoundedSprite(8, 64, 4);
            float angle = Mathf.Atan2(Projection.LaneWidth * 0.5f, Projection.HorizonY - Projection.NearY) * Mathf.Rad2Deg;
            for (int lane = 0; lane < 2; lane++)
            {
                float boundary = (lane == 0) ? -Projection.LaneWidth * 0.5f : Projection.LaneWidth * 0.5f;
                for (int i = 0; i < 18; i++)
                {
                    Dash dash = new Dash();
                    dash.Z = i * (dashSpan / 18f);
                    dash.Boundary = boundary;
                    dash.Renderer = RuntimeUtil.CreateSprite("Dash" + lane + "_" + i, transform, dashSprite, 50,
                        new Color(0.9f, 0.98f, 1f, 0.55f), Vector3.zero,
                        new Vector3(0.65f, 1.35f, 1f), true);
                    dash.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, boundary > 0 ? angle : -angle);
                    dash.Renderer.gameObject.SetActive(true);
                    dashes.Add(dash);
                }
            }

            // Edge posts with neon lamps.
            Sprite poleSprite = Art.RoundedSprite(10, 96, 5);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 12; i++)
                {
                    Post post = new Post();
                    post.Side = side;
                    post.Z = i * (postSpan / 12f) + 4f;
                    if (i % 2 == 0) post.Z += postSpan * 0.5f;
                    post.Pole = RuntimeUtil.CreateSprite("Post" + side + "_" + i, transform, poleSprite, 45,
                        new Color(0.1f, 0.15f, 0.22f, 0.9f), Vector3.zero, Vector3.one);
                    post.Lamp = RuntimeUtil.CreateGlow("PostLamp" + side + "_" + i, transform, 46,
                        side < 0 ? new Color(0f, 0.9f, 1f, 0.7f) : new Color(0.65f, 0.25f, 1f, 0.7f),
                        Vector3.zero, 1.1f);
                    posts.Add(post);
                }
            }

            ApplyPalette(new Color(0.18f, 0.22f, 0.32f, 1f), new Color(0.05f, 0.1f, 0.2f, 1f),
                new Color(0.5f, 0.2f, 0.8f, 1f));
        }

        private LineRenderer CreateLine(string name, Vector3 a, Vector3 b, float width, Color color, int order)
        {
            GameObject go = RuntimeUtil.Create(name, transform);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 2;
            line.material = new Material(Art.AdditiveMaterial);
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = order;
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }

        public void Tick(float deltaTime, float worldSpeed, bool playing)
        {
            float speed = playing ? worldSpeed : 0.9f;
            for (int i = 0; i < dashes.Count; i++)
            {
                Dash dash = dashes[i];
                dash.Z -= speed * deltaTime;
                if (dash.Z < -2f) dash.Z += dashSpan;
                UpdateDash(dash);
            }
            for (int i = 0; i < posts.Count; i++)
            {
                Post post = posts[i];
                post.Z -= speed * deltaTime;
                if (post.Z < -3f) post.Z += postSpan;
                UpdatePost(post);
            }
            wetness = Mathf.Lerp(wetness, EnvironmentController.Instance != null
                ? Mathf.Clamp01(EnvironmentController.Instance.RainAmount) : 0f, 1f - Mathf.Exp(-2f * deltaTime));
            Color wc = wetGlow.color;
            wc.a = wetness * 0.16f;
            wetGlow.color = wc;
        }

        private void UpdateDash(Dash dash)
        {
            float k = Projection.Scale(dash.Z);
            float x = dash.Boundary * k;
            float y = Projection.HorizonY - (Projection.HorizonY - Projection.NearY) * k;
            dash.Renderer.transform.localPosition = new Vector3(x, y, 0f);
            dash.Renderer.transform.localScale = new Vector3(0.55f * k, 1.25f * k, 1f);
        }

        private void UpdatePost(Post post)
        {
            float k = Projection.Scale(post.Z);
            float x = post.Side * (Projection.NearHalfWidth + 0.18f) * k;
            float baseY = Projection.HorizonY - (Projection.HorizonY - Projection.NearY) * k;
            post.Pole.transform.localPosition = new Vector3(x, baseY + 0.52f * k, 0f);
            post.Pole.transform.localScale = new Vector3(0.14f * k, 1.05f * k, 1f);
            post.Lamp.transform.localPosition = new Vector3(x, baseY + 1.12f * k, 0f);
            post.Lamp.transform.localScale = new Vector3(0.9f * k, 0.9f * k, 1f);
            post.Pole.sortingOrder = Projection.SortingOrder(post.Z, 180);
            post.Lamp.sortingOrder = post.Pole.sortingOrder + 1;
        }

        public void ApplyPalette(Color roadTint, Color top, Color bottom)
        {
            if (roadMaterial != null)
            {
                Color c = Color.Lerp(roadTint, new Color(0.22f, 0.26f, 0.34f, 1f), 0.45f);
                c.a = 1f;
                roadMaterial.color = c;
            }
            Color edge = Color.Lerp(bottom, top, 0.4f);
            edge = Color.Lerp(edge, Palette.Primary, 0.45f);
            if (leftEdge != null)
            {
                leftEdge.startColor = edge;
                leftEdge.endColor = edge;
                leftEdgeGlow.startColor = RuntimeUtil.WithAlpha(edge, 0.08f);
                leftEdgeGlow.endColor = RuntimeUtil.WithAlpha(edge, 0.08f);
                Color edge2 = Color.Lerp(top, Palette.Secondary, 0.5f);
                rightEdge.startColor = edge2;
                rightEdge.endColor = edge2;
                rightEdgeGlow.startColor = RuntimeUtil.WithAlpha(edge2, 0.08f);
                rightEdgeGlow.endColor = RuntimeUtil.WithAlpha(edge2, 0.08f);
            }
            if (horizonGlow != null)
            {
                Color hg = Color.Lerp(Palette.Primary, Palette.Secondary, 0.35f);
                hg.a = 0.18f;
                horizonGlow.color = hg;
            }
        }

        private class Dash
        {
            public SpriteRenderer Renderer;
            public float Z;
            public float Boundary;
        }

        private class Post
        {
            public SpriteRenderer Pole;
            public SpriteRenderer Lamp;
            public float Z;
            public int Side;
        }
    }
}
