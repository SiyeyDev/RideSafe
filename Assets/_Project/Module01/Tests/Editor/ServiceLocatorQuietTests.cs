using Cachacos;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// UIText pide el proveedor de localización en cada texto, y ServiceLocator escribía un
    /// Debug.Log con traza de pila en cada petición: al abrir un panel, docenas de logs en un
    /// fotograma, que en Quest es un tirón visible.
    /// </summary>
    public class ServiceLocatorQuietTests
    {
        private sealed class Probe { }

        [TearDown]
        public void TearDown() => ServiceLocator.Instance.TryDeregisterService(new Probe());

        [Test]
        public void Pedir_un_servicio_no_escribe_en_la_consola()
        {
            ServiceLocator.Instance.RegisterService(new Probe());
            for (int i = 0; i < 20; i++)
                Assert.IsNotNull(ServiceLocator.Instance.RequestService<Probe>());
            LogAssert.NoUnexpectedReceived();
        }
    }
}
