using NUnit.Framework;

namespace RideSafe.Module01.Tests
{
    public class Module01ContextTests
    {
        [Test]
        public void Las_claves_y_los_valores_se_normalizan()
        {
            Module01Context context = new Module01Context();
            context.Set("  Vehicle  ", "  EBike  ");

            Assert.AreEqual("ebike", context.Get("vehicle"));
            Assert.AreEqual("ebike", context.Lookup("VEHICLE"));
        }

        [Test]
        public void Una_clave_no_publicada_devuelve_vacio_en_vez_de_null()
        {
            Module01Context context = new Module01Context();

            Assert.AreEqual(string.Empty, context.Lookup("vehicle"));
        }

        [Test]
        public void Reemplazar_un_valor_no_duplica_la_clave()
        {
            Module01Context context = new Module01Context();
            context.Set("vehicle", "ebike");
            context.Set("vehicle", "escooter");

            Assert.AreEqual("escooter", context.Get("vehicle"));
        }
    }
}
