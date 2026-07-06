using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishArea : MonoBehaviour {
    public GameObject[] prefabs;
    public int count;
    public float speed = 10;
    public float rotationSpeed = 5f;
    public float raycastDistance = 10f;
    public int Count
    {
        get
        {
            return count;
        }
        set
        {
            if (count != value)
            {
                count = value;
                SpawnFishes();
                transform.hasChanged = false;
            }
        }
    }

    Fish[] fishes;
    BoxCollider collider;
    Vector3 center;
    Vector3 max;
    Vector3 min;
    void Start()
    {
        fishes = transform.GetComponentsInChildren<Fish>();
        InitializeAllFishes();
    }

    public void InitializeAllFishes()
    {
        if (collider == null)
            collider = GetComponent<BoxCollider>();
        max = center + collider.size / 2f;
        min = center - collider.size / 2f;
        for (int i = 0; i < fishes.Length; i++)
            fishes[i].Initialize(this);
    }

    public void SpawnFishes()
    {
        fishes = transform.GetComponentsInChildren<Fish>();
        if (collider == null)
            collider = GetComponent<BoxCollider>();
        center = collider.center;
        max = center + collider.size / 2f;
        min = center - collider.size / 2f;
        if (fishes.Length < count)
            for (int i = fishes.Length; i < count; i++)
                SpawnFish();
        else
            if (fishes.Length > count)
            for (int i = 0; i < fishes.Length - count; i++)
#if UNITY_EDITOR
                DestroyImmediate(fishes[i].gameObject);
#else
                Destroy(fishes[i].gameObject);
#endif
        fishes = transform.GetComponentsInChildren<Fish>();
        MixPositions();
    }

    public void RemoveFishes()
    {
        fishes = transform.GetComponentsInChildren<Fish>();
        for (int i = 0; i < fishes.Length; i++)
#if UNITY_EDITOR
            DestroyImmediate(fishes[i].gameObject);
#else
                Destroy(fishes[i].gameObject);
#endif
    }

    public void Update()
    {
        UpdateFishes();
    }

    private void UpdateFishes()
    {
        for (int i = 0; i < fishes.Length; i++)
        {
            fishes[i].Move();
        }
    }

    public void SpawnFish()
    {
        GameObject temp = Instantiate(GetRandomPrefab(), transform);

    }

    public void MixPositions()
    {
        for (int i = 0; i < fishes.Length; i++)
        {
            Transform temp = fishes[i].transform;
            temp.position = GetRandomPoint();
            temp.Rotate(0f, UnityEngine.Random.Range(0f, 360f), 0f, Space.Self);
        }

    }

    protected GameObject GetRandomPrefab()
    {
        int id = UnityEngine.Random.Range(0, prefabs.Length);
        return prefabs[id];
    }

    public Vector3 GetRandomPoint()
    {
        
        float x = UnityEngine.Random.Range(min.x, max.x);
        float y = UnityEngine.Random.Range(min.y, max.y);
        float z = UnityEngine.Random.Range(min.z, max.z);
        Vector3 p = new Vector3(x, y, z);
        return transform.TransformPoint(p);
    }
}
