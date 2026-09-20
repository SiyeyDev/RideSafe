#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FolderUtilities
{
    public static List<T> GetAssetWith<T>(string folderExtension, string filter = "") where T : Object
    {
        List<T> assetsWith = new List<T>();
        string[] folder = new string[1] { folderExtension };
        string[] guids = AssetDatabase.FindAssets(filter, folder);
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            assetsWith.Add(AssetDatabase.LoadAssetAtPath<T>(assetPath));
        }
        return assetsWith;
    }

    public static void SaveFile(string fileName, string data, string folderPath, string key, string extension = ".json", bool createInSameFolder = false)
    {
        string filePath = GetPathByFolderPath(fileName, folderPath, key, extension, createInSameFolder);
        if (!Directory.Exists(GetPathByFolderPath(fileName, folderPath, key, extension, createInSameFolder, false)))
            Directory.CreateDirectory(GetPathByFolderPath(fileName, folderPath, key, extension, createInSameFolder, false));
        Debug.Log(filePath);
        File.WriteAllText(filePath, data);
        AssetDatabase.Refresh();
    }
    public static string ReadFile<T>(this T caller, string folderPath, string key, string extension = ".json", bool createInSameFolder = false)
                                    => ReadFile(caller.GetType().Name, folderPath, key, extension, createInSameFolder);
    public static string ReadFile(string fileName, string folderPath, string key, string extension = ".json", bool createInSameFolder = false)
    {
        string filePath = GetPathByFolderPath(fileName, folderPath, key, extension, createInSameFolder);
        if (!FileExist(fileName, folderPath, key, createInSameFolder))
            return string.Empty;
        return File.ReadAllText(filePath);
    }

    public static void DeleteFile<T>(this T caller, string folderPath, string key, string extension = ".json", bool createInSameFolder = false)
    {
        string filePath = caller.GetPathByFoldePath(folderPath, key, extension, createInSameFolder);
        if (!caller.FileExist(folderPath, key, createInSameFolder))
            return;
        File.Delete(filePath);
    }
    public static bool FileExist(string fileName, string folderPath, string extension = ".json", bool createInSameFolder = false)
     => File.Exists(GetPathByFolderPath(fileName, folderPath, fileName, extension, createInSameFolder));
    public static bool FileExist<T>(this T caller, string folderPath, string key, bool createInSameFolder = false)
                                    => FileExist(caller.GetType().Name, folderPath, key, createInSameFolder);
    public static bool FileExist(string fileName, string folderPath, string key, string extension = ".json", bool createInSameFolder = false)
    {
        bool exits = File.Exists(GetPathByFolderPath(fileName, folderPath, key, extension, createInSameFolder));
        return exits;
    }
    /// <summary>
    /// Finds the path of an asset based on its filename, type, extension, and parent path.
    /// </summary>
    /// <param name="fileName">The filename of the asset (without extension).</param>
    /// <param name="type">The asset type (e.g., "Texture2D", "AudioClip"). Optional.</param>
    /// <param name="extension">The file extension (e.g., ".png", ".wav"). Optional.</param>
    /// <param name="parentPath">The parent path to search within. Optional.</param>
    /// <returns>The path of the found asset, or null if no asset is found.</returns>
    public static string GetPath(string fileName, string type = "", string extension = "", string parentPath = "")
    {
        string searchGuide = fileName;
        if (type != "")
            searchGuide = $"t:{type} {fileName}";
        string[] guids = AssetDatabase.FindAssets(searchGuide);
        if (guids.Length == 0)
            return null;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (parentPath != "" && !path.Contains(parentPath))
                continue;
            if (extension == "")
                return path;
            if (Path.GetExtension(path) == extension)
                return path;
        }
        return null;
    }
    public static string GetPathByFoldePath<T>(this T caller, string folderPath, string key, string extension = ".json", bool createInSameFolder = false, bool withKey = true)
                                   => GetPathByFolderPath(caller.GetType().Name, folderPath, key, extension, createInSameFolder, withKey);

    public static string GetPathByFolderPath(string fileName, string folderPath, string key, string extension = ".json", bool createInSameFolder = false, bool withKey = true)
    {
        string basePath = Application.dataPath;
        if (createInSameFolder)
        {
            string[] assetsPaths = AssetDatabase.FindAssets(fileName);
            string assetPath = AssetDatabase.GUIDToAssetPath(assetsPaths[0]);
            basePath = Path.GetDirectoryName(assetPath);
        }
        if (!withKey)
            return Path.Combine(basePath, folderPath);
        string sanitizedKey = SanitizeFileName(key);
        return Path.Combine(basePath, folderPath, $"{sanitizedKey}{extension}");
    }
    private static string SanitizeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
            fileName = fileName.Replace(c, '_');
        return fileName;
    }
    public static string ExtractJsonValue(string json, string key)
    {
        int startIdx = json.IndexOf($"\"{key}\":") + key.Length + 3;
        char firstChar = json[startIdx];
        int endIdx = 0;
        if (firstChar == '{' || firstChar == '[')
        {
            endIdx = FindClosingBrace(json, startIdx);
            return json.Substring(startIdx, endIdx - startIdx);
        }

        endIdx = json.IndexOf(',', startIdx);
        if (endIdx == -1)
            endIdx = json.IndexOf('}', startIdx);
        return json.Substring(startIdx, endIdx - startIdx);
    }
    private static int FindClosingBrace(string json, int startIdx)
    {
        int braceCount = 0;
        for (int i = startIdx; i < json.Length; i++)
        {
            if (json[i] == '{' || json[i] == '[')
                braceCount++;
            else if (json[i] == '}' || json[i] == ']')
                braceCount--;
            if (braceCount == 0)
                return i + 1;
        }
        return json.Length;
    }
}
#endif