using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class AgentsPool : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject AgentPrefab;
    public int MaxAgents = 100;
    public MeshBaker MeshBaker;
    private void Start()
    {
        for (int i = 0; i < MaxAgents; i++)
        {
            GameObject go = Instantiate(AgentPrefab, transform);
            go.transform.position += Vector3.up * 0.01f * i;
            go.GetComponent<FieldGridAgent>().baker = MeshBaker;
        }
    }
}
