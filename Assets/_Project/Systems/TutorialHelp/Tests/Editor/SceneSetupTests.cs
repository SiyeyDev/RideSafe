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
                Assert.AreEqual(0.062f, caption.transform.localPosition.y, 1e-4f, "the first build must already place it");
                Vector2 saved = new SerializedObject(caption.rectTransform).FindProperty("m_AnchoredPosition").vector2Value;
                Assert.AreEqual(0.062f, saved.y, 1e-4f, "what the scene file stores, which wins on reload");
                // 2026-10-16: two lines rose above the card. The box stays inside the card's top, and
                // narrow enough that the sentence wraps with room at both sides.
                float top = caption.transform.localPosition.y + caption.rectTransform.sizeDelta.y * 0.5f;
                Assert.LessOrEqual(top, 0.21f * 0.5f, "inside the 21 cm card, centred on the hint");
                Assert.LessOrEqual(caption.rectTransform.sizeDelta.x, 0.2f - 2f * 0.03f,
                                   "two lines with room at both sides of the card, not one line edge to edge");
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
