using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static  class StepCommandExtensions 
{
    public static string GetGameObjectName(this object obj)
    {
        if (obj is MonoBehaviour mono)
            return mono.gameObject.name;
        return "Objeto sin GameObject";
    }
}
