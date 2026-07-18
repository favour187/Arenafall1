# ArenaFall — One-button Unity Build Automation APK

You do **not** need Unity, Unity Hub, the Android SDK, or a computer capable of running Unity. Unity Build Automation runs Unity and Android build tools in the cloud.

## Before you begin

This repository must contain `Assets/Editor/ArenaFallCloudBuild.cs`. It registers the project's 14 scenes automatically before Unity exports the Android player. Commit and push that file to GitHub before creating the build configuration.

## One-time Dashboard setup

1. Create or sign in to a Unity account at <https://id.unity.com>.
2. Open <https://cloud.unity.com/> and create a **Personal** organization if Unity asks.
3. Create a new Unity project, named for example `ArenaFall`.
4. In that project select **DevOps** > **Build automation** > **Configurations**.
5. Click **Get started** and connect GitHub. Authorize Unity to access the `favour187/Arenafall` repository.
6. Select **Target setup** (not Quick target setup), then choose **Android**.
7. Use these basic settings:
   - **Target name:** `ArenaFall-APK`
   - **Branch:** `main`
   - **Project subfolder path:** leave empty — `Assets` and `ProjectSettings` are at the repository root.
   - **Unity version:** enable **Auto detect Unity version**. The repository contains `ProjectSettings/ProjectVersion.txt`, which selects Unity `2022.3.30f1`.
8. Open **Advanced settings** and configure:
   - **Pre-export method name:** `ArenaFall.Editor.ArenaFallCloudBuild.PreExport`
   - Under Android, leave **Build app bundles (.aab) instead of an APK** turned **off**. This produces an APK.
9. Save the configuration.
10. Select the `ArenaFall-APK` configuration and press **Build**.

When it finishes, open the build record and download its APK artifact. That is the only button you need for later APK builds.

## Later builds

- Want a new APK manually? Open **Build automation** > **Configurations** > `ArenaFall-APK` > **Build**.
- Want an APK for every GitHub update? Enable **Auto-build** for the `main` branch in the configuration.

## Signing and Google Play

The first APK can be a debug/test build. Before publishing, create an Android signing keystore and add it under the build configuration's **Credentials** section. Never put a `.keystore` file or its passwords in GitHub. For Google Play choose the Android option to build an `.aab` instead of an APK.
