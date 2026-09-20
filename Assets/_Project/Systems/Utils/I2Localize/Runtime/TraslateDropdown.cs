using I2.Loc;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TraslateDropdown : MonoBehaviour
{
    [SerializeField] private List<Sprite> flags = new List<Sprite>();
    public Image referenceImg;
    [SerializeField] private string currentLanguage;
    private TMP_Dropdown dropdown;
    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null)
            return;


        currentLanguage = LocalizationManager.CurrentLanguage;
        if (LocalizationManager.Sources.Count == 0) LocalizationManager.UpdateSources();
        var languages = LocalizationManager.GetAllLanguages();


        // Fill the dropdown elements
        dropdown.ClearOptions();
        dropdown.AddOptions(languages);
        dropdown.value = languages.IndexOf(currentLanguage);

        //SearchFlags();


        dropdown.onValueChanged.RemoveListener(OnValueChanged);
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    void OnValueChanged(int index)
    {
        var dropdown = GetComponent<TMP_Dropdown>();
        if (index < 0)
        {
            index = 0;
            dropdown.value = index;
        }
        currentLanguage = LocalizationManager.CurrentLanguage = dropdown.options[index].text;
        string code = LocalizationManager.GetLanguageCode(currentLanguage);
       // EndPoint.EndPointManager.Instance.ChangeLanguage(JsonConvert.SerializeObject(new LanguageBody() { language = code }), (error, helper) => { HabytatDebug.Log("Helper" + helper.Text); });

    }

    public void SearchFlags()
    {
        List<string> codes = LocalizationManager.GetAllLanguagesCode();
        for (int i = 0; i < codes.Count; i++)
        {
            //Sprite spriteFlag = LocalizationManagerExtension.GetCountryFlagSprite(codes[i]);
            //flags.Add(spriteFlag);
        }
        for (int j = 0; j < flags.Count; j++)
        {
            if (flags[j].name == LocalizationManager.GetLanguageCode(currentLanguage))
            {
                referenceImg.sprite = flags[j];
            }
        }
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            for (int j = 0; j < flags.Count; j++)
            {
                if (LocalizationManager.GetLanguageCode(dropdown.options[i].text) == flags[j].name)
                {
                    dropdown.options[i].image = flags[j];
                }
            }
        }
    }
}
