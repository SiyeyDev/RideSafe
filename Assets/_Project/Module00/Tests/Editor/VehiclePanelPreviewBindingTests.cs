using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Module00.Tests
{
    public class VehiclePanelPreviewBindingTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private ModuleUI _module;
        private ChoicePanel _vehiclePanel;
        private VehiclePreviewController _preview;
        private VehiclePanelPreviewBinding _binding;

        [SetUp]
        public void SetUp()
        {
            GameObject canvas = Own(new GameObject("PF_Module00_UI", typeof(Canvas)));
            _module = canvas.AddComponent<ModuleUI>();
            GameObject vehicle = new GameObject("Vehicle", typeof(RectTransform));
            vehicle.transform.SetParent(canvas.transform, false);
            _vehiclePanel = vehicle.AddComponent<ChoicePanel>();
            Set(_module, "_panels", new List<ModuleUI.Panel> { new ModuleUI.Panel { Id = "Vehicle", Root = vehicle } });

            Transform ebike = Own(new GameObject("ebike")).transform;
            _preview = Own(new GameObject("Preview")).AddComponent<VehiclePreviewController>();
            Set(_preview, "_vehicles", new List<VehiclePreviewController.Vehicle>
            {
                new VehiclePreviewController.Vehicle("ebike", ebike, null)
            });
            _preview.Initialize();

            _binding = Own(new GameObject("Binding")).AddComponent<VehiclePanelPreviewBinding>();
            Set(_binding, "_module", _module);
            Set(_binding, "_preview", _preview);
            _binding.Subscribe();
        }

        [TearDown]
        public void TearDown()
        {
            _binding.Unsubscribe();
            foreach (Object owned in _owned)
                Object.DestroyImmediate(owned);
            _owned.Clear();
        }

        [Test]
        public void ChoosingOnTheVehiclePanel_PresentsThatVehicle()
        {
            _vehiclePanel.onSelectionChanged.Invoke("ebike");
            Assert.AreEqual("ebike", _preview.SelectedId);
        }

        [Test]
        public void LeavingTheVehicleScreen_EndsThePresentation()
        {
            _vehiclePanel.onSelectionChanged.Invoke("ebike");
            _module.onPanelShown.Invoke("Comfort");
            Assert.IsNull(_preview.SelectedId);
        }

        [Test]
        public void ShowingTheVehicleScreenAgain_KeepsTheSelection()
        {
            _vehiclePanel.onSelectionChanged.Invoke("ebike");
            _module.onPanelShown.Invoke("Vehicle");
            Assert.AreEqual("ebike", _preview.SelectedId);
        }

        [Test]
        public void Unsubscribe_StopsForwarding()
        {
            _binding.Unsubscribe();
            _vehiclePanel.onSelectionChanged.Invoke("ebike");
            Assert.IsNull(_preview.SelectedId);
        }

        private GameObject Own(GameObject go)
        {
            _owned.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, field);
            info.SetValue(target, value);
        }
    }
}
