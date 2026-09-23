
# XR Classroom

XR Classroom is a base project for prototyping VR multiplayer classroom experiences. The idea is to allow you to skip solving multiplayer networking, voice chat, and avatar representation — this project has already solved those problems once, reasonably well, and you can get straight to the interesting stuff.

If you have ANY questions about how to use it, please contact me at simonson.au@northeastern.edu 

## Important note about Unity Versions
Unity is very picky about what version you are using!!
This project is in unity version 6000.0.51f1 and you should not open it in anything else or it will definitely break!
If you're new to github, don't be intimidated by all of the things that seem to want you type stuff into the terminal. You can just download a zip file, unzip it, and open that through the Unity hub. 
![The green "Code" button on GitHub, expanded to show the "Download ZIP" option](README_Images/downloadzip.jpg)


## Table of contents

- [Introduction](#introduction)
- [Project structure](#project-structure)
- [Vivox (voice chat)](#vivox-voice-chat)
- [Changing the environment](#changing-the-environment)
- [Avatars](#avatars)
- [Networking reference](#networking-reference)

## Introduction

This project is built on top of **Unity's official VR Multiplayer project template** — the one Unity ships as a starting point for networked VR apps, built on XR Interaction Toolkit, Netcode for GameObjects, and Unity Gaming Services (Lobby, Relay, Vivox, Authentication).

![Unity Hub's New Project screen with the VR Multiplayer template selected](README_Images/vrmultiplayertemplate.jpg)

XR Classroom **simplifies** that template significantly, and removes a lot of its functionality in exchange for something smaller and easier to understand. Concretely, this project:

- Replaces the template's full lobby browser (create a room, browse a list of rooms, join a specific one) with a single "Join Online" button that quick-joins or creates a session automatically. **If you need multiple simultaneous rooms that players can browse and choose between, this project doesn't do that anymore — go back to the original VR Multiplayer template and build from there instead.**
- Removes the template's sample mini-games and its in-editor tutorial system. You don't need them.
- Replaces the template's rigged, bone-based avatar system with a much simpler part-based one (see [Avatars](#avatars) below) — no skeletons, no armatures, nothing that requires rigging skill to extend.


## Project structure

| Folder                     | What's in it                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Assets/Scripts`           | These are all of the scripts that Aubrey or an AI he was using wrote and can probably answer questions about.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `Assets/Prefabs/Managers`  | `XR Classroom Game Manager` and `XR Classroom Network Manager` — the two manager prefabs that drive the scene and the network session.                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| `Assets/Prefabs/Player`    | `XRClassroomLocalRig` and `XRClassroomAvatarBase` — see [Avatars](#avatars) for what these are and how they differ.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| `Assets/Avatars`           | The source art for the costume closet: `HeadModels/`, `BodyModels/`, `DefaultAvatar.fbx` (the base head/body mesh), and `SimpleAvatar.prefab` (the default avatar look, nested inside `XRClassroomAvatarBase`).                                                                                                                                                                                                                                                                                                                                                                                     |
| `Assets/Scenes`            | Just `XRClassroom.unity` — the one finished scene.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| `Assets/EnvironmentAssets` | The active skybox (material + texture pair) and the environment mesh. See [Changing the environment](#changing-the-environment).                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| `Assets/Materials`         | Various materials used around the scene — avatar skin/eye materials, ground/building materials, and the teleport boundary debug material.                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| `Assets/Tools`             | `Two Player Scenario.asset` — a Unity **Multiplayer Play Mode** config that lets you test with two simulated clients in one Editor session (Window → Multiplayer Play Mode). Useful for testing networked changes without a second headset.                                                                                                                                                                                                                                                                                                                                                         |
| `Assets/Resources`         | Empty right now, but special: anything you put here ships in the build automatically and can be loaded by name at runtime (`Resources.Load`), with no scene reference needed. Only put something here if you specifically need that behavior.                                                                                                                                                                                                                                                                                                                                                       |
| `Assets/TextMesh Pro`      | This is a standard Unity asset for text that looks nice and you shouldn't need to do anything with it.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| `Assets/XR`, `Assets/XRI`  | This is all of the stuff that makes the XRTK work. Don't touch these.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| `Assets/VRMPAssets`        | The trimmed remainder of the original VR Multiplayer Template. This is the foundation almost everything else is built on — network session management, voice chat, hand-pose replication, core UI. `Assets/VRMPAssets/Scripts` in particular is essential; see [Networking reference](#networking-reference) below for the pieces you're most likely to actually touch. The rest of this folder is a mix of things that are load-bearing and things that are just unaudited template leftovers — if you're not sure whether something in here is used, check what references it before deleting it. |
| `Assets/XRTKSamples`       | Imported samples from the XR Interaction Toolkit and XR Hands packages (renamed from Unity's default `Samples` folder for clarity). The actual player rig — controllers, teleportation, hand tracking — is built as a prefab variant chain that runs through here. Important -- don't delete it.                                                                                                                                                                                                                                                                                                    |
| `Assets/AVATAR_GUIDE.md`   | Older documentation with some more details about how avatars work. Might be out of date?                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |

## Vivox (voice chat)

[Vivox](https://unity.com/products/vivox) is Unity's real-time voice chat service, and it's what carries player voice over the network in this project. `XRINetworkPlayer` (part of the template) is where all of this is connected. 

**Before you can test voice chat locally**, go to **Edit → Project Settings → Services → Vivox** and enable **Test Mode**. This lets your Editor generate Vivox access tokens locally using the project's secret key, which is what you want for development, but Unity will show a security warning — that warning is correct and intentional.

**Do not ship a build with Test Mode on.** A secret embedded in a client build can be extracted by anyone who has the build. Before any real release, disable Test Mode, remove the client-side Vivox secret, and generate Vivox access tokens from a trusted server instead.

## Environment

In the main scene, Environment>environment is a gameobject that contains a 3D model of approximately Centennial Common, along with a bunch of walls and teleport areas. The walls and teleport areas were manually placed to map out where players should and should not be able to go. Feel free to delete any of that stuff and replace it with other things -- it shouldn't break anything. 

The environment's skybox lives in `Assets/EnvironmentAssets/` as a material + texture pair (`Sky_Anime_11_morning_a.mat` / `.png`). To change it, either edit that material's texture directly, or point the scene's **Skybox Material** (Window → Rendering → Lighting → Environment tab) at a different material entirely. 

### How the boundary script works

`Boundary.cs` is a safety net for players who somehow end up outside the space they're supposed to be in. Put it on a cube with "isTrigger" marked on the collider, and players who reach that cube will be teleported back to whatever you marked as the Respawn Point.

It's more involved than "move one transform" because this is a networked game, and a few things have to be true for it to work correctly:

- **It only ever moves the local player.** The XR rig is a plain, non-networked object that exists once per client — Boundary specifically checks that whatever touched it belongs to the local player's `CharacterController`, not a remote player's avatar.
- **It moves the player through the same `TeleportationProvider` the rest of the project uses**, instead of setting `transform.position` directly. Writing the position directly fights the `CharacterController` and skips the game's normal teleport handling (comfort fade, gravity), so this avoids the player getting stuck in geometry or slamming into the floor on arrival.
- **It clears any built-up fall velocity** once the teleport lands, so a player who was falling doesn't immediately fall again.
- **It drops anything the player is holding** before moving them, instead of dragging a held object across the map or leaving its network ownership in a weird state.
- **It has a short cooldown** so lingering in the boundary volume doesn't queue up a burst of teleports.

Nothing about this needs to be networked directly — each client is entirely responsible for rescuing its own player, and the normal avatar-position replication (see [Avatars](#avatars)) carries the result to everyone else automatically.

## Avatars

The avatar system is as simple as it could be made
- **There are no skeletons and no armatures.** Avatars are not rigged humanoids.
- **Every avatar has a head, a body, and hands** — each one is just a GameObject (which can itself contain smaller pieces, like eyes or hair on a head object), positioned and rotated by a script instead of animated by a skeleton.
- **Hands are the one part of this that's still a little complicated.** Avatars have hands, but — unlike heads and bodies — there's currently no way to swap them out. That's future work; see "Cool ways this could be improved" below.

### Two copies of every avatar

Multiplayer is inherently a little complicated here, because every computer in the session needs a copy of *every* player's avatar, and needs to know which one is "me" versus everyone else. Concretely, there are two different prefabs:

- **`XRClassroomLocalRig` is you.** It's the camera, hands, and interactors driven directly by your headset and controllers. It is *not* networked, and no one else ever sees it — it's purely how your own client tracks and drives your input.
- **`XRClassroomAvatarBase` is how other players see you.** It's the networked avatar prefab, spawned once per connected player by the Network Manager. Your own client's copy of it is driven by your local rig's tracked poses (replicated over the network); every other client's copy of it is driven by the replicated data it receives from you.

If you're trying to figure out why something looks right on your own screen but wrong to other players (or vice versa), this is usually the first thing to check — you're probably looking at the wrong one of these two prefabs.

### Costume Closet: how avatar customization works

![The Costume Closet in the scene](README_Images/costumecloset.jpg)

The **Costume Closet** is a collection of heads and a collection of bodies that a player can put on. The obvious way to implement this would be "send the newly-chosen prefab to every other machine over the network" — but that doesn't actually work, because a 3D model is too much data to reasonably send around every time someone changes clothes.

Instead: **every client already has an identical copy of the full list of available garments**, sitting in the closet in the scene. Choosing a garment doesn't send a model anywhere — it just tells the network "I'm now wearing the one named X," and every client looks that name up in its own local copy of the closet and switches to it. 

> **Every wearable in the closet needs a unique, stable `GameObject` name.** That name is the only thing that gets sent over the network to identify a garment — if you rename or duplicate a garment carelessly and end up with two objects sharing a name (or a name that changes), the network sync will match the wrong one, or nothing at all.

### Adding new items to the costume closet

1. Drag your 3D model into the scene.
2. Duplicate one of the existing **Garment Button**s in the Costume Closet.
3. Delete whatever model is currently sitting inside that duplicated button.
	1. ![A duplicated Garment Button with its placeholder model deleted](README_Images/garment_button.jpg)
	2. in this case it would be SquareHead
4. Make your 3D model a child of the Garment Button.
5. On the Garment Button's **Wearable Garment** component, point the **Garment Prefab** field at your 3D model (the one you just made a child of it).
	1. ![The Wearable Garment component's Garment Prefab field](README_Images/wearable_garment.jpg)
6. Set **Garment Type** to whichever it is: **Head** or **Body**.
7. Adjust where it is on the shelf to not look stupid.

That's the whole process — no code required. Remember the naming rule above: give your model's `GameObject` a name that isn't already used by another garment.

## Networking reference

The **`SessionManager`** is the thing you want to interact with for anything related to creating, joining, or leaving a networked session — creating a room, quick-joining one, leaving, changing a room's name or privacy. It's accessible as `XRINetworkGameManager.Instance.sessionManager`.

If your code just needs to know whether the player is currently online, don't go through `SessionManager` — check:

```csharp
XRINetworkGameManager.Connected.Value
```

That's a bindable bool (`XRINetworkGameManager.Connected.Subscribe(...)` to react to it changing, or just read `.Value` for a one-off check) and it's the standard way anything in this project answers "am I online right now?" `ConnectionUIController` (`Assets/Scripts/ConnectionUIController.cs`) is a good example to read if you want to see this pattern in use — it also demonstrates `XRINetworkGameManager.CurrentConnectionState`, which additionally distinguishes "connecting" from "connected," in case a simple online/offline bool isn't enough for what you're building.

### Cool ways this could be improved

- **Better models!** I am not really much of a technical artist. This is designed to be extended by someone who can make interesting shapes on purpose in Blender but doesn’t require you to know how to do rigging or programming.
- **Hands.** Right now there’s no way to change out the hand models. There are a lot of bones in the hand, and that makes them a bit more complicated.
- **Mouth Animations**. 
- `AvatarVoiceSignal` turns the voice level received by XRINetworkPlayer from Vivox into a float called m_Loudness between 0 and 1. There's also a unity event associated with it (**On Loudness Changed**). Use it to animate the avatar of whoever is talking in some way. `AvatarVoiceSignal` deliberately doesn't know anything about mouths — its event can be pointed at any public method that takes a single float, so you can drive other things (a glow, a particle effect, a UI meter) off the same voice signal without touching it.

### Known Bugs and Problems to Fix
- Right now when you teleport, you see the back of your own avatar for a second. 
- Eyeballs on current heads have too many polygons. It creates a bit of lag when you look at all of them. Either the parts of the eyes that are inside of the head and can't be seen by anyone should be deleted, or the meshes should be decimated, or both. 
- You can sometimes see parts of your avatar's own face, especially when using translational motion. Right now the solution to allow the camera to be inside of the avatar's head while mostly being able to see out of it is to set the near culling plane a little far. That isn't a particularly elegant solution and you could probably do something better with it. 
- Hands are not networked at all right now. You can see your hands. No one else can see your hands. Hands have been quite complicated throughout this project. 
- Player nameplates (the thing floating above your head that lets other players see your name) are generally not working right now. 