using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour {
    Animator animator;
    float timer;
    FStates state;
    List<Transform> playersAround = new List<Transform>();
    private FishArea fishArea;
    private Vector3 target;
    public enum FStates
    {
        Patrol, Stay, SwimAway
    }

    internal void Move()
    {
        timer -= Time.deltaTime;
        switch (state)
        {
            case FStates.Patrol:
                animator.SetInteger("State", 1 );
                Ray ray = new Ray(transform.position, transform.forward);
                var casts = Physics.RaycastAll(ray, fishArea.raycastDistance);
                foreach (var cast in casts)
                {
                    if (cast.collider.transform != this)
                    {
                        target = fishArea.GetRandomPoint();
                        timer = UnityEngine.Random.Range(0, 10f);
                        Debug.Log("Change direction");
                    }
                }
                transform.position += transform.forward * Time.deltaTime * fishArea.speed;
                transform.forward = Vector3.MoveTowards(transform.forward, target-transform.position, Time.deltaTime*fishArea.rotationSpeed);
                if ((transform.position - target).magnitude < fishArea.speed * Time.deltaTime * 3 || timer<0f)
                {
                    target = fishArea.GetRandomPoint();
                    timer = UnityEngine.Random.Range(0, 10f);
                    if (UnityEngine.Random.Range(0f, 1f) > 0.9f)
                        state = FStates.Stay;
                }
                break;
            case FStates.Stay:
                transform.position += transform.forward * Time.deltaTime * fishArea.speed/20f;
                animator.SetInteger("State", 0);
                if (timer < 0f)
                {
                    if (UnityEngine.Random.Range(0f, 1f) < 0.9f)
                        state = FStates.Patrol;
                }
                break;
            case FStates.SwimAway:
                Vector3 runVector = Vector3.zero;
                foreach (var t in playersAround)
                    runVector += (t.transform.position - transform.position).normalized;
                runVector.Normalize();
                transform.forward = Vector3.MoveTowards(transform.forward, -runVector, Time.deltaTime * fishArea.rotationSpeed*10);
                transform.position += transform.forward * Time.deltaTime * fishArea.speed;
                break;
        }
    }

    internal void Initialize(FishArea fishArea)
    {
        state = FStates.Patrol;
        this.fishArea = fishArea;
        animator = GetComponent<Animator>();
        target = fishArea.GetRandomPoint();
    }

    public void AddPlayer(Transform t)
    {
        playersAround.Add(t);
    }

    public void RemovePlayer(Transform t)
    {
        playersAround.Remove(t);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            AddPlayer(other.transform);
            state = FStates.SwimAway;
            if (animator!=null)
            animator.SetInteger("State", 1);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            RemovePlayer(other.transform);
            if (playersAround.Count == 0)
            {
                state = FStates.Patrol;
                target = fishArea.GetRandomPoint();
            }
        }
    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Ray ray = new Ray(transform.position, transform.forward);
        if (transform.parent == null)
            return;
        fishArea = transform.parent.GetComponent<FishArea>();
        if (fishArea == null)
            return;
        float raycastDistance = fishArea.raycastDistance;
        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, transform.forward * raycastDistance);
    }
}
