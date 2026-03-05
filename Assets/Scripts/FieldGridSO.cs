using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
using UnityEditor;

[CreateAssetMenu(fileName = "New Field Grid", menuName = "Field Grid")]
public class FieldGridSO : ScriptableObject
{
   public Vector2Int GridSize = new Vector2Int(150, 100);
   public float CellSize = 1f;
   public Vector2 origin = Vector2.zero;
   public bool isFieldReady;

   private Vector2Int target;
   
   private bool[,] _obstacleField;
   private int[,] _costField;
   private Vector2[,] _flowField;
   public void Init(Vector2Int gridSize, float cellSize, Vector2 ori)
   {
      
      GridSize = gridSize;
      CellSize = cellSize;
      origin = ori;
   }

   public async void SetTarget(Vector2 target)
   {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      Vector2Int intTarget = Vector2Int.zero;
      int x = (int)Math.Floor((target.x - origin.x) / CellSize);
      int y = (int)Math.Floor((target.y - origin.y) / CellSize);
      if (x >= 0 && x < GridSize.x && y >= 0 && y < GridSize.y)
      {
         intTarget = new Vector2Int(x, y);
         this.target = intTarget;
      }
      Action act = delegate {
         CalculateCostField(intTarget);
         CalculateFlowField();
      };
      await Task.Run(act);
      // Debug.Log("------------------------------------------------------");
      // CalculateFlowField1();
      if (!isFieldReady)
      {
         isFieldReady = true;
      }
      sw.Stop();
      Debug.Log(sw.ElapsedMilliseconds + " ms");
   }

   public Vector2 GetFlowField(int x, int y)
   {
      if (x >= 0 && x < GridSize.x && y >= 0 && y < GridSize.y)
      {
         return _flowField[x, y];  
      }
      else
      {
         Vector2Int flowdir = Vector2Int.zero;
         if (x < 0)
         {
            flowdir.x = 1;
         }
         else if (x > GridSize.x)
         {
            flowdir.x = -1;
         }

         if (y < 0)
         {
            flowdir.y = 1;
         }
         else if (y > GridSize.y)
         {
            flowdir.y = -1;
         }
         
         return flowdir;
      }
   }
   public Vector2 GetFlowFieldVector(Vector2 pos)
   {
      if (_costField == null || _flowField == null)
      {
         return Vector2Int.zero;
      }
      
      Vector2Int intPos = Vector2Int.zero;
      int x = (int)Math.Floor((pos.x - origin.x) / CellSize);
      int y = (int)Math.Floor((pos.y - origin.y) / CellSize);
      if (x >= 0 && x < GridSize.x && y >= 0 && y < GridSize.y)
      {
         intPos = new Vector2Int(x, y);
         Vector2 cellPos = new Vector2(origin.x + (x + 0.5f) * CellSize, origin.y + (y + 0.5f) * CellSize);

         int lx = intPos.x <= cellPos.x ? -1 : 1;
         int ly = intPos.y <= cellPos.y ? -1 : 1;
         float wx = (pos.x - cellPos.x) * 2/ CellSize;
         float wy = (pos.y - cellPos.y) * 2/ CellSize;
         Vector2 x1 = Vector2.Lerp(GetFlowField(intPos.x, intPos.y), GetFlowField(intPos.x + lx, intPos.y), wx);
         Vector2 x2 = Vector2.Lerp(GetFlowField(intPos.x, intPos.y + ly), GetFlowField(intPos.x + lx, intPos.y + ly), wx);
         Vector2 res = Vector2.Lerp(x1, x2, wy);
         return res.normalized;
      }
      else
      {
         Vector2Int flowdir = Vector2Int.zero;
         if (x < 0)
         {
            flowdir.x = 1;
         }
         else if (x > GridSize.x)
         {
            flowdir.x = -1;
         }

         if (y < 0)
         {
            flowdir.y = 1;
         }
         else if (y > GridSize.y)
         {
            flowdir.y = -1;
         }
         
         return flowdir;
      }
   }
   struct BakeObstacleFieldArgs
   {
      public Vector2 center;
      public Vector2 halfSize;

