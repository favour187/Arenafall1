// Unity Build Automation pre-export hook.
// In Unity Dashboard > Build Automation > Advanced settings, set Pre-export method name to:
// ArenaFall.Editor.ArenaFallCloudBuild.PreExport

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArenaFall.Editor
{
    public static class ArenaFallCloudBuild
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/01_Boot/Boot.unity",
            "Assets/Scenes/02_Login/Login.unity",
            "Assets/Scenes/03_MainMenu/MainMenu.unity",
            "Assets/Scenes/04_Profile/Profile.unity",
            "Assets/Scenes/05_Lobby/Lobby.unity",
            "Assets/Scenes/06_Customization/Customization.unity",
            "Assets/Scenes/07_Loadout/Loadout.unity",
            "Assets/Scenes/08_TrainingGround/TrainingGround.unity",
            "Assets/Scenes/09_Matchmaking/Matchmaking.unity",
            "Assets/Scenes/10_GameMap/GameMap.unity",
            "Assets/Scenes/11_ResultScreen/ResultScreen.unity",
            "Assets/Scenes/12_Replay/Replay.unity",
            "Assets/Scenes/13_Settings/Settings.unity",
            "Assets/Scenes/14_TestScene/TestScene.unity"
        };

        /// <summary>
        /// Called by Unity Build Automation immediately before it exports the Android player.
        /// This makes the cloud build self-contained: it registers every game scene without
        /// requiring a locally-installed Unity editor or EditorBuildSettings.asset.
        /// </summary>
        public static void PreExport()
        {
            var scenes = new EditorBuildSettingsScene[ScenePaths.Length];
            for (int i = 0; i < ScenePaths.Length; i++)
            {
                if (!File.Exists(ScenePaths[i]))
                    throw new FileNotFoundException("Required build scene was not found.", ScenePaths[i]);
                scenes[i] = new EditorBuildSettingsScene(ScenePaths[i], true);
            }

            EditorBuildSettings.scenes = scenes;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.arenagames.arenafall");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // This is a mobile landscape game. Lock Android to landscape rather than
            // allowing the device's portrait orientation to control the player window.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            ConfigureAndroidIcon();
            AssetDatabase.SaveAssets();
            Debug.Log("[ArenaFall Cloud Build] Registered 14 scenes and configured Android player settings, landscape orientation, and launcher icon.");
        }

        private static void ConfigureAndroidIcon()
        {
            const string iconPath = "Assets/Art/Branding/ArenaFallIcon.png";
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon == null)
            {
                Debug.LogWarning("[ArenaFall Cloud Build] Launcher icon could not be loaded: " + iconPath);
                return;
            }

            // Unity uses an array of Android density icon slots. Reuse this source icon
            // for every slot; Unity/Gradle creates the required Android resources.
            Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Android);
            if (icons == null || icons.Length == 0)
                icons = new Texture2D[6];

            for (int i = 0; i < icons.Length; i++)
                icons[i] = icon;

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
        }

    }
}
