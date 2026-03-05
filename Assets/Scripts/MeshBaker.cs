using System;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Serialization;

public class MeshBaker : MonoBehaviour
{
    public Vector2Int gridSize = new Vector2Int(100, 100);
    public float cellSize = 0.5f;
    
    public bool TempMapGizmosEnable = true;
    public bool ObstacleFieldGizmosEnable = true;
    public bool GridGizmosEnable = true;
    public bool FlowGizmosEnable = true;
    
    // Start is called before the first frame update
    public FieldGridSO _fieldGrid;
    
    private void Awake()
    {
        if (_fieldGrid != null)
        {
            _fieldGrid.Init(gridSize, cellSize, transform.position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("update");
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            if (_fieldGrid != null)
            {
                _fieldGrid.SetTarget(worldPos);
            }
        }
    }

    public Vector2 FindDir(Vector2 pos)
    {
        return _fieldGrid.GetFlowFieldVector(pos);
    }
    
    private void OnDrawGizmos()
    {
        if (_fieldGrid != null && TempMapGizmosEnable)
        {
            _fieldGrid.DrawTempMapGizmos(transform.position);
        }

        if (_fieldGrid != null && ObstacleFieldGizmosEnable)
        {
            _fieldGrid.DrawObstacleFieldGizmos(transform.position);
        }

        if (GridGizmosEnable && _fieldGrid != null)
        {
            _fieldGrid.DrawGridGizmos(transform.position);
        }

        if (FlowGizmosEnable && _fieldGrid != null)
        {
            _fieldGrid.DrawFlowVector(transform.position);
        }
    }

    public void ttttt()
    {
        Debug.Log("Start Bake Grid Mesh");
        int layer = LayerMask.NameToLayer("obs");
        _fieldGrid.BakeObstacleField(layer);
    }
}

[CustomEditor(typeof(MeshBaker))]
public class MeshBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        //MeshBaker baker = (MeshBaker)target;
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Bake Grid Mesh"))
        {
            ((MeshBaker)target).ttttt();
            EditorUtility.SetDirty(((MeshBaker)target)._fieldGrid);
            AssetDatabase.SaveAssets();
        }
    }
}