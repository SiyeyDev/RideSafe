using System;
using System.Linq;
using System.Reflection;
using Cachacos;
using NUnit.Framework;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Lo que se suscribe al cambio de idioma tiene que poder desuscribirse. Si no, cada
    /// <see cref="Module01Text"/> de una escena que ya se cerró sigue en la lista de I2, y el
    /// siguiente cambio de idioma —o el LocalizeAll al cargar otra escena— lo llama ya destruido:
    /// <c>MissingReferenceException</c> en Module01Text.Refresh, visto al pasar de una escena a otra.
    /// <para>
    /// Se mira la lista de invocación de I2 por reflexión, sin cambiar el idioma: cambiarlo en el
    /// editor relocaliza la escena abierta.
    /// </para>
    /// </summary>
    public class LanguageSubscriptionTests
    {
        private static int Listeners()
        {
            Type manager = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("I2.Loc.LocalizationManager")).First(t => t != null);
            FieldInfo field = manager.GetField("OnLocalizeEvent", BindingFlags.Static | BindingFlags.NonPublic);
            return (field.GetValue(null) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        [Test]
        public void Quien_se_desuscribe_del_idioma_sale_de_la_lista_de_I2()
        {
            ILocalizationProvider provider = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            Assume.That(provider, Is.Not.Null, "Sin proveedor de idioma registrado.");

            int before = Listeners();
            void Refresh() { }
            // Como Module01Text: dos grupos de método distintos, uno al suscribir y otro al quitar.
            provider.LanguageChanged += Refresh;
            Assert.AreEqual(before + 1, Listeners());
            provider.LanguageChanged -= Refresh;

            Assert.AreEqual(before, Listeners(), "El oyente sigue en I2 después de desuscribirse.");
        }
    }
}
