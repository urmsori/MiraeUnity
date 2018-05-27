using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoEnvironment : MonoBehaviour
{
    Mirae.MiraeService service;

    // Use this for initialization
    void Start()
    {
        service.Environment.AddExecuteBlock(12, () => { Move(); });
        service.Environment.AddIfBlock(11, this, (thisEnv) => { return thisEnv.enabled; });
    }

    public void Move()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
