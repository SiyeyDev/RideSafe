using Cachacos;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[DefaultExecutionOrder(-10)]
public class DistanceObjectManager : MonoBehaviour
{
    public Dictionary<SelectObjectType, SelectObjectGesture> SeletObjectGestures { get; private set; }
    private void Awake()
    {
        SeletObjectGestures = new Dictionary<SelectObjectType, SelectObjectGesture>();
        ServiceLocator.Instance.RegisterService(this);
    }

    public void RegisteSelectObjectGestures(SelectObjectGesture selectObjectGesture, SelectObjectType selectObjectType)
    {
        if (SeletObjectGestures.ContainsKey(selectObjectType))
        {
            Assert.IsTrue(SeletObjectGestures[selectObjectType] != null, $"///// SERVICE LOCATOR ///// \n SelectObject with Enum {selectObjectType} already registered");
            return;
        }
        SeletObjectGestures.Add(selectObjectType, selectObjectGesture);
    }
    public void DeregisteSelectObjectGestures(SelectObjectGesture selectObjectGesture, SelectObjectType selectObjectType)
    {
        if (SeletObjectGestures.ContainsKey(selectObjectType))
        {
            Assert.IsTrue(SeletObjectGestures[selectObjectType] != null, $"///// SERVICE LOCATOR ///// \n SelectObject with Enum {selectObjectType} desregistered");
            SeletObjectGestures.Remove(selectObjectType);
        }
        Debug.Log($"///// SERVICE LOCATOR ///// \nService {selectObjectType} is not register");
    }

}

public enum SelectObjectType
{
    DistanceObjectPrimary,
    DistanceObjectSecondary
}
