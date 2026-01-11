using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Animations;

public class NPCSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup NPCs")]
    public static void SetupNPCs()
    {
        // Deselect everything to prevent Animator Window from trying to update dead assets
        Selection.activeObject = null;
        
        // Force simple refresh to ensure no stale references
        AssetDatabase.Refresh();

        // Sheriff and Thief (Single File: Model + Anim)
        CreateNPC("Sheriff", "Assets/NPC/Sheriff Anim/Breathing Idle.fbx", null, "Assets/NPC/material_0.png");
        CreateNPC("Thief", "Assets/NPC/Theif Anim/Idle.fbx", null, "Assets/NPC/material_0_Pbr_Diffuse.png");
        
        // TownPerson (Separate: Model + Anim)
        CreateNPC("TownPerson", "Assets/NPC/TownPerson.fbx", "Assets/NPC/TownPerson Anim/Breathing Idle.fbx", "Assets/NPC/material_0_Pbr_Diffuse 1.png");
    }

    static void CreateNPC(string npcName, string modelPath, string animPath, string texturePath)
    {
        // If animPath is null/empty, we assume the modelPath contains the animation
        string clipSourcePath = string.IsNullOrEmpty(animPath) ? modelPath : animPath;

        // 1. Ensure Directories Exist
        string prefabDir = "Assets/NPC/Prefabs";
        string controllerDir = "Assets/NPC/Controllers";
        string materialDir = "Assets/NPC/Materials"; // centralized materials

        if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
        if (!Directory.Exists(controllerDir)) Directory.CreateDirectory(controllerDir);
        if (!Directory.Exists(materialDir)) Directory.CreateDirectory(materialDir);

        // 2. Configure Import Settings (Force Loop Time)
        ModelImporter importer = AssetImporter.GetAtPath(clipSourcePath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"[NPCSetup] FAILED: Could not find Animation Source at {clipSourcePath}");
            return;
        }

        bool changed = false;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        
        // If imports are set to use specific clips, check them. 
        if (clips.Length > 0)
        {
            foreach (var clip in clips)
            {
                if (!clip.loopTime)
                {
                    clip.loopTime = true;
                    changed = true;
                }
            }
        }
        else
        {
             ModelImporterClipAnimation defaultClip = new ModelImporterClipAnimation();
             defaultClip.name = "Idle";
             defaultClip.loopTime = true;
             defaultClip.firstFrame = 0; 
             defaultClip.lastFrame = 0; // 0 means "all"
             
             clips = new ModelImporterClipAnimation[] { defaultClip };
             changed = true;
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log($"[NPCSetup] Enforced Loop Time on {clipSourcePath}");
        }

        // 3. Find the Animation Clip in the Asset
        AnimationClip idleClip = null;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(clipSourcePath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                idleClip = clip;
                break;
            }
        }

        if (idleClip == null)
        {
            Debug.LogError($"[NPCSetup] FAILED: No AnimationClip found in {clipSourcePath}");
            return;
        }

        // 4. Create Animator Controller
        string controllerPath = $"{controllerDir}/{npcName}IdleController.controller";
        
        // Prevent generic Unity "Graph" errors by deleting existing controller first
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }
        AssetDatabase.Refresh(); // Sync state

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var state = controller.AddMotion(idleClip); // Defaults to this state
        state.name = "Idle"; // Rename state to avoid "'.' is not allowed" error if clip has dots


        // 5. Instantiate the FBX
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelPrefab == null)
        {
             Debug.LogError($"[NPCSetup] Critical Error: Failed to load model prefab at {modelPath}");
             return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        
        // 5.4. Ensure destination prefab is cleaned up too
        string finalPrefabPath = $"{prefabDir}/{npcName}NPC.prefab";
        if (File.Exists(finalPrefabPath))
        {
            AssetDatabase.DeleteAsset(finalPrefabPath);
        }

        // 5.5 Assign Material if texture provided
        if (!string.IsNullOrEmpty(texturePath))
        {
            Debug.Log($"[NPCSetup] Attempting to load texture for {npcName} at: {texturePath}");
            
            // 5.5.1 Configure Texture Import Settings
            ConfigureTextureImporter(texturePath);

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null)
            {
                Debug.Log($"[NPCSetup] Texture loaded successfully: {tex.name} ({tex.width}x{tex.height})");

                // Create Material
                string matPath = $"{materialDir}/{npcName}_Mat.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                
                // Check if URP is active (naive check or just try URP shader first)
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                bool isURP = (shader != null);
                if (!isURP) 
                {
                    Debug.LogWarning("[NPCSetup] URP Shader not found, falling back to Standard.");
                    shader = Shader.Find("Standard");
                }
                else
                {
                    Debug.Log($"[NPCSetup] Using Shader: {shader.name}");
                }

                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, matPath);
                    Debug.Log($"[NPCSetup] Created new material at {matPath}");
                }
                else
                {
                    mat.shader = shader; // Reset shader just in case
                    EditorUtility.SetDirty(mat); // Ensure changes are saved
                    Debug.Log($"[NPCSetup] Updated existing material at {matPath}");
                }

                // Assign Texture
                if (isURP)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.color = Color.white; // Ensure tint is white
                }
                else
                {
                    mat.mainTexture = tex;
                }

                // Assign to ALL Renderers (SkinnedMeshRenderer)
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    foreach (var r in renderers)
                    {
                        r.sharedMaterial = mat;
                        Debug.Log($"[NPCSetup] Assigned material to Renderer: {r.name} ({r.GetType().Name})");
                    }
                    Debug.Log($"[NPCSetup] Assigned Material with {tex.name} to {renderers.Length} renderers on {npcName}");
                }
                else
                {
                     Debug.LogError($"[NPCSetup] ERROR: No Renderer found on {npcName} prefab instance! Material could not be assigned.");
                }
            }
            else
            {
                Debug.LogError($"[NPCSetup] FAILED to load texture at '{texturePath}'. Check if file exists and path is correct.");
            }
        }
        // 6. Setup Animator
        Animator animator = instance.GetComponent<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false; // Generally false for idle NPCs to prevent drifting

        // 6.1 Assign Avatar if loading separate files
        if (modelPath != clipSourcePath)
        {
             // Try to find an Avatar in the Model Path
             Avatar modelAvatar = null;
             Object[] modelAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
             foreach(var obj in modelAssets) { if(obj is Avatar av) { modelAvatar = av; break; } }
             
             if (modelAvatar != null)
             {
                 animator.avatar = modelAvatar;
                 Debug.Log($"[NPCSetup] Assigned Avatar {modelAvatar.name} to {npcName}");
             }
        }

        // 7. Save as new Prefab
        finalPrefabPath = $"{prefabDir}/{npcName}NPC.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, finalPrefabPath);
        
        // Cleanup
        DestroyImmediate(instance);

        Debug.Log($"[NPCSetup] SUCCESS: Created self-contained {npcName} Prefab at {finalPrefabPath}");
    }

    static void ConfigureTextureImporter(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;

            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }

            if (!importer.sRGBTexture)
            {
                importer.sRGBTexture = true;
                changed = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.None)
            {
                importer.alphaSource = TextureImporterAlphaSource.None;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[NPCSetup] Updated Texture Import Settings for {texturePath} (Type: Default, sRGB: On, Alpha: None)");
            }
        }
    }
}