      public BakeObstacleFieldArgs(Vector2 center, Vector2 halfSize)
      {
         this.center = center;
         this.halfSize = halfSize;
      }
   }
   public void BakeObstacleField(int obstacleMask)
   {
      if (_obstacleField == null)
      {
         _obstacleField = new bool[GridSize.x, GridSize.y];
      }
      else
      {
         Array.Clear(_obstacleField, 0, _obstacleField.Length);
      }
      
      Stopwatch sw = new Stopwatch();
      sw.Start();
      var center = origin + new Vector2(GridSize.x * CellSize / 2f, GridSize.y * CellSize / 2f);
      var halfSize = new Vector2(GridSize.x * CellSize / 2f, GridSize.y * CellSize / 2f);
      Stack<BakeObstacleFieldArgs> _argsStack = new Stack<BakeObstacleFieldArgs>();
      BakeObstacleFieldArgs args = new BakeObstacleFieldArgs(center, halfSize);
      
      _argsStack.Push(args);
   
      while (_argsStack.Count > 0 && _argsStack.Count <= 300)
      {
         args = _argsStack.Pop();
         
         bool isHit = Physics.CheckBox(
            new Vector3(args.center.x, args.center.y, 0f),
            new Vector3(args.halfSize.x, args.halfSize.y, 1f),
            quaternion.identity,
            1 << obstacleMask
         );
         if (!isHit)
         {
            continue;
         }
         
         if (args.halfSize.x <= CellSize * 0.5f && args.halfSize.y <= CellSize * 0.5f)
         {
            Vector2Int intPos = Vector2Int.zero;
            int x = (int)Math.Floor((args.center.x - origin.x) / CellSize);
            int y = (int)Math.Floor((args.center.y - origin.y) / CellSize);
            if (x >= 0 && x < GridSize.x && y >= 0 && y < GridSize.y)
            {
               intPos = new Vector2Int(x, y);
               _obstacleField[intPos.x, intPos.y] = true;
            }
            continue;
         }
         
         Vector2 newHalfSize = new Vector2(0.5f * args.halfSize.x, 0.5f * args.halfSize.y);
         Vector2[] newCenters = {
            new Vector2(args.center.x - newHalfSize.x, args.center.y - newHalfSize.y),
            new Vector2(args.center.x + newHalfSize.x, args.center.y + newHalfSize.y),
         
            new Vector2(args.center.x - newHalfSize.x, args.center.y + newHalfSize.y),
            new Vector2(args.center.x + newHalfSize.x, args.center.y - newHalfSize.y),
         };

         foreach (var newCenter in newCenters)
         {
            BakeObstacleFieldArgs newArgs = new BakeObstacleFieldArgs(newCenter, newHalfSize);
            _argsStack.Push(newArgs);
         }
         
      }

      if (_argsStack.Count >= 500)
      {
         Debug.Log("Too many obstacle fields");
      }
      sw.Stop();
      Debug.Log(sw.ElapsedMilliseconds + "ms");
   }

