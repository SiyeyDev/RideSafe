using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.Tutorial.Presentation;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Tutorial.Tests
{
    /// <summary>
    /// The tutorial shows the controller lying on its side, ring to the right and handle down to the
    /// left, the trigger in profile under the ring (PO, 2026-11-04, with a product photo). Both hands
    /// point the same way on screen. The hint view faces the learner along its +Z, so the learner, looking
    /// down its -Z, has its -X on their right.
    /// </summary>
    public class TriggerPoseTests
    {
        [TestCase("Assets/_Project/Tutorial/Controllers/PF_TouchPlus_Right.prefab", "R_")]
        [TestCase("Assets/_Project/Tutorial/Controllers/PF_TouchPlus_Left.prefab", "L_")]
        public void TheTriggerPose_ShowsTheControllerInProfile_RingToTheRight(string path, string prefix)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject model = Object.Instantiate(prefab);
            try
            {
                model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Vector3 centre = Find(model.transform, prefix + "Controller_Mesh").GetComponent<Renderer>().bounds.center;
                Dictionary<ControllerControl, Vector3> part = new Dictionary<ControllerControl, Vector3>();
                foreach (ControllerButtonVisual button in model.GetComponentsInChildren<ControllerButtonVisual>(true))
                    part[button.Control] = (button.GetComponent<Renderer>().bounds.center - centre).normalized;
                Vector3 top = ((part[ControllerControl.PrimaryButton] + part[ControllerControl.SecondaryButton]) * 0.5f).normalized;
                Vector3 front = Vector3.ProjectOnPlane(part[ControllerControl.Trigger], top).normalized;

                Quaternion pose = model.GetComponent<ControllerModelVisual>().PoseFor(ControllerControl.Trigger).rotation;
                Vector3 topOnScreen = pose * top, frontOnScreen = pose * front;
                Assert.Less(frontOnScreen.x, -0.5f, "the ring points to the learner's right");
                Assert.Greater(topOnScreen.y, 0.5f, "the buttons' face up, not toward the learner");
                Assert.Less(Mathf.Abs(topOnScreen.z), 0.3f, "only the rim of that face shows: seen in profile");
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
            Assert.Fail(root.name + " has no " + name);
            return null;
        }
    }
}
