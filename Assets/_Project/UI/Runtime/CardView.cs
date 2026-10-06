using TMPro;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Any titled block: instruction panels, step card, beat card, hold prompt, subtitle
    /// strip, completion chip. Every slot is optional; unset slots are ignored.
    /// </summary>
    [DisallowMultipleComponent]
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _counter;
        [SerializeField] private string _counterFormat = "Step {0} of {1}";
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private ProgressView _progress;

        public void SetContent(string title, string body = null)
        {
            if (_title != null)
                _title.text = title;
            if (_body != null)
            {
                _body.text = body ?? string.Empty;
                _body.gameObject.SetActive(!string.IsNullOrEmpty(body));
            }
        }

        /// <summary>1-based counter ("Beat 2 of 4"); also advances a segmented progress.</summary>
        public void SetCounter(int index, int total)
        {
            if (_counter != null)
                _counter.text = string.Format(_counterFormat, index, total);
            if (_progress != null)
                _progress.SetStep(index);
        }

        /// <summary>0..1 for bar progress (e.g. brake hold).</summary>
        public void SetProgress(float normalized)
        {
            if (_progress != null)
                _progress.SetValue(normalized);
        }

        public void Show(string title = null)
        {
            if (title != null)
                SetContent(title, _body != null ? _body.text : null);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