   private void BakeTest(string obstacleMask)
   {
      Stopwatch sw = new Stopwatch();
      sw.Start();

      for (int x = 0; x < GridSize.x; x++)
      {
         for (int y = 0; y < GridSize.y; y++)
         {
            Vector3 pos = new Vector3(origin.x + x * CellSize + 0.5f * CellSize, origin.y + y * CellSize + 0.5f * CellSize, -5f);
            var isHit = Physics.Raycast(
               pos,
               new Vector3(0f, 0f, 1f),
               Mathf.Infinity,
               1 << LayerMask.NameToLayer(obstacleMask)
            );
            if (isHit)
            {
               _obstacleField[x, y] = true;
            }
         }
      }
      sw.Stop();
      Debug.Log(sw.ElapsedMilliseconds);

   }
   private void CalculateCostField(Vector2Int target)
   {
      if (_costField == null)
      {
         _costField = new int[GridSize.x, GridSize.y];
      }
      
      bool[,] isVisited = new bool[GridSize.x, GridSize.y];

      Vector2Int[] dirs = {
         new Vector2Int(0, -1),  // up
         new Vector2Int(0, 1),   // down
         new Vector2Int(-1, 0),  // left
         new Vector2Int(1, 0),   // right
         new Vector2Int(1, 1),
         new Vector2Int(-1, 1),
         new Vector2Int(-1, -1),
         new Vector2Int(1, -1)
      };
      
      Queue<int[]> queue = new Queue<int[]>();
      queue.Enqueue(new[] { target.x, target.y , 0});
      isVisited[target.x, target.y] = true;
      while (queue.Count != 0)
      {
         Int32[] current = queue.Dequeue();
         
         int x = current[0];
         int y = current[1];
         int lCost = current[2];
         
         _costField[x, y] = lCost;

         foreach (var dir in dirs)
         {
            int x1 = x + dir.x;
            int y1 = y + dir.y;
            if (x1 < GridSize.x && x1 >= 0 && y1 < GridSize.y && y1 >= 0 &&
                isVisited[x1, y1] == false)
            {
               isVisited[x1, y1] = true;

               if (_obstacleField[x1, y1])
               {
                  _costField[x1, y1] = int.MaxValue;
               }
               else
               {
                  queue.Enqueue(new[] { x1, y1, lCost + 1});
               }
            }
            
         }
         
      }
   }

   private void CalculateFlowField()
   {
      Stopwatch ss = new Stopwatch();
      ss.Start();
      _flowField = new Vector2[GridSize.x, GridSize.y];
      
      Vector2Int[] dirs = {
         new Vector2Int(0, -1),//up
         new Vector2Int(0, 1),// down
         new Vector2Int(-1, 0),
         new Vector2Int(1, 0),
         new Vector2Int(1, 1),
         new Vector2Int(-1, 1),
         new Vector2Int(-1, -1),
         new Vector2Int(1, -1),
      };
      ss.Stop();
      Debug.Log(ss.ElapsedMilliseconds + "ms. 初始化数组");
      
      ss.Restart();
      for (int x = 0; x < GridSize.x; x++)
      {
         for (int y = 0; y < GridSize.y; y++)
         {
            if (new Vector2Int(x, y) == target)
            {
               _flowField[x, y] = Vector2.zero;
               continue;
            }
            int minCostMove = int.MaxValue;
            Vector2 minDir = new Vector2(x, y);
            foreach (var dir in dirs)
            {
               int ix = x + dir.x;
               int iy = y + dir.y;
               if (ix >= 0 && ix < GridSize.x && iy >= 0 && iy < GridSize.y && _obstacleField[ix, iy] == false)
               {
                  if (_costField[ix, iy] < minCostMove)
                  {
                     minCostMove = _costField[ix, iy];
                     minDir = dir;
                  }
               }
            }
            _flowField[x, y] = minDir.normalized;
         }
      }
      ss.Stop();
      Debug.Log(ss.ElapsedMilliseconds + "ms. 计算数据");
   }

