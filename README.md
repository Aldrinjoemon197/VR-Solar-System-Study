# VR Solar System

A VR solar system exploration project built in Unity. The user flies through a to-scale model of the solar system, splits planets open with a hand-held gun to reveal their internal layers (crust/mantle/core for rocky planets, atmosphere/hydrogen layers for gas and ice giants), and can use an Analysis Mode to compare planets, measure distances between them, and inspect their moons.

This guide is written for someone setting the project up for the **first time**, with no assumptions about prior Unity or Git experience.

## 1. What You Need Before Starting

Install these first, in this order:

1. **Unity Hub** — download from [unity.com/download](https://unity.com/download). Unity Hub is the launcher that manages Unity Editor versions and projects; you do not install Unity itself directly.
2. **Unity Editor version `6000.4.5f1`** — installed *through* Unity Hub (see step-by-step below), not downloaded separately.
3. **Git** — needed to download (clone) the project. On Windows, install [Git for Windows](https://git-scm.com/download/win). On macOS, Git usually comes with Xcode Command Line Tools, or install via [git-scm.com](https://git-scm.com/download/mac).
4. **Git LFS** (Git Large File Storage) — this project stores large files (textures, audio, HDRI skyboxes) through Git LFS. Without it, those files will silently fail to download correctly. Install from [git-lfs.com](https://git-lfs.com/), then run this once in a terminal:
   ```
   git lfs install
   ```
5. *(Optional, recommended)* **GitHub Desktop** — a graphical alternative to typing Git commands, available at [desktop.github.com](https://desktop.github.com/).
6. *(Optional)* **A VR headset** (Quest-style controllers) if you want to test the actual VR interaction. **You do not need a headset to open the project, look at the scene, or read the code** — the Unity Editor runs fine without one, and the gun/hologram scripts have a mouse-and-keyboard fallback for testing without hardware (see Section 5).

This project can be opened and run on both **Windows and macOS** — it was actively developed and tested on macOS during parts of this project.

## 2. Download the Project

**Option A — GitHub Desktop (easier for beginners):**
1. Open GitHub Desktop.
2. File → Clone Repository → paste this repository's URL.
3. Choose a location on your computer and click Clone.

**Option B — command line:**
```
git clone <repository-url>
```

Either way, wait for the download to finish completely before continuing — this repository contains large binary files (textures, HDRI skybox, audio) and the initial clone can take a while.

## 3. Download the Missing "Space Cube" Skybox Asset

One large environment asset (a cubemap/skybox used to render the star field around the solar system) is **not stored in this Git repository**, because it repeatedly caused upload/download problems through Git and Git LFS. Instead, it's hosted separately here:

**[Google Drive: Space skybox cubemap](https://drive.google.com/drive/folders/1zSDA1SFh0TvLP2tIYuCZ-qPc475b6TUw)**

To use it:
1. Open the link above and download the file(s) in that folder.
2. Copy the downloaded file(s) into the project's `Assets/` folder (inside the `VR Solar System` project folder you cloned in Step 2).
3. If Unity is already open, right-click in the Project window and choose **Reimport All** so it picks up the new file.

Without this asset, the project will still open and run, but the space/star background around the solar system will not render correctly (you may see a blank or pink background instead of stars).

## 4. Install Unity and Open the Project

1. Open **Unity Hub**.
2. Go to the **Installs** tab → **Install Editor** → find and install version **`6000.4.5f1`** specifically (not just "the latest version" — using a different version can corrupt the scene). If Unity Hub prompts you with a version-mismatch warning when opening the project later, choose to install the exact version it asks for.
3. Go to the **Projects** tab → **Add** → **Add project from disk**.
4. Select the `VR Solar System` folder (the one containing the `Assets`, `Packages`, and `ProjectSettings` folders — this is *inside* the folder you cloned in Step 2, not the top-level repository folder).
5. Click the project to open it. Unity will spend a few minutes importing packages and assets the first time — this is normal, let it finish.
6. If a **HDRP Wizard** window pops up automatically, that's expected and safe — everything under "Global" and "Current Quality" should already show green checkmarks. You can close this window; no action is needed unless something shows a red warning.

## 5. Open the Scene and Run It

1. In the **Project** window (bottom panel), find and double-click:
   ```
   Assets/Solar system backup 2.unity
   ```
2. Press the **Play** button (▶) at the top of the Editor.
3. **If you don't have a VR headset connected**, you can still test the core interactions with mouse and keyboard:
   - **Hold the left mouse button** to aim (equivalent to the left controller trigger).
   - **Right-click** to fire the gun and split whichever planet you're aiming at.
4. **If you do have a Quest-style headset** connected and configured, put it on and use the controls listed below.

You should see the player start in a small **tutorial lobby**, with on-screen prompts explaining movement and shooting. Pulling the trigger/pressing the menu button there sends you through a wormhole into the solar system itself, where you can fly to any planet, shoot it to split it open, and point your other hand at the exposed layers to read information about them in a hologram panel.

## 6. Controls

Quest-style controls:

- **Left stick**: move / fly
- **Right stick**: turn / camera
- **Left grip**: toggle between Spaceship mode and Astronaut (free-movement) mode
- **A / B buttons**: move up / down
- **Left trigger**: aim (shows a targeting beam)
- **Right trigger**: fire the gun (splits whatever planet is hit)
- **Left Y button**: open/close Analysis Mode (Distance, Compare, and Moons pages)
- **Menu button**: activates the wormhole from the lobby into the solar system

## 7. What's In the Project

- All eight planets (Mercury through Neptune) can be split open to reveal scientifically-based internal layers.
- A hologram on the left hand identifies whichever layer you're pointing at, in real time.
- An **Analysis Mode** lets you compare two planets' atmospheric composition, measure the distance between planets, and view a planet's moons.
- A procedurally generated **asteroid belt** sits at its real proportional distance between Mars and Jupiter.
- A **tutorial lobby** teaches the controls before the player enters the solar system.
- Spaceship third-person travel, a wormhole transition effect, procedural ambient/action audio, and a Sun destruction effect.

### Main Scripts

- `PlanetHalfPlanetSplitter.cs` / `PlanetSplitMeshUtility.cs` — internal-layer splitting for Mercury, Mars, Jupiter, Saturn, Uranus, and Neptune (shared implementation).
- `ExactHalfPlanetSplitter.cs` / `VenusHalfPlanetSplitter.cs` — internal-layer splitting for Earth and Venus (each has its own implementation).
- `EarthLayerWristHologram.cs` — the left-hand hologram that identifies planets/layers in real time.
- `AsteroidBeltGenerator.cs` — procedurally scatters the Main Asteroid Belt.
- `SolarSystemAnalysisMode.cs` — the Distance / Compare / Moons analysis menu.
- `LobbyTutorialSystem.cs` — the tutorial lobby shown at the start of a session.
- `QuestHandGunLaser.cs` / `BetterSciFiGunVisual.cs` — the gun's aim/fire logic and visual model.
- `SpaceshipThirdPersonController.cs` / `WormholeIntroTravel.cs` / `QuestJoystickMove.cs` — movement and travel between the lobby and the solar system.
- `SunDestructionController.cs` / `ProceduralSolarSystemAudio.cs` — Sun visual effect and procedural audio.

## 8. Project Documentation

A written report describing the project's design and implementation (for course submission) is included at `VR_Solar_System_Report.tex`, formatted using the IEEE VGTC/TVCG journal template (the required class file and bibliography style files are included alongside it in this repository, so it can be compiled directly, e.g. by uploading the whole repository folder to [Overleaf](https://www.overleaf.com/)).

`Project_Directory_Structure.txt` lists every file and folder in this repository, with a short note on what the key ones are. If anything looks missing or out of place after cloning, or if you're not sure where a particular file is supposed to be, check that file first.

## 9. Troubleshooting

**Textures or the skybox look wrong / missing after cloning:**
1. Confirm Git LFS is installed (`git lfs install`).
2. In the repository folder, run:
   ```
   git lfs pull
   ```
3. Confirm you've also downloaded the separate skybox asset from the Google Drive link in Section 3.
4. Reopen Unity and let it reimport (right-click in the Project window → Reimport All).

**Unity asks to install a different Editor version:**
Install the exact version it requests (should be `6000.4.5f1`). Using a different version can cause the scene or HDRP settings to behave unexpectedly.

**No VR headset available:**
See Section 5 — the Editor supports mouse-and-keyboard testing for the gun and hologram without a headset.

## 10. For Contributors: Git Notes

Do not commit Unity-generated folders — they're already excluded by `.gitignore`:
- `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, generated `.csproj`/solution files

Only commit source files, scenes, assets, packages, and project settings.
