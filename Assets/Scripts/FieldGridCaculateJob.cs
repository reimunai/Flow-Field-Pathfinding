using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompatible]
public struct FieldGridCaculateJob : IJobParallelFor
{
    [ReadOnly] public int2 GridSize;
    [ReadOnly] public NativeArray<bool> ObstacleField;
    [ReadOnly] public NativeArray<int> CostField;
    [WriteOnly] public NativeArray<float2> FlowField;
    
    public void Execute(int index)
    {
        int x = index % GridSize.x;
        int y = index / GridSize.x;
        
        int minCostMove = int.MaxValue;
        float2 minDir = new float2(0, 0);
        
        // 定义方向数组（直接在方法内部）
        int2[] dirs = {
            new int2(0, -1),  // up
            new int2(0, 1),   // down
            new int2(-1, 0),  // left
            new int2(1, 0),   // right
            new int2(1, 1),
            new int2(-1, 1),
            new int2(-1, -1),
            new int2(1, -1)
        };
        
        foreach (var dir in dirs)
        {
            int ix = x + dir.x;
            int iy = y + dir.y;
            
            if (ix >= 0 && ix < GridSize.x && iy >= 0 && iy < GridSize.y)
            {
                int gridIndex = iy * GridSize.x + ix;
                
                if (!ObstacleField[gridIndex])
                {
                    int cost = CostField[gridIndex];
                    if (cost < minCostMove)
                    {
                        minCostMove = cost;
                        minDir = new float2(dir.x, dir.y);
                    }
                }
            }
        }
        
        // 归一化方向向量
        float length = math.sqrt(minDir.x * minDir.x + minDir.y * minDir.y);
        if (length > 0)
        {
            minDir /= length;
        }
        
        FlowField[index] = minDir;
    }
}
