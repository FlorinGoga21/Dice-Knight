using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Imports the original audio pack and assigns its manifest through Unity's asset APIs.</summary>
public static class AssignGeneratedAudio
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before assigning audio assets.");
        var manifest = JObject.Parse(File.ReadAllText("Assets/Audio/AudioManifest.json"));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        int imported = 0;
        foreach (JObject entry in (JArray)manifest["assets"])
        {
            string path = (string)entry["path"];
            bool music = (bool)entry["loop"];
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Could not import " + path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = 0.8f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = !music;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !music;
            importer.loadInBackground = music;
            importer.SaveAndReimport();
            imported++;
        }

        const string sharedPath = "Assets/Prefabs/GameAudio.prefab";
        var sharedRoot = PrefabUtility.LoadPrefabContents(sharedPath);
        int sharedAssignments = 0;
        try
        {
            var fields = new SerializedObject(sharedRoot.GetComponent<GameAudio>());
            foreach (string section in new[] { "music", "shared" })
                foreach (JObject entry in (JArray)manifest[section])
                {
                    fields.FindProperty((string)entry["field"]).objectReferenceValue = Clip((string)entry["path"]);
                    sharedAssignments++;
                }
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(sharedRoot, sharedPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(sharedRoot); }

        int characterAssignments = 0;
        foreach (JObject character in (JArray)manifest["characters"])
        {
            string path = (string)character["prefab"];
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var component = root.GetComponent<CharacterAudio>();
                if (component == null) throw new InvalidOperationException("Character Audio is missing from " + path);
                var fields = new SerializedObject(component);
                foreach (var entry in ((JObject)character["fields"]).Properties())
                {
                    var property = fields.FindProperty(entry.Name);
                    if (entry.Value is JArray clips)
                    {
                        property.arraySize = clips.Count;
                        for (int i = 0; i < clips.Count; i++)
                        {
                            property.GetArrayElementAtIndex(i).objectReferenceValue = Clip((string)clips[i]);
                            characterAssignments++;
                        }
                    }
                    else
                    {
                        property.objectReferenceValue = Clip((string)entry.Value);
                        characterAssignments++;
                    }
                }
                fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        return new { imported, musicAndSharedAssignments = sharedAssignments, characterAssignments,
            totalAssignments = sharedAssignments + characterAssignments };
    }

    private static AudioClip Clip(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) throw new InvalidOperationException("Missing AudioClip: " + path);
        return clip;
    }
}
