# MoreBasic

This folder contains project-owned copies of the multiplayer template assets used by `MoreBasicScene`.

## Safe to edit

- `Prefabs/Player/MoreBasicLocalRig.prefab`
- `Prefabs/Player/MoreBasicAvatarBase.prefab`
- `Prefabs/Managers/MoreBasic Network Manager.prefab`
- `Prefabs/Managers/MoreBasic Game Manager.prefab`

`MoreBasicScene` references these copies instead of the corresponding assets in `VRMPAssets`.
The owned Network Manager currently spawns `MoreBasicAvatarBase` as the player avatar.

## Still inherited

The XR Origin remains a prefab variant and therefore still inherits its large template hierarchy.
That is intentional for the first checkpoint: ownership has changed, but runtime behavior has not.
Later checkpoints can simplify the hierarchy without modifying the original template.

## Voice presentation

The owned network avatar has an `AvatarVoiceSignal`. During Play mode it exposes raw
Vivox-derived loudness and a reusable `Loudness` value produced by its editable remapping curve.
Its **On Loudness Changed (float)** Inspector event can connect that value to any public method
that accepts one float; voice does not intrinsically mean mouth animation.

`MouthMotion` is the current response component. It maps loudness to a mouth blend shape
selected by name through **On Loudness Changed (float)**.

`MoreBasicAvatarVisuals` preserves the useful template visual behavior but disables its
hard-coded voice-to-blend-shape-zero behavior, so response components remain in control.
The owned network avatar currently connects that event to `MouthMotion.SetLevel`,
mapping quiet-to-loud output from 100 to 0 on the explicitly named `Mouth_Open` blend shape.
That event can instead—or additionally—connect to any custom component's public method
that accepts one float.

## Local first-person visibility

Network avatars use `LocalAvatarHiddenRenderers` to hide face geometry from the player
who owns that avatar. This prevents the local VR camera from seeing the inside/back side
of its own face, eyes, eyebrows, hats, or headset geometry.

On the network avatar root, edit **GameObjects hidden from the owning player** for whole
face-part objects, or **Renderers hidden from the owning player** for individual renderers.
Remote players still see these normally.

## Avatar pose presentation

The network avatar uses `AvatarPositioner` instead of the template `XRAvatarIK`.
MoreBasic avatars are now intentionally part-based: assign visible GameObjects for the
head, body, left hand, and right hand instead of using armature bones.

**Preserve Authored Rotation Offsets** should normally remain checked. It preserves each
part's authored rotation difference from the tracking source, so model-specific axes can be
fixed in the prefab instead of in code.

`MoreBasicScene` no longer keeps a separate offline avatar active as a network object.
The Network Manager should be responsible for spawning the player avatar after connection.
Avoid placing a copy of the network player prefab in the scene with its `NetworkObject`
active; that can interfere with Netcode spawning and scene synchronization.

## Simple world mirror

For solo headset testing, add a flat Quad or Plane to the scene and attach `SimpleWorldMirror`.
The object's forward direction is treated as the mirror normal, so rotate the object until its
blue arrow points out from the reflective side.

At runtime the component creates a small reflection camera and paints its view onto the object.
It does not know anything about avatars; it simply reflects whatever the mirror camera sees.

## Vivox development setup

For local development, check **Test Mode** under **Edit > Project Settings > Services > Vivox**.
Test Mode lets the client generate Vivox access tokens locally with the project's secret key,
which is convenient for testing but intentionally produces a security warning.

Do not ship a production build this way. A secret included in a client build can be extracted.
Before release, disable Test Mode, remove any client-side Vivox secret, and generate Vivox
access tokens on a trusted server.

## TODO

- Detect likely audio feedback or echo when multiple users are physically in the same room.
  Consider comparing sustained microphone and remote-output levels, then warn users or
  automatically reduce/mute one voice path with a clear notification and manual override.
- Add a controller-mode avatar hand visual. Networked finger tracking now works for hand
  tracking, but controller users still appear as static hand models. A future pass should
  switch the visible avatar hand objects to controller models when controller mode is active.
- Restore the camera in the player appearance UI. It would be useful as an in-menu preview
  where players can quickly check what their avatar looks like.
- Clarify or change room privacy. In the classroom version, "private" should mean preventing
  new people from joining the room/session if that is not already what the current setting does.
