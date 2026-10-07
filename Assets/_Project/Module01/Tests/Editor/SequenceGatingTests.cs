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
            Assert.AreEqual("Module01.Vehicle.Ebike.Visibility", Load("SO_Seq_Module01_Ebike_Visibility")?.SequenceId);
        }

        [Test]
        public void Las_zonas_del_escooter_solo_corren_con_el_escooter()
        {
            TaskSequenceSO cockpit = Load("SO_Seq_Module01_Escooter_Cockpit");
            Assert.IsNotNull(cockpit, Missing);

            Module01Context scooter = new Module01Context();
            scooter.Set(Module01Context.VehicleKey, "escooter");
            Assert.IsTrue(cockpit.MatchesContext(scooter.Lookup, out _));

            Module01Context ebike = new Module01Context();
            ebike.Set(Module01Context.VehicleKey, "ebike");
            Assert.IsFalse(cockpit.MatchesContext(ebike.Lookup, out string unmet));
            StringAssert.Contains("vehicle", unmet);
        }

        [Test]
        public void El_escooter_tiene_las_tres_zonas_de_vehiculo()
        {
            foreach (string zone in new[] { "Cockpit", "Wheels", "Visibility" })
                Assert.IsNotNull(Load("SO_Seq_Module01_Escooter_" + zone),
                    $"Falta la secuencia {zone} del escooter. " + Missing);
        }

        /// <summary>
        /// <c>TaskSequenceRunner</c> indexa lo completado y el punto de reanudación por
        /// <c>SequenceId</c>. Dos vehículos con el mismo id compartirían ese estado: cerrar
        /// el cockpit de la bici daría por hecho el del patinete.
        /// </summary>
        [Test]
        public void Ningun_par_de_secuencias_comparte_SequenceId()
        {
            string[] assets =
            {
                "SO_Seq_Module01_Head", "SO_Seq_Module01_Clothing", "SO_Seq_Module01_Load",
                "SO_Seq_Module01_Ebike_Cockpit", "SO_Seq_Module01_Ebike_Wheels", "SO_Seq_Module01_Ebike_Visibility",
                "SO_Seq_Module01_Escooter_Cockpit", "SO_Seq_Module01_Escooter_Wheels", "SO_Seq_Module01_Escooter_Visibility"
            };

            System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>();
            foreach (string asset in assets)
            {
                TaskSequenceSO sequence = Load(asset);
                Assert.IsNotNull(sequence, $"Falta {asset}. " + Missing);
                Assert.IsTrue(seen.Add(sequence.SequenceId),
                    $"'{sequence.SequenceId}' esta repetido; {asset} necesita el suyo.");
            }
        }
    }
}
