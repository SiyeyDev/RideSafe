using NUnit.Framework;
using UnityEditor;
using RideSafe.TaskSequence;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El criterio de verificación 8 de la spec: cambiar el vehículo en el contexto
    /// carga otra secuencia sin tocar código.
    /// </summary>
    public class SequenceGatingTests
    {
        private const string Folder = "Assets/_Project/Module01/Data/Sequences/";
        private const string Missing = "Corre el menu RideSafe > Modulo 1 > Crear secuencias de zona.";

        private static TaskSequenceSO Load(string assetName) =>
            AssetDatabase.LoadAssetAtPath<TaskSequenceSO>(Folder + assetName + ".asset");

        [Test]
        public void Las_zonas_de_equipo_personal_corren_con_cualquier_vehiculo()
        {
            TaskSequenceSO head = Load("SO_Seq_Module01_Head");
            Assert.IsNotNull(head, Missing);

            Module01Context context = new Module01Context();
            context.Set(Module01Context.VehicleKey, "escooter");

            Assert.IsTrue(head.MatchesContext(context.Lookup, out _),
                "Los elementos personales no dependen del vehiculo.");
        }

        [Test]
        public void Las_zonas_del_ebike_solo_corren_con_el_ebike()
        {
            TaskSequenceSO cockpit = Load("SO_Seq_Module01_Ebike_Cockpit");
            Assert.IsNotNull(cockpit, Missing);

            Module01Context ebike = new Module01Context();
            ebike.Set(Module01Context.VehicleKey, "ebike");
            Assert.IsTrue(cockpit.MatchesContext(ebike.Lookup, out _));

            Module01Context scooter = new Module01Context();
            scooter.Set(Module01Context.VehicleKey, "escooter");
            Assert.IsFalse(cockpit.MatchesContext(scooter.Lookup, out string unmet));
            StringAssert.Contains("vehicle", unmet);
        }

        [Test]
        public void Sin_vehiculo_publicado_las_zonas_del_ebike_no_corren()
        {
            TaskSequenceSO wheels = Load("SO_Seq_Module01_Ebike_Wheels");
            Assert.IsNotNull(wheels, Missing);

            Module01Context empty = new Module01Context();

            Assert.IsFalse(wheels.MatchesContext(empty.Lookup, out _),
                "Un contexto vacio no debe cargar una secuencia de vehiculo.");
        }

        [Test]
        public void Cada_secuencia_declara_su_id_global()
        {
            Assert.AreEqual("Module01.Zone.Head", Load("SO_Seq_Module01_Head")?.SequenceId, Missing);
            Assert.AreEqual("Module01.Vehicle.Visibility", Load("SO_Seq_Module01_Ebike_Visibility")?.SequenceId);
        }
    }
}
