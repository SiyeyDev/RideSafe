#if UNITY_EDITOR
using Cachacos;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

public class TypingTextDisplayWindow : EditorWindow
{
    private static TypingTextDisplayWindow _window;
    private static string _displayedText = null;
    private static EditorCoroutine _coroutine;
    private static GUIStyle _typingStyle;
    public static bool Opened { get; private set; }

    private static float _maxWidth = 512;
    private static float _padding =16;
    private Vector2 _smoothVel;
    private float _smoothTime = 0.1f;
    private static Vector2 _startPosition;

    /// <summary>
    /// Esta ventana se crea con ShowPopup(): no tiene barra de titulo ni boton de cerrar, y como
    /// es un ScriptableObject sobrevive a los domain reloads (recompilar, entrar a Play). Los
    /// statics de abajo SI se resetean, asi que tras un reload la instancia queda huerfana: vacia,
    /// sin nadie que la cierre y sin forma de cerrarla a mano. Por eso se barren al cargar.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void CloseOrphans()
    {
        _displayedText = null;
        _coroutine = null;
        Opened = false;
        _window = null;
        EditorApplication.delayCall += () =>
        {
            foreach (TypingTextDisplayWindow orphan in Resources.FindObjectsOfTypeAll<TypingTextDisplayWindow>())
            {
                if (orphan != null)
                    orphan.Close();
            }
        };
    }

    public static void OpenTyping(AudioClip audioClip, string jsonData, Rect startRect)
    {
        CloseTyping();
        if (_typingStyle == null)
        {
            _typingStyle = EditorGUIUtils.GetButtonStyle(Color.white);
            _typingStyle.fontSize = 18;
            _typingStyle.wordWrap = true;
        }
        _window = CreateInstance<TypingTextDisplayWindow>();
        Opened = true;
        Alignment data = JsonUtility.FromJson<Alignment>(jsonData);
        EditorAudioUtils.PlayClip(audioClip);
        _coroutine = EditorCoroutineUtility.StartCoroutine(ShowTypingText.ShowTextEditor(SetText, data, endPlaying: EndText), _window);
        Vector2 textSize = _typingStyle.CalcSize(new GUIContent("Test"));
        float height = textSize.y ;
        _window.ShowPopup();
        _window.titleContent = new GUIContent("");
        _startPosition = startRect.position;
        _window.position = new Rect(startRect.position, Vector2.zero);
        _window.minSize = new Vector2(_padding * 2, height);
    }
    private void OnDestroy()
    {
        if (_coroutine != null)
            EditorCoroutineUtility.StopCoroutine(_coroutine);
        _coroutine = null;
        EditorAudioUtils.StopAllClips();
        if (_window == this)
            _window = null;
        _displayedText = null;
        Opened = false;
    }
    public static void CloseTyping()
    {
        if (_coroutine != null)
            EditorCoroutineUtility.StopCoroutine(_coroutine);
        _coroutine = null;
        _displayedText = null;
        EditorAudioUtils.StopAllClips();
        Opened = false;
        // Cierra cualquier instancia viva, no solo la que apunta el static: si hubo un domain
        // reload en medio, _window ya no apunta a la ventana que quedo abierta.
        foreach (TypingTextDisplayWindow open in Resources.FindObjectsOfTypeAll<TypingTextDisplayWindow>())
        {
            if (open != null)
                open.Close();
        }
        _window = null;
    }
    private static void SetText(string newText)
    {
        _displayedText = newText;
    }

    private static void EndText()
    {
        _coroutine = null;
        _displayedText = null;
        Opened = false;
        if (_window != null)
            _window.Close();
        _window = null;
    }

    private void OnGUI()
    {
        // Si ya no es la ventana activa (tipico despues de un domain reload) no hay nadie que la
        // cierre y ShowPopup no deja chrome para hacerlo a mano: se cierra sola.
        if (_window != this || !Opened || _typingStyle == null)
        {
            Close();
            return;
        }
        if (_displayedText == null)
            return;
        GUILayout.Label(WrapText(_displayedText), _typingStyle);
        GUILayout.Label(_displayedText, _typingStyle);
        UpdateWindowSize();
        Repaint();
    }

    private static string WrapText(string text)
    {
        StringBuilder wrappedText = new StringBuilder();
        string[] words = text.Split(' ');
        float currentLineWidth = 0;

        foreach (string word in words)
        {
            GUIContent content = new GUIContent(word + " ");
            Vector2 wordSize = _typingStyle.CalcSize(content);

            if (currentLineWidth + wordSize.x > _maxWidth)
            {
                wrappedText.AppendLine();
                currentLineWidth = 0;
            }

            wrappedText.Append(word + " ");
            currentLineWidth += wordSize.x;
        }

        return wrappedText.ToString();
    }

    private void UpdateWindowSize()
    {
        if (_displayedText == null)
            return;
        //string wrappedText = WrapText(_displayedText);
        string wrappedText = _displayedText;
        GUIContent content = new GUIContent(wrappedText);
        Vector2 textSize = _typingStyle.CalcSize(content);
        float newWidth = Mathf.Clamp(textSize.x + _padding * 2, _padding, _maxWidth);
        float newHeight = _typingStyle.CalcHeight(content,_maxWidth) + GetSyleExtraHeight(_typingStyle);
        Vector2 position = _startPosition;
        position.x -= newWidth / 2;
        Vector2 newPostion = Vector2.SmoothDamp(_window.position.position, position, ref _smoothVel, _smoothTime);
        Vector2 newSize = Vector2.SmoothDamp(_window.position.size, new Vector2(newWidth, newHeight), ref _smoothVel, _smoothTime);
        _window.position = new Rect(newPostion, newSize);
    }
    private float GetSyleExtraHeight(GUIStyle style)
    {
        float bounds = style.margin.top;
        bounds += style.margin.bottom;
        bounds += style.padding.bottom;
        bounds += style.padding.bottom;
        return bounds;
    }
}
#endif