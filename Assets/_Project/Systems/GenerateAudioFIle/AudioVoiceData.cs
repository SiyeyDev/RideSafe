using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class Voice
{
    public string voice_id;
    public string name;
}

[Serializable]
public class VoicesResponse
{
    public List<Voice> voices;
}
[Serializable]
public class VoiceSettings
{
    public double stability = 0.75f;
    public double similarity_boost =0.85f;
    public double style = 0.1f;
    public bool use_speaker_boost = true;
    public double speed = 1;
}
[Serializable]
public class DataGenerated
{
    public string audio_base64;
    public Alignment alignment;
    public Alignment normalized_alignment;
}
[Serializable]
public class Alignment
{
    public List<string> characters;
    public List<double> character_start_times_seconds;
    public List<double> character_end_times_seconds;

    public Alignment(List<string> characters, List<double> character_start_times_seconds, List<double> character_end_times_seconds)
    {
        this.characters = characters;
        this.character_start_times_seconds = character_start_times_seconds;
        this.character_end_times_seconds = character_end_times_seconds;
    }
}