   private void CalculateFlowField1()
   {
      Stopwatch ss = new Stopwatch();
      ss.Start();
      _flowField = new Vector2[GridSize.x, GridSize.y];
      
      int totalCells = GridSize.x * GridSize.y;
      
      NativeArray<float2> flowFieldNative = new NativeArray<float2>(totalCells, Allocator.TempJob);
      NativeArray<int> costFieldNative = new NativeArray<int>(totalCells, Allocator.TempJob);
      NativeArray<bool> obstacleFieldNative = new NativeArray<bool>(totalCells, Allocator.TempJob);
      
      // 复制数据到Native Arrays（这部分也可以优化）
      for (int x = 0; x < GridSize.x; x++)
      {
         for (int y = 0; y < GridSize.y; y++)
         {
            int index = y * GridSize.x + x;
            costFieldNative[index] = _costField[x, y];
            obstacleFieldNative[index] = _obstacleField[x, y];
         }
      }
      ss.Stop();
      Debug.Log(ss.ElapsedMilliseconds + " ms 初始化数组与复制数据。");
      
      
      ss.Restart();
      // 2. 创建并调度Job
      var job = new FieldGridCaculateJob
      {
         GridSize = new int2(GridSize.x, GridSize.y),
         ObstacleField = obstacleFieldNative,
         CostField = costFieldNative,
         FlowField = flowFieldNative
      };
      // 调度Job（并行执行）
      JobHandle jobHandle = job.Schedule(totalCells, totalCells / 10);
      jobHandle.Complete();
      ss.Stop();
      Debug.Log(ss.ElapsedMilliseconds + " ms in multiTread job.");
      
      ss.Restart();
      // 3. 将结果复制回数组
      for (int x = 0; x < GridSize.x; x++)
      {
         for (int y = 0; y < GridSize.y; y++)
         {
            int index = y * GridSize.x + x;
            _flowField[x, y] = new Vector2(flowFieldNative[index].x, flowFieldNative[index].y);
         }
      }
      ss.Stop();
      Debug.Log(ss.ElapsedMilliseconds + " ms 回传数据。");
    
      // 4. 清理Native Arrays
      flowFieldNative.Dispose();
      costFieldNative.Dispose();
      obstacleFieldNative.Dispose();
      
   }
   public void DrawTempMapGizmos(Vector2 ori)
   {
      if (_costField == null)
      {
         return;
      }
      for (int x = 0; x < GridSize.x; x++)
      {
         for (int y = 0; y < GridSize.y; y++)
         {
            Gizmos.color = Color.white;
            Vector2 pos = new Vector2(origin.x + (x + 0.5f) * CellSize, origin.y + (y + 0.5f) * CellSize);
            if (_costField != null)
            {
               // Color color = Color.Lerp(Color.blue, Color.red, _costField[x, y] / (float)(GridSize.x + GridSize.y));
               // Gizmos.color = new Color(color.r, color.g, color.b, 0.2f);
               // Gizmos.DrawCube(pos, new Vector3(CellSize, CellSize, 0f));
               // Gizmos.color = Color.white;
               if (_costField[x, y] == int.MaxValue)
               {
                  Handles.Label(pos, "Max");
               }
               else
               {
                  Handles.Label(pos, _costField[x, y].ToString());
               }
            }
         }
      }
   }

   public void DrawGridGizmos(Vector2 ori)
   {
      Gizmos.color = Color.white;
      for (int x = 0; x < GridSize.x; x++)
      {
         Gizmos.DrawLine(ori + new Vector2(x * CellSize, 0f), ori + new Vector2(x * CellSize, GridSize.y * CellSize));
      }
      for (int y = 0; y < GridSize.y; y++)
      {
         Gizmos.DrawLine(ori + new Vector2(0f, y * CellSize), ori + new Vector2(GridSize.x * CellSize, y * CellSize));
      }
   }
   public void DrawObstacleFieldGizmos(Vector2 ori)
   {
      if (_obstacleField == null)
      {
         return;
      }
      for (int x = 0; x < GridSize.x; x++)
      {
         for (int y = 0; y < GridSize.y; y++)
         {
            Vector2 pos = new Vector2(origin.x + (x + 0.5f) * CellSize, origin.y + (y + 0.5f) * CellSize);
            if (_obstacleField[x, y])
            {
               Gizmos.color = Color.yellow;
               Gizmos.DrawCube(pos, new Vector3(CellSize, CellSize, 0f));
               Gizmos.color = Color.white;
            }
         }
      }
   }

   public void DrawFlowVector(Vector2 ori)
   {
      if (_flowField == null)
      {
         return;
      }
      for (int x = 0; x < GridSize.x; x++)
      {
         for (int y = 0; y < GridSize.y; y++)
         {
            Vector2 pos = new Vector2(origin.x + (x + 0.5f) * CellSize, origin.y + (y + 0.5f) * CellSize);
               Gizmos.color = Color.green;
               Gizmos.DrawLine(pos, pos + _flowField[x, y] * CellSize * 0.5f);
               Gizmos.color = Color.white;
         }
      }
   }
}
