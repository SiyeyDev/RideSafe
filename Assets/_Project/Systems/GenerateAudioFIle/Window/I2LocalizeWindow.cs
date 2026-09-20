#if UNITY_EDITOR
using I2.Loc;
using Cachacos;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;

public class I2LocalizeWindow : BaseWindow
{
    private readonly string[] _languages;
    private StringWrapper _language = new StringWrapper("");
    public string Language => _language.value;
    private readonly string[] _categories;
    private StringWrapper _lastCategory = new StringWrapper("");
    public string Category { get; private set; }
    private string[] _terms;
    private HashSet<string> _selectedTerms = new HashSet<string>();
    public HashSet<string> SelectedTerms { get; private set; }
    private HashSet<string> _lastSelectedTerms;
    private Button _languageBtn;
    private Button _categoriesBtn;
    private Button _termBtn;
    private ScrollView _scrollView;
    public I2LocalizeWindow() : base()
    {
        _languages = LocalizationManager.GetAllLanguages().ToArray();
        _language = new StringWrapper(LocalizationManager.CurrentLanguage);
        _categories = LocalizationManager.GetCategories().ToArray();
        _lastCategory.value = _categories[0];
        _selectedTerms = new HashSet<string>();
        _lastSelectedTerms = new HashSet<string>();
    }
    #region BaseWindow Methods
    public override void Show()
    {
        _languageBtn.text = _language.value;
        _categoriesBtn.text = _lastCategory.value;
        _termBtn.text = "Select Terms";
        if (Category != _lastCategory.value)
        {
            Category = _lastCategory.value;
            _terms = LocalizationManager.GetTermsList(Category).ToArray();
            _selectedTerms.Clear();
        }
        if (_selectedTerms.Count == 0)
        {
            _scrollView.style.display = DisplayStyle.None;
            return;
        }
        SelectedTerms = _terms.Where(term => _selectedTerms.Contains(term)).ToHashSet();
        if (_lastSelectedTerms.SetEquals(_selectedTerms))
            return;
        _scrollView.style.display = DisplayStyle.Flex;
        _scrollView.Clear();
        foreach (string term in SelectedTerms)
        {
            Label label = CreateLabel(term);
            if (!_scrollView.Contains(label))
                _scrollView.Add(label);
        }
        _lastSelectedTerms = new HashSet<string>(_selectedTerms);
    }
    public override void CreateGUI(EditorWindow window, VisualElement root, string buttonName)
    {
        base.CreateGUI(window,root, buttonName);
        _languageBtn = AddButton("LanguageBtn", SelectLanguage);
        _categoriesBtn = AddButton("CategoriesBtn", Categories);
        _termBtn = AddButton("TermsBtn", SelectTerms);
        _scrollView = root.Q<ScrollView>("TermScrollView");
        _scrollView.style.display = DisplayStyle.None;
    }
    #endregion
    public Dictionary<string, string> GetTexts()
    {
        Dictionary<string, string> texts = new Dictionary<string, string>();
        string category = Category;
        foreach (string term in SelectedTerms)
        {
            string text = LocalizationManager.GetTermTranslation(term, overrideLanguage: Language);
            texts.Add(term.Replace($"{category}/", ""), text);
        }
        return texts;
    }
    private void SelectLanguage()
    {
        SelcectPopUp.Open<SelcectPopUp>(GetRect(_languageBtn), _languages, ref _language, _languageBtn.resolvedStyle.color);
    }
    private void Categories()
    {
        SelcectPopUp.Open<SelcectPopUp>(GetRect(_categoriesBtn), _categories, ref _lastCategory, _categoriesBtn.resolvedStyle.color);
    }
    private void SelectTerms()
    {
        SelectablePopUp.Open<SelectablePopUp>(GetRect(_termBtn), _terms, ref _selectedTerms, _termBtn.resolvedStyle.color, $"{Category}/");
    }
    private Label CreateLabel(string term)
    {
        Label label = new Label();
        label.text = term.Replace($"{Category}/", "");
        label.style.fontSize = 12f;
        label.style.color = _termBtn.resolvedStyle.color * 0.5f;
        label.style.display = DisplayStyle.Flex;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        return label;
    }
}
#endif