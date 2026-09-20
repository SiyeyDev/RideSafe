using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_FloatShareData", menuName = "System/ShareData/Float", order = 51)]
public class FloatShareDataSO : BaseShareDataSO<float> 
{
}
[Serializable]
public class FloatSharedData : SharedData<float>
{

}
