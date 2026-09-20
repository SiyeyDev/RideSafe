#if UNITY_EDITOR
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.WSA;

public class GenerateAudio
{
    private readonly string _apiKey = "sk_6a403c9f83f8dd84c9c0610755576173ea7b2b5f4d65385c";

    private readonly string _folderRouth;
    private readonly bool _overrideAudio;
    private readonly bool _overrideTimeStamp;
    private readonly VoiceSettings _voiceSettings;

    public GenerateAudio(string folderRouth, bool overrideAudio, bool overrideTimeStamp, VoiceSettings voiceSettings)
    {
        _folderRouth = folderRouth;
        _overrideAudio = overrideAudio;
        _overrideTimeStamp = overrideTimeStamp;
        _voiceSettings = voiceSettings;
    }
    public IEnumerator Generate(string url, string text, string name)
    {
        string jsonData = JsonConvert.SerializeObject(new
        {
            text = text,
            model_id = "eleven_multilingual_v2",
            voice_settings = _voiceSettings 
        });
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST"))
        {
            SetRequest(jsonData, request);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}\n{request.downloadHandler.text}");
                yield break;
            }
            CreateFiles(name, request);
        }
    }

    private void SetRequest(string jsonData, UnityWebRequest request)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("xi-api-key", _apiKey);
        request.SetRequestHeader("Accept", "application/json");
    }

    private void CreateFiles(string name, UnityWebRequest request)
    {
        string jsonResponse = request.downloadHandler.text;
        try
        {
            DataGenerated data = JsonConvert.DeserializeObject<DataGenerated>(jsonResponse);
            string filePath = $"{_folderRouth}/{name}";
            if (_overrideAudio || !File.Exists($"{filePath}{Constants.k_audioFileExtension}"))
                ConvertBase64ToAudioClip(data.audio_base64, filePath);
            if (_overrideTimeStamp || !File.Exists($"{filePath}{Constants.k_jsonExtension}"))
                SaveJsonFile(ConvertTimeStampChartToWord(data.alignment), filePath);
        }
        catch (JsonException e)
        {
            Debug.LogError($"Error formatting JSON: {e.Message}\nRaw JSON: {jsonResponse}");
            Debug.Log($"Raw Response: {jsonResponse}");
        }
    }

    private void ConvertBase64ToAudioClip(string base64Audio, string filePath)
    {
        try
        {
            byte[] audioBytes = Convert.FromBase64String(base64Audio);
            HandleAudioData(audioBytes, filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error decoding base64: " + e.Message);
        }
    }
    private void HandleAudioData(byte[] audioBytes, string filePath)
    {
        try
        {
            File.WriteAllBytes($"{filePath}{Constants.k_audioFileExtension}", audioBytes);
            AssetDatabase.Refresh();
            Debug.Log("MP3 file saved to: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving MP3 file: " + e.Message);
        }
    }
    private void SaveJsonFile(Alignment wordAligment, string filePath)
    {
        try
        {
            string json = JsonConvert.SerializeObject(wordAligment, Formatting.Indented);
            string folderPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            File.WriteAllText($"{filePath}{Constants.k_jsonExtension}", json);
            AssetDatabase.Refresh();
            Debug.Log($"TimeStamp data saved to {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving TimeStamp data: {ex.Message}");
        }
    }
    private Alignment ConvertTimeStampChartToWord(Alignment aligment)
    {
        List<string> words = new List<string>();
        List<double> wordStartTimes = new List<double>();
        List<double> wordEndTimes = new List<double>();

        StringBuilder fullTextBuilder = new StringBuilder();
        foreach (string character in aligment.characters)
            fullTextBuilder.Append(character);
        string fullText = fullTextBuilder.ToString();
        string[] wordTokens = Regex.Split(fullText, @"\s+");
        int charIndex = 0;
        foreach (string word in wordTokens)
        {
            if (string.IsNullOrEmpty(word))
                continue;
            int wordStartIndex = charIndex;
            int wordEndIndex = charIndex + word.Length - 1;
            double wordStartTime = GetStartTime(aligment, wordStartIndex, wordEndIndex);
            double wordEndTime = GetEndTime(aligment, wordStartIndex, wordEndIndex);
            charIndex += word.Length + 1;
            if (wordStartTime == -1 || wordEndTime == -1)
                continue;
            words.Add(word);
            wordStartTimes.Add(wordStartTime);
            wordEndTimes.Add(wordEndTime);
        }
        return new Alignment(words, wordStartTimes, wordEndTimes);
    }
    private static double GetEndTime(Alignment aligment, int wordStartIndex, int wordEndIndex)
    {
        double wordEndTime = -1;
        for (int i = wordEndIndex; i >= wordStartIndex; i--)
        {
            if (i < aligment.character_end_times_seconds.Count && aligment.character_end_times_seconds[i] >= 0)
            {
                wordEndTime = aligment.character_end_times_seconds[i];
                break;
            }
        }
        return wordEndTime;
    }
    private static double GetStartTime(Alignment aligment, int wordStartIndex, int wordEndIndex)
    {
        double wordStartTime = -1;
        for (int i = wordStartIndex; i <= wordEndIndex; i++)
        {
            if (i < aligment.character_start_times_seconds.Count && aligment.character_start_times_seconds[i] >= 0)
            {
                wordStartTime = aligment.character_start_times_seconds[i];
                break;
            }
        }
        return wordStartTime;
    }
}
#endif