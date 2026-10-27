using NUnit.Framework;
using RideSafe.Module01.Editor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Las fracciones de los mounts se escribieron con el eje largo en X. El scooter de
    /// 2026-10-26 lo trae en Z: sin esto, «0,95 a lo largo» caía a un costado.
    /// </summary>
    public class VehicleMountTests
    {
        [Test]
        public void Con_el_eje_largo_en_X_la_u_va_a_lo_largo_de_X()
        {
            Bounds local = new Bounds(Vector3.zero, new Vector3(2f, 1f, .4f));
            Vector3 p = Module01VehicleStaging.MountPosition(local, new Vector3(1f, .5f, .5f));
            Assert.AreEqual(1f, p.x, 1e-4f);
            Assert.AreEqual(0f, p.z, 1e-4f);
        }

        [Test]
        public void Con_el_eje_largo_en_Z_la_u_va_a_lo_largo_de_Z()
        {
            Bounds local = new Bounds(Vector3.zero, new Vector3(.4f, 1f, 2f));
            Vector3 p = Module01VehicleStaging.MountPosition(local, new Vector3(1f, .5f, .5f));
            Assert.AreEqual(1f, p.z, 1e-4f, "u = 1 es el extremo alto del eje largo, que es Z.");
            Assert.AreEqual(0f, p.x, 1e-4f, "w = .5 es el centro del ancho, que ahora es X.");
        }
    }
}
