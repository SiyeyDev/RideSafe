using NUnit.Framework;
using RideSafe.Module01.Editor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Las miniaturas de lo traslúcido (las gafas transparentes) se oscurecen y ganan alfa para
    /// leerse sobre la tarjeta clara; lo opaco sale igual que antes.
    /// </summary>
    public class ThumbnailSeeThroughTests
    {
        [Test]
        public void Lo_opaco_no_cambia()
        {
            Color[] px = { Color.clear, new Color(.8f, .2f, .1f, 1f), new Color(.8f, .2f, .1f, .5f), new Color(1, 1, 1, 1) };
            Color[] before = (Color[])px.Clone();
            EppThumbnailRenderer.SeeThrough(px);
            CollectionAssert.AreEqual(before, px);
        }

        [Test]
        public void Lo_casi_blanco_se_oscurece_sin_tocar_el_alfa()
        {
            Color[] px = { Color.clear, new Color(.95f, .95f, .95f, 1f), new Color(1, 1, 1, .9f) };
            EppThumbnailRenderer.SeeThrough(px);
            Assert.Less(px[1].r, .7f);
            Assert.AreEqual(1f, px[1].a, 1e-4f);
            Assert.AreEqual(.9f, px[2].a, 1e-4f);
        }

        [Test]
        public void Lo_traslucido_gana_alfa_y_se_oscurece()
        {
            Color[] px = { Color.clear, new Color(1, 1, 1, .2f), new Color(1, 1, 1, .2f), new Color(1, 1, 1, .1f) };
            EppThumbnailRenderer.SeeThrough(px);
            Assert.AreEqual(Color.clear, px[0]);
            Assert.AreEqual(.85f, px[1].a, 1e-4f);
            Assert.Less(px[1].r, 1f);
            Assert.Greater(px[3].a, .1f);
        }
    }
}
