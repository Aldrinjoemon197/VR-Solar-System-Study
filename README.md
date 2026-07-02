# Unity Solar System VR

A VR solar system exploration project built in Unity. The final working scene is:

`Assets/Solar system backup 2.unity`

This repository is cleaned so it contains only the files needed for the current working project: code, scene, project settings, packages, and referenced assets.

## Required Environment

Use the same setup for everyone on the team:

- Unity Editor: `6000.4.5f1`
- Unity Hub
- GitHub Desktop
- Git LFS
- Windows PC
- VR headset/controllers supported by the configured XR setup, tested around Quest-style controls

Unity packages are restored from `Packages/manifest.json`. Main packages used:

- High Definition Render Pipeline `17.4.0`
- Input System `1.19.0`
- XR Interaction Toolkit `3.4.1`
- Oculus XR Plugin `4.5.4`
- OpenXR Plugin `1.17.0`

## Important: Git LFS

This project contains large asset files. Before cloning or pulling the project, install Git LFS:

```powershell
git lfs install
```

If you use GitHub Desktop, install Git LFS on your machine first. Otherwise large files such as `.exr` or `.psd` assets may not download correctly.

## First Time Setup

1. Install Unity Hub.
2. Install Unity Editor `6000.4.5f1`.
3. Install Git and Git LFS.
4. Clone this repository with GitHub Desktop.
5. Open Unity Hub.
6. Click **Add project from disk**.
7. Select the cloned repository folder.
8. Open the project with Unity `6000.4.5f1`.
9. Wait for Unity to import packages and assets.
10. Open the scene:

```text
Assets/Solar system backup 2.unity
```

11. Press Play, or connect/build to the VR headset using the configured XR settings.

## What Is In The Project

The current clean project keeps:

- Final Unity scene
- Solar system planet assets/materials
- Spaceship third-person VR controller
- Wormhole travel system
- Quest joystick movement
- Gun laser and gun visual scripts
- Solar system analysis mode
- Earth wrist hologram / info target scripts
- XR, HDRP, and project settings

## Main Scripts

- `SpaceshipThirdPersonController.cs` - spaceship mode, ship movement, third-person camera placement
- `WormholeIntroTravel.cs` - wormhole transition and intro movement logic
- `QuestJoystickMove.cs` - normal VR movement outside ship mode
- `SolarSystemAnalysisMode.cs` - analysis mode and planet information controls
- `QuestHandGunLaser.cs` - gun laser interaction
- `BetterSciFiGunVisual.cs` - gun visual builder
- `EarthLayerWristHologram.cs` - wrist hologram UI
- `EarthLayerInfoTarget.cs` - Earth layer info display target
- `ExactHalfPlanetSplitter.cs` and `VenusHalfPlanetSplitter.cs` - planet splitting visuals

## Scene Notes

The final scene contains the active VR rig:

- `XR Origin (VR)`
- `Main Camera`
- `LeftHand Controller`
- `RightHand Controller`
- `ShipSpawnPoint`
- `ShipExitPoint`
- `SolarSystemLandingPoint`

Project build settings are already pointed at:

```text
Assets/Solar system backup 2.unity
```

## Controls

Current behavior is based on Quest-style controls:

- Left thumbstick: movement/ship movement depending on mode
- Right thumbstick: ship turning / camera turning depending on mode
- Left grip: toggle ship mode and astronaut mode
- Right A/B: vertical movement where enabled
- Left menu / glove button: wormhole trigger

## If Something Looks Missing

If textures or large assets are missing after cloning:

1. Confirm Git LFS is installed.
2. In the repository folder, run:

```powershell
git lfs pull
```

3. Reopen Unity and let it reimport.

## GitHub Notes

Do not commit Unity generated folders. They are ignored by `.gitignore`:

- `Library/`
- `Temp/`
- `Obj/`
- `Logs/`
- `UserSettings/`
- generated `.csproj` and solution files

Only commit source files, scenes, assets, packages, and project settings.
