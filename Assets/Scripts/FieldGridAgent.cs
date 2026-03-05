using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class FieldGridAgent : MonoBehaviour
{
    // Start is called before the first frame update
    public MeshBaker baker;
    
    private Rigidbody2D rb2d;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (baker != null)
        {
            Vector2 dir = baker.FindDir(transform.position);
            rb2d.velocity = dir * 0.5f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position,transform.position + (Vector3)rb2d.velocity.normalized);
    }
}
