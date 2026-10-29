using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Las paradas de EPP con la colocación del artista (PO, 2026-10-27): los EPP van donde los
    /// dejó él en <c>Garage.unity</c>, así que ya no se reparten en fila. Quedan tres cuentas:
    /// dónde cabe el signo «!» de lo que aún no tiene modelo sin pisar lo que el artista puso,
    /// cuándo algo queda tapado, y desde dónde se mira cada mueble: de frente y centrado.
    /// </summary>
    public class ArtStationLayoutTests
    {
        /// <summary>Una tapa de 3 m de largo en X, a 1 m de alto.</summary>
        private static Bounds Mostrador() =>
            new Bounds(new Vector3(0f, .5f, -4.5f), new Vector3(3f, 1f, .6f));

        private static readonly Vector3 Signo = new Vector3(.15f, .15f, .15f);

        /// <summary>Una planta de garaje de 9 x 11 m, de suelo a techo.</summary>
        private static Bounds Planta() =>
            new Bounds(new Vector3(0f, 1.3f, .5f), new Vector3(9f, 2.6f, 11f));

        private static Bounds Caja(float x, float z) =>
            new Bounds(new Vector3(x, 1.1f, z), new Vector3(.3f, .2f, .3f));

        private const float Ojos = 1.45f, Burbuja = 1.9f;

        [Test]
        public void Un_signo_no_pisa_lo_que_el_artista_ya_puso_en_la_mesa()
        {
            Bounds casco = new Bounds(new Vector3(0f, 1.15f, -4.5f), new Vector3(.4f, .3f, .3f));

            List<Vector3> sitios = StationLayout.FreeSpots(
                Mostrador(), 1f, Signo, 1, new List<Bounds> { casco }, new Vector3(0f, 1f, -4.5f));

            Assert.AreEqual(1, sitios.Count);
            Assert.IsFalse(new Bounds(sitios[0], Signo).Intersects(casco), "el signo cae encima del casco");
        }

        [Test]
        public void El_signo_se_apoya_en_la_tapa_y_dentro_del_mueble()
        {
            List<Vector3> sitios = StationLayout.FreeSpots(
                Mostrador(), 1f, Signo, 1, new List<Bounds>(), new Vector3(0f, 1f, -4.5f));

            Assert.AreEqual(1f + Signo.y * .5f, sitios[0].y, .0001f, "no descansa sobre la tapa");
            Bounds planta = Mostrador();
            Assert.IsTrue(planta.min.x <= sitios[0].x - Signo.x * .5f && sitios[0].x + Signo.x * .5f <= planta.max.x);
            Assert.IsTrue(planta.min.z <= sitios[0].z - Signo.z * .5f && sitios[0].z + Signo.z * .5f <= planta.max.z);
        }

        [Test]
        public void Los_signos_quedan_lo_mas_cerca_posible_de_su_grupo()
        {
            List<Vector3> sitios = StationLayout.FreeSpots(
                Mostrador(), 1f, Signo, 1, new List<Bounds>(), new Vector3(1f, 1f, -4.5f));

            Assert.AreEqual(1f, sitios[0].x, .06f);
        }

        [Test]
        public void Varios_signos_no_se_pisan_entre_ellos()
        {
            List<Vector3> sitios = StationLayout.FreeSpots(
                Mostrador(), 1f, Signo, 4, new List<Bounds>(), new Vector3(0f, 1f, -4.5f));

            Assert.AreEqual(4, sitios.Count);
            for (int i = 0; i < sitios.Count; i++)
                for (int j = i + 1; j < sitios.Count; j++)
                    Assert.IsFalse(new Bounds(sitios[i], Signo).Intersects(new Bounds(sitios[j], Signo)),
                        $"los signos {i} y {j} se pisan");
        }

        /// <summary>
        /// Para buscar un hueco que cumpla algo más (que se vea desde el ancla) hacen falta todos
        /// los sitios libres, no solo los que caben a la vez: en «otra mesa» quedaban 12.
        /// </summary>
        [Test]
        public void Los_candidatos_son_todos_los_sitios_libres_aunque_se_pisen_entre_ellos()
        {
            List<Vector3> todos = StationLayout.FreeCandidates(
                Mostrador(), 1f, Signo, new List<Bounds>(), new Vector3(0f, 1f, -4.5f));
            List<Vector3> aLaVez = StationLayout.FreeSpots(
                Mostrador(), 1f, Signo, int.MaxValue, new List<Bounds>(), new Vector3(0f, 1f, -4.5f));

            Assert.Greater(todos.Count, 2 * aLaVez.Count);
            Assert.AreEqual(new Vector3(0f, 1f, -4.5f).x, todos[0].x, .06f, "el primero es el más cercano");
        }

        /// <summary>
        /// El armario alto del rincón viene en la misma malla que el mostrador: los signos solo
        /// pueden ir en el tramo que es tapa.
        /// </summary>
        [Test]
        public void Los_signos_no_salen_del_tramo_de_tapa_declarado()
        {
            Bounds mueble = new Bounds(new Vector3(-2.47f, 1.3f, -4.47f), new Vector3(3.34f, 2.6f, .66f));   // -4,14 a -0,80
            Bounds tapa = new Surface("cabinet (1)", 1.08f, usableFrom: -2.89f).Clip(mueble);

            List<Vector3> sitios = StationLayout.FreeSpots(tapa, 1.08f, Signo, int.MaxValue,
                new List<Bounds>(), new Vector3(-4f, 1f, -4.47f));

            Assert.IsNotEmpty(sitios);
            foreach (Vector3 s in sitios)
                Assert.GreaterOrEqual(s.x - Signo.x * .5f, -2.89f, $"signo delante del armario alto en x={s.x:F2}");
        }

        [Test]
        public void En_una_mesa_llena_no_se_inventa_sitio()
        {
            Bounds todo = new Bounds(new Vector3(0f, 1.2f, -4.5f), new Vector3(3f, .4f, .6f));

            List<Vector3> sitios = StationLayout.FreeSpots(
                Mostrador(), 1f, Signo, 2, new List<Bounds> { todo }, Vector3.zero);

            Assert.AreEqual(0, sitios.Count);
        }

        /// <summary>La mesa del centro de la pared derecha: 0,7 de fondo y 2,9 de largo.</summary>
        private static Bounds MesaDerecha(float largo = 2.9f) =>
            new Bounds(new Vector3(4.2f, 1.2f, .8f), new Vector3(.7f, 2.4f, largo));

        [Test]
        public void Cada_mueble_se_mira_de_frente_y_centrado()
        {
            Assert.IsTrue(StationLayout.TryFrontView(MesaDerecha(), new List<Bounds>(), new List<Bounds>(),
                Planta(), Burbuja, Ojos, out Pose pose));

            Vector3 forward = pose.rotation * Vector3.forward;
            Assert.AreEqual(1f, forward.x, .001f, "no mira de frente a la pared");
            Assert.AreEqual(.8f, pose.position.z, .001f, "no está centrado en el mueble");
            Assert.Less(pose.position.x, MesaDerecha().min.x, "no está delante del mueble");
            Assert.AreEqual(Ojos, pose.position.y, .0001f);
        }

        [Test]
        public void El_mostrador_del_fondo_se_mira_desde_el_garaje()
        {
            Bounds mostrador = new Bounds(new Vector3(-1.3f, 1.3f, -4.47f), new Vector3(5.7f, 2.6f, .66f));

            Assert.IsTrue(StationLayout.TryFrontView(mostrador, new List<Bounds>(), new List<Bounds>(),
                Planta(), Burbuja, Ojos, out Pose pose));

            Assert.AreEqual(-1f, (pose.rotation * Vector3.forward).z, .001f, "no mira a la pared del fondo");
            Assert.AreEqual(-1.3f, pose.position.x, .001f);
        }

        [Test]
        public void Un_mueble_mas_largo_se_mira_desde_mas_lejos_para_que_entre_entero()
        {
            StationLayout.TryFrontView(MesaDerecha(2f), new List<Bounds>(), new List<Bounds>(), Planta(), 0f, Ojos, out Pose corta);
            StationLayout.TryFrontView(MesaDerecha(4f), new List<Bounds>(), new List<Bounds>(), Planta(), 0f, Ojos, out Pose larga);

            Assert.Less(larga.position.x, corta.position.x);
            float distancia = MesaDerecha(4f).min.x - larga.position.x;
            Assert.GreaterOrEqual(distancia * Module01Stations.HalfWidthPerMeter, 2f, "el mueble no entra entero");
        }

        [Test]
        public void Ningun_elemento_queda_dentro_de_la_burbuja()
        {
            Bounds mochila = new Bounds(new Vector3(3.4f, .35f, .8f), new Vector3(.7f, .7f, .7f));   // en el suelo, delante

            StationLayout.TryFrontView(MesaDerecha(), new List<Bounds> { mochila }, new List<Bounds>(),
                Planta(), Burbuja, Ojos, out Pose pose);

            Assert.GreaterOrEqual(Vector3.Distance(pose.position, mochila.ClosestPoint(pose.position)), Burbuja - .001f);
        }

        [Test]
        public void El_aprendiz_no_queda_dentro_de_la_bici()
        {
            Bounds bici = new Bounds(new Vector3(2.5f, .55f, .8f), new Vector3(1f, 1.1f, 2f));

            Assert.IsTrue(StationLayout.TryFrontView(MesaDerecha(), new List<Bounds>(), new List<Bounds> { bici },
                Planta(), Burbuja, Ojos, out Pose pose));

            Bounds cuerpo = new Bounds(new Vector3(pose.position.x, .9f, pose.position.z), new Vector3(.6f, 1.8f, .6f));
            Assert.IsFalse(cuerpo.Intersects(bici));
        }

        /// <summary>
        /// Un tabique estrecho a medio camino deja los costados a 7,5 mm de su borde: sin
        /// holgura contaría como visto, y en cuanto el aprendiz mueve la cabeza deja de verse.
        /// </summary>
        [Test]
        public void Lo_que_pasa_rozando_otro_objeto_cuenta_como_tapado()
        {
            Vector3 ojo = new Vector3(0f, 1.1f, 0f);
            Bounds luz = new Bounds(new Vector3(4f, 1.1f, 0f), new Vector3(.3f, .3f, .3f));
            Bounds tabique = new Bounds(new Vector3(2f, 1.1f, 0f), new Vector3(.3f, .3f, .09f));

            Assert.AreEqual(1, StationLayout.Hidden(ojo, new List<Bounds> { luz }, new List<Bounds> { tabique }));
        }

        /// <summary>Basta con ver un trozo para poder pulsarlo: tapado solo es tapado entero.</summary>
        [Test]
        public void Si_se_ve_un_trozo_no_esta_tapado()
        {
            Vector3 ojo = new Vector3(0f, 1.1f, 0f);
            Bounds luz = new Bounds(new Vector3(4f, 1.1f, 0f), new Vector3(.3f, .3f, .3f));
            Bounds gafas = new Bounds(new Vector3(2f, 1.1f, .1f), new Vector3(.3f, .3f, .2f));   // tapa la mitad

            Assert.AreEqual(0, StationLayout.Hidden(ojo, new List<Bounds> { luz }, new List<Bounds> { gafas }));
        }

        [Test]
        public void Los_puntos_de_un_objeto_caen_dentro_de_el()
        {
            Bounds luz = new Bounds(new Vector3(4f, 1.1f, 0f), new Vector3(.3f, .3f, .3f));

            foreach (Vector3 p in StationLayout.SamplePoints(luz))
                Assert.IsTrue(luz.Contains(p), p.ToString("F3"));
        }

        [Test]
        public void Sin_sitio_libre_no_hay_punto_de_vista()
        {
            Bounds cuarto = new Bounds(new Vector3(4f, 1.3f, 0f), new Vector3(1f, 2.6f, 1f));

            Assert.IsFalse(StationLayout.TryFrontView(MesaDerecha(), new List<Bounds> { Caja(4.1f, 0f) },
                new List<Bounds>(), cuarto, Burbuja, Ojos, out _));
        }
    }
}
