using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class ShowTypingText
{
    private static readonly HashSet<char> punctuationMarks = new HashSet<char> { '.', ',', ':', ';', '!', '?' };
    public delegate void UpdateTextDelegate(string text);
    public static IEnumerator ShowTexTCharByChart(TMP_Text text, AudioSource audioSource, Alignment aligment, float offsetTime = 0)
    {
        int currentWordIndex = 0;
        StringBuilder typingText = new StringBuilder();
        string lastTypingText = "";
        float startTime = 0;
        int lastCharCount = 0;
        while (currentWordIndex < aligment.characters.Count)
        {
            float currentTime = audioSource.time - startTime + offsetTime;
            double startWordTime = aligment.character_start_times_seconds[currentWordIndex];
            double endWordTime = aligment.character_end_times_seconds[currentWordIndex];
            if (currentTime >= startWordTime)
            {
                if (ShoudlReset(lastTypingText))
                    typingText.Clear();
                string wordToAdd = aligment.characters[currentWordIndex];
                double duration = (endWordTime - startWordTime) / wordToAdd.Length;
                int charsToShow = Mathf.FloorToInt((float)((currentTime - startWordTime) / duration));
                if (charsToShow > wordToAdd.Length)
                    charsToShow = wordToAdd.Length;
                int newChars = charsToShow - lastCharCount;
                if (newChars > 0)
                {
                    typingText.Append(wordToAdd.Substring(lastCharCount, newChars));
                    lastCharCount = charsToShow;
                }
                if (currentTime >= endWordTime)
                {
                    currentWordIndex++;
                    lastCharCount = 0;
                    typingText.Append(" ");
                }
            }
            string currentTyped = typingText.ToString();
            if (text.text != currentTyped)
            {
                lastTypingText = typingText.ToString();
                text.SetText(lastTypingText);
            }
            yield return null;
        }
    }
    public static IEnumerator ShowTexTByAlligment(TMP_Text text, AudioSource audioSource, Alignment alignment, float offsetTime = 0, bool mergeAligmenUntilMark = false)
    {
        if (mergeAligmenUntilMark)
            alignment = MergedAlignment(alignment);
        int currentWordIndex = 0;
        StringBuilder typingText = new StringBuilder();
        string lastTypingText = "";
        while (currentWordIndex < alignment.characters.Count)
        {
            float currentTime = audioSource.time + offsetTime;
            double wordStartTime = alignment.character_start_times_seconds[currentWordIndex];
            if (currentTime >= wordStartTime)
            {
                if (ShoudlReset(typingText.ToString()))
                    typingText.Clear();
                typingText.Append($"{alignment.characters[currentWordIndex]} ");
                currentWordIndex++;
               
            }
            string currentTyped = typingText.ToString();
            if (text.text != currentTyped)
            {
                lastTypingText = typingText.ToString();
                text.SetText(lastTypingText);
            }
            yield return null;
        }
    }

    private static Alignment MergedAlignment(Alignment alignment)
    {
        List<string> characters = new List<string>();
        List<double> character_start_times_seconds = new List<double>();
        List<double> character_end_times_seconds = new List<double>();
        StringBuilder typingText = new StringBuilder();
        int currentWordIndex = 0;
        while (currentWordIndex < alignment.characters.Count)
        {
            int startIndex = currentWordIndex;
            while (!ShoudlReset(alignment.characters[currentWordIndex])) 
            {
                typingText.Append($" {alignment.characters[currentWordIndex]}");
                currentWordIndex++;
            }
            typingText.Append($" {alignment.characters[currentWordIndex]}");
            characters.Add(typingText.ToString());
            character_start_times_seconds.Add(alignment.character_start_times_seconds[startIndex]);
            character_end_times_seconds.Add(alignment.character_end_times_seconds[currentWordIndex]);
            typingText.Clear();
            currentWordIndex++;
        }
        return new Alignment(characters, character_start_times_seconds, character_end_times_seconds);
    }
#if UNITY_EDITOR
    public static IEnumerator ShowTextEditor(UpdateTextDelegate updateText, Alignment aligment, float offsetTime = 0, Action endPlaying = null)
    {
        int currentWordIndex = 0;
        StringBuilder typingText = new StringBuilder();
        string lastTypingText = "";
        float startTime = (float)EditorApplication.timeSinceStartup;
        int lastCharCount = 0;
        while (currentWordIndex < aligment.characters.Count)
        {
            float currentTime = (float)EditorApplication.timeSinceStartup - startTime + offsetTime;
            double startWordTime = aligment.character_start_times_seconds[currentWordIndex];
            double endWordTime = aligment.character_end_times_seconds[currentWordIndex];
            if (currentTime >= startWordTime)
            {
                if (ShoudlReset(lastTypingText))
                    typingText.Clear();
                string wordToAdd = aligment.characters[currentWordIndex];
                double duration = (endWordTime - startWordTime) / wordToAdd.Length;
                int charsToShow = Mathf.FloorToInt((float)((currentTime - startWordTime) / duration));
                if (charsToShow > wordToAdd.Length)
                    charsToShow = wordToAdd.Length;
                int newChars = charsToShow - lastCharCount;
                if (newChars > 0)
                {
                    typingText.Append(wordToAdd.Substring(lastCharCount, newChars));
                    lastCharCount = charsToShow;
                }
                if (currentTime >= endWordTime)
                {
                    currentWordIndex++;
                    lastCharCount = 0;
                    typingText.Append(" ");
                }
            }
            if (lastTypingText != typingText.ToString())
            {
                EditorUtils.ClearConsole();
                Debug.Log(typingText);
                lastTypingText = typingText.ToString();
                updateText(lastTypingText);

            }
            yield return null;
        }
        endPlaying?.Invoke();
    }
#endif
    private static bool ShoudlReset(string word)
    {
        if (string.IsNullOrEmpty(word))
            return false;
        foreach (char c in word)
        {
            if (punctuationMarks.Contains(c))
                return true;
        }
        return false;
    }
}

