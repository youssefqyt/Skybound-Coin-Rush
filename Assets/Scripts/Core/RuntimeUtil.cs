using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    public static class RuntimeUtil
    {
        public static GameObject Create(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, int sortingOrder,
            Color color, Vector3 localPosition, Vector3 localScale, bool additive = false)
        {
            GameObject go = Create(name, parent);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = additive ? Art.AdditiveMaterial : Art.SpriteMaterial;
            return renderer;
        }

        public static SpriteRenderer CreateGlow(string name, Transform parent, int sortingOrder, Color color,
            Vector3 localPosition, float scale)
        {
            return CreateSprite(name, parent, Art.Glow(), sortingOrder, color, localPosition,
                new Vector3(scale, scale, 1f), true);
        }

        public static MeshRenderer CreateMesh(string name, Transform parent, Mesh mesh, Material material,
            int sortingOrder)
        {
            GameObject go = Create(name, parent);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        public static Material MakeSpriteMaterial(Texture2D texture, bool additive = false)
        {
            Material mat = new Material(additive ? Art.AdditiveMaterial : Art.SpriteMaterial);
            mat.mainTexture = texture;
            return mat;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        public static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }

    public class SimplePool<T> where T : Component
    {
        private readonly Func<T> factory;
        private readonly Stack<T> free = new Stack<T>();
        private readonly List<T> all = new List<T>();
        private readonly Transform parent;

        public SimplePool(Func<T> factory, Transform parent, int prewarm = 0)
        {
            this.factory = factory;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                T item = factory();
                if (parent != null) item.transform.SetParent(parent, false);
                item.gameObject.SetActive(false);
                free.Push(item);
                all.Add(item);
            }
        }

        public int Count { get { return all.Count; } }

        public T Get()
        {
            T item;
            if (free.Count > 0)
            {
                item = free.Pop();
            }
            else
            {
                item = factory();
                if (parent != null) item.transform.SetParent(parent, false);
                all.Add(item);
            }
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            free.Push(item);
        }

        public void ReleaseAll(List<T> active)
        {
            for (int i = 0; i < active.Count; i++) Release(active[i]);
            active.Clear();
        }
    }
}
