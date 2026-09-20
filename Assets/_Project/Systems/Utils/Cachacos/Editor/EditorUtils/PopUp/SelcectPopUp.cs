
#if UNITY_EDITOR
namespace Cachacos
{
    public class SelcectPopUp : BasePopUp<StringWrapper>
    {
        protected override bool CompareSelection(string option) => selection.value == option;
        protected override void ClickButton(string option)
        {
            selection.value = option;
            Close();
        }

    }
    public class StringWrapper
    {
        public string value;
        public StringWrapper(string value) => this.value = value;
    }
}
#endif