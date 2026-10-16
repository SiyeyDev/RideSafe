using NUnit.Framework;
using RideSafe.Help.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Help.Tests
{
    public class SceneSetupTests
    {
        [Test]
        public void ANewCaption_SitsAboveTheController()
        {
            GameObject hint = new GameObject("Gesture Hint");
            try
            {
                TMP_Text caption = TutorialHelpSceneSetup.EnsureCaption(hint.transform);
                Assert.AreEqual(0.075f, caption.transform.localPosition.y, 1e-4f, "the first build must already place it");
                Vector2 saved = new SerializedObject(caption.rectTransform).FindProperty("m_AnchoredPosition").vector2Value;
                Assert.AreEqual(0.075f, saved.y, 1e-4f, "what the scene file stores, which wins on reload");
                Assert.AreEqual(180f, caption.transform.localEulerAngles.y, 1e-3f,
                                "the hint's +Z faces the learner; TMP reads from its back like the backdrop");
            }
            finally
            {
                Object.DestroyImmediate(hint);
            }
        }
    }
}
