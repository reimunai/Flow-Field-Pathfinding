using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AsyncTest : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        await Task.Run(TestAsync);
        Debug.Log("Test");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Update");
    }

    public async Task TestAsync()
    {
        long a = 1;
        for (int i = 1; i <= 100000; i++)
        {
            a *= i;
        }
        
        Debug.Log(a);
    }
}
