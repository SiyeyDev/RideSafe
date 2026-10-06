using System.Collections.Generic;
using RideSafe.UI;
using UnityEngine;
using UnityEngine.UI;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// Connects the generated PF_Module00_UI to the tutorial at runtime: task entities on its
    /// choices and buttons, panel flow, and the chosen language/location/vehicle.
    /// <para>
    /// Nothing is stored as a scene override on the prefab, because RideSafeUIPrefabBuilder
    /// regenerates it with new internal ids and such overrides would silently vanish. The
    /// binding finds elements by the panel ids and object names the builder writes. If one
    /// is missing it logs exactly which, and <see cref="FindMissing"/> lets a test catch that
    /// before the scene is ever run.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial/Module 00 Tutorial Binding")]
    public class Module00TutorialBinding : MonoBehaviour
    {
        public const string LanguagePanel = "Language";
        public const string LocationPanel = "Jurisdiction";
        public const string VehiclePanel = "Vehicle";
        public const string ComfortPanel = "Comfort";

        /// <summary>(panel, child object, entity id). Names come from UIModules.Module00.</summary>
        private static readonly (string Panel, string Child, string EntityId)[] Entities =
        {
            (LanguagePanel, "Options", "onboarding.language.options"),
            (LanguagePanel, "ContinueButton", "onboarding.language.continue"),
            (LocationPanel, "Options", "onboarding.location.options"),
            (LocationPanel, "ContinueButton", "onboarding.location.continue"),
            (VehiclePanel, "VehicleLabels", "onboarding.vehicle.options"),
            (VehiclePanel, "ConfirmButton", "onboarding.vehicle.confirm")
        };

        private const string LanguageBackButton = "BackButton";

        [Tooltip("Placed in front of the learner when the onboarding appears. Holds PF_Module00_UI and the instruction.")]
        [SerializeField] private HeadRelativePlacement _anchor;
        [SerializeField] private TutorialSession _session;

        private ModuleUI _module;
        private ChoicePanel _language;
        private ChoicePanel _location;
        private ChoicePanel _vehicle;

        protected virtual void Awake()
        {
            // Runs while the anchor is still inactive, so the entities added below only wake
            // up (and register) once they have their ids.
            _module = _anchor != null ? _anchor.GetComponentInChildren<ModuleUI>(true) : null;
            if (_module == null)
            {
                Debug.LogError("[Tutorial] Module00TutorialBinding found no ModuleUI (PF_Module00_UI) under its anchor.", this);
                return;
            }

            List<string> missing = FindMissing(_module);
            if (missing.Count > 0)
            {
                Debug.LogError("[Tutorial] PF_Module00_UI no longer matches the tutorial binding. Missing: " +
                               string.Join(", ", missing) + ". Was the prefab regenerated with new names?", this);
                return;
            }

            foreach (var entity in Entities)
                AddEntity(Find(_module, entity.Panel, entity.Child), entity.EntityId);

            _language = _module.Get<ChoicePanel>(LanguagePanel);
            _location = _module.Get<ChoicePanel>(LocationPanel);
            _vehicle = _module.Get<ChoicePanel>(VehiclePanel);

            // Language is the first screen after the gate: nothing to go back to.
            Find(_module, LanguagePanel, LanguageBackButton).gameObject.SetActive(false);

            _language.onSelectionChanged.AddListener(_session.SetLanguage);
            _language.onContinue.AddListener(ShowLocation);
            _location.onContinue.AddListener(_session.SetJurisdiction);
            _location.onContinue.AddListener(ShowVehicle);
            _location.onBack.AddListener(ShowLanguage);
            _vehicle.onContinue.AddListener(_session.SetVehicle);
            _vehicle.onContinue.AddListener(ShowComfort);
        }

        protected virtual void OnDestroy()
        {
            if (_language == null)
                return;
            _language.onSelectionChanged.RemoveListener(_session.SetLanguage);
            _language.onContinue.RemoveListener(ShowLocation);
            _location.onContinue.RemoveListener(_session.SetJurisdiction);
            _location.onContinue.RemoveListener(ShowVehicle);
            _location.onBack.RemoveListener(ShowLanguage);
            _vehicle.onContinue.RemoveListener(_session.SetVehicle);
            _vehicle.onContinue.RemoveListener(ShowComfort);
        }

        /// <summary>Brings the onboarding in front of the learner and opens Language. Wired from the gate stage.</summary>
        public void Show()
        {
            if (_module == null)
                return;
            _anchor.Place();
            _anchor.gameObject.SetActive(true);
            _module.Show(LanguagePanel);
        }

        /// <summary>Every panel and object this binding needs, as "Panel/Child", that the module lacks.</summary>
        public static List<string> FindMissing(ModuleUI module)
        {
            List<string> missing = new List<string>();
            foreach (string panel in new[] { LanguagePanel, LocationPanel, VehiclePanel, ComfortPanel })
            {
                if (module.Get<RectTransform>(panel) == null)
                    missing.Add(panel);
            }
            foreach (string panel in new[] { LanguagePanel, LocationPanel, VehiclePanel })
            {
                if (module.Get<RectTransform>(panel) != null && module.Get<ChoicePanel>(panel) == null)
                    missing.Add(panel + "/ChoicePanel");
            }
            foreach (var entity in Entities)
            {
                if (Find(module, entity.Panel, entity.Child) == null)
                    missing.Add(entity.Panel + "/" + entity.Child);
            }
            if (Find(module, LanguagePanel, LanguageBackButton) == null)
                missing.Add(LanguagePanel + "/" + LanguageBackButton);
            return missing;
        }

        private void ShowLanguage() => _module.Show(LanguagePanel);
        private void ShowLocation(string _) => _module.Show(LocationPanel);
        private void ShowVehicle(string _) => _module.Show(VehiclePanel);
        private void ShowComfort(string _) => _module.Show(ComfortPanel);

        private static void AddEntity(Transform target, string id)
        {
            AutoHandUITaskEntity entity = target.gameObject.AddComponent<AutoHandUITaskEntity>();
            entity.Configure(new EntityId(id), target.GetComponent<Toggle>(), target.GetComponent<ToggleGroup>(),
                             target.GetComponent<Button>());
            entity.EnsureRegistered();
        }

        private static Transform Find(ModuleUI module, string panel, string child)
        {
            RectTransform root = module.Get<RectTransform>(panel);
            if (root == null)
                return null;
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == child)
                    return candidate;
            }
            return null;
        }
    }
}
