using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    public class ParallaxCity : MonoBehaviour
    {
        private readonly List<BuildingItem> buildings = new List<BuildingItem>();
        private Material windowsMaterial;
        private Material bodyMaterial;

        public static ParallaxCity Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("ParallaxCity", parent);
            ParallaxCity city = go.AddComponent<ParallaxCity>();
            city.Build();
            return city;
        }

        private void Build()
        {
            bodyMaterial = new Material(Art.SpriteMaterial);
            windowsMaterial = new Material(Art.AdditiveMaterial);
            System.Random rng = new System.Random(90210);
            int count = 28;
            for (int i = 0; i < count; i++)
            {
                BuildingItem item = CreateBuilding(i, rng);
                item.Z = 6f + i * 5.8f + (float)rng.NextDouble() * 5f;
                item.Side = (i % 2 == 0) ? -1 : 1;
                item.WidthScale = 0.9f + (float)rng.NextDouble() * 1.3f;
                item.HeightScale = 2.6f + (float)rng.NextDouble() * 5.2f;
                item.XOffset = 0.35f + (float)rng.NextDouble() * 1.9f;
                item.NeonColor = NeonColor(rng);
                item.Phase = (float)rng.NextDouble() * 6.28f;
                Place(item);
                buildings.Add(item);
            }
            ApplyPalette(new Color(0.2f, 0.25f, 0.4f, 1f), new Color(0.7f, 0.9f, 1f, 1f), 0.6f, Color.gray);
        }

        private BuildingItem CreateBuilding(int index, System.Random rng)
        {
            int variant = rng.Next(0, 6);
            BuildingSet set = Art.Building(variant);
            BuildingItem item = new BuildingItem();
            item.Root = RuntimeUtil.Create("Building" + index, transform);
            item.Body = item.Root.AddComponent<SpriteRenderer>();
            item.Body.sprite = set.Body;
            item.Body.sharedMaterial = bodyMaterial;
            item.Body.sortingOrder = 40;
            item.Body.color = new Color(0.2f, 0.25f, 0.4f, 1f);

            GameObject winGo = RuntimeUtil.Create("Windows", item.Root.transform);
            item.Windows = winGo.AddComponent<SpriteRenderer>();
            item.Windows.sprite = set.Windows;
            item.Windows.sharedMaterial = windowsMaterial;
            item.Windows.sortingOrder = 41;
            item.Windows.color = new Color(0.7f, 0.9f, 1f, 0.45f);

            GameObject neonGo = RuntimeUtil.Create("Neon", item.Root.transform);
            item.Neon = neonGo.AddComponent<SpriteRenderer>();
            item.Neon.sprite = Art.RoundedSprite(64, 16, 8);
            item.Neon.sharedMaterial = windowsMaterial;
            item.Neon.sortingOrder = 42;
            item.Neon.color = new Color(0.3f, 0.9f, 1f, 0.8f);
            item.Neon.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            item.Neon.transform.localScale = new Vector3(1.1f, 0.14f, 1f);

            GameObject glowGo = RuntimeUtil.Create("NeonGlow", item.Root.transform);
            item.NeonGlow = glowGo.AddComponent<SpriteRenderer>();
            item.NeonGlow.sprite = Art.Glow();
            item.NeonGlow.sharedMaterial = windowsMaterial;
            item.NeonGlow.sortingOrder = 43;
            item.NeonGlow.color = new Color(0.3f, 0.9f, 1f, 0.3f);
            item.NeonGlow.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            item.NeonGlow.transform.localScale = new Vector3(2.5f, 0.8f, 1f);
            return item;
        }

        private static Color NeonColor(System.Random rng)
        {
            int pick = rng.Next(0, 6);
            switch (pick)
            {
                case 0: return Palette.Primary;
                case 1: return Palette.Secondary;
                case 2: return Palette.Hex("#FF38D0");
                case 3: return Palette.Hex("#5CFFB1");
                case 4: return Palette.Gold;
                default: return Palette.Hex("#4D8BFF");
            }
        }

        private void Place(BuildingItem item)
        {
            float k = Projection.Scale(item.Z);
            float baseX = item.Side * (Projection.NearHalfWidth + item.XOffset + item.WidthScale * 0.6f) * k;
            float baseY = Projection.Y(item.Z);
            float heightWorld = 3.84f * item.HeightScale * k;
            float widthWorld = 0.96f * item.WidthScale * k;
            item.Root.transform.localPosition = new Vector3(baseX, baseY + heightWorld * 0.5f, 0f);
            item.Root.transform.localScale = new Vector3(item.WidthScale * k, item.HeightScale * k, 1f);
            int order = Mathf.Clamp(70 - Mathf.RoundToInt(item.Z * 0.35f), 12, 70);
            item.Body.sortingOrder = order;
            item.Windows.sortingOrder = order + 1;
            item.Neon.sortingOrder = order + 2;
            item.NeonGlow.sortingOrder = order + 3;
            item.Neon.color = new Color(item.NeonColor.r, item.NeonColor.g, item.NeonColor.b, 0.9f);
            item.NeonGlow.color = new Color(item.NeonColor.r, item.NeonColor.g, item.NeonColor.b, 0.22f);
            bool hasNeon = (item.Root.transform.GetSiblingIndex() & 2) == 0;
            item.Neon.gameObject.SetActive(hasNeon);
            item.NeonGlow.gameObject.SetActive(hasNeon);
        }

        public void Tick(float deltaTime, float worldSpeed, bool playing, float menuTime)
        {
            float speed = playing ? worldSpeed : 1.6f;
            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingItem item = buildings[i];
                item.Z -= speed * deltaTime;
                if (item.Z < -8f)
                {
                    item.Z = 150f + UnityEngine.Random.value * 45f;
                    item.Side = (UnityEngine.Random.value < 0.5f) ? -1 : 1;
                    item.WidthScale = 0.9f + UnityEngine.Random.value * 1.3f;
                    item.HeightScale = 2.6f + UnityEngine.Random.value * 5.2f;
                    item.XOffset = 0.35f + UnityEngine.Random.value * 1.9f;
                    item.NeonColor = NeonColor(new System.Random(item.GetHashCode() + Mathf.RoundToInt(Time.time * 100f)));
                }
                Place(item);
                // Neon pulse
                float pulse = 0.72f + 0.28f * Mathf.Sin(menuTime * 2.1f + item.Phase);
                Color nc = item.Neon.color;
                nc.a = 0.8f * pulse;
                item.Neon.color = nc;
                Color gc = item.NeonGlow.color;
                gc.a = 0.18f * pulse;
                item.NeonGlow.color = gc;
            }
        }

        public void ApplyPalette(Color cityTint, Color windowColor, float windowAlpha, Color fog)
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingItem item = buildings[i];
                float depth = Mathf.Clamp01(item.Z / 160f);
                Color body = Color.Lerp(cityTint, fog, 0.12f + depth * 0.28f);
                body.a = 1f;
                item.Body.color = body;
                Color win = windowColor;
                win.a = windowAlpha * (1f - depth * 0.45f);
                item.Windows.color = win;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < buildings.Count; i++) buildings[i].Root.SetActive(false);
            buildings.Clear();
        }

        private class BuildingItem
        {
            public GameObject Root;
            public SpriteRenderer Body;
            public SpriteRenderer Windows;
            public SpriteRenderer Neon;
            public SpriteRenderer NeonGlow;
            public float Z;
            public int Side;
            public float WidthScale;
            public float HeightScale;
            public float XOffset;
            public Color NeonColor;
            public float Phase;
        }
    }
}
