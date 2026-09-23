# Creating Avatars for XR Classroom

This guide explains how to prepare an avatar in Blender and connect it to the XR Classroom
multiplayer avatar system in Unity. It assumes familiarity with basic Blender modeling
and FBX export, but no prior knowledge of this Unity project.

## The short version

XR Classroom avatars are currently **part-based puppets**, not rigged humanoids.
At minimum, make separate visible objects for:

1. Head.
2. Body.
3. Left hand.
4. Right hand.

Each part can be a single mesh or a parent GameObject containing smaller pieces. For example,
the head object can contain eyes, eyebrows, mouth, hair, glasses, and other face parts.

Optional extras:

| Feature | Avatar asset needs | Unity hookup |
| --- | --- | --- |
| Head tracking | A head GameObject with a useful origin | Assign it as **Head Object** |
| Body tracking | A body GameObject | Assign it as **Body Object** |
| Hand tracking | Left/right hand GameObjects | Assign them as **Left Hand Object** and **Right Hand Object** |
| Talking mouth | A blend shape/shape key | `MouthMotion` targets it by name |
| Player color | A suitable material property | Currently requires additional avatar-specific setup |

The safest workflow is to keep the networking/tracking root from the existing
`XRClassroomAvatarBase` prefab and replace only its visual model.

## How the system is divided

The avatar has three conceptual layers:

```text
XR headset/hands -> networked pose transforms -> visible avatar part objects
```

- `XRINetworkPlayer` and Netcode handle ownership and pose synchronization.
- `AvatarPositioner` positions the visible head, body, and hand objects.
- `AvatarVoiceSignal` publishes a reusable loudness value.
- Components such as `MouthMotion` decide what loudness does visually.

The visual model should not contain networking or Vivox logic.

## Preparing the model in Blender

### 1. Establish a clean rest pose

- Put the avatar in the pose it should have while idle.
- Place the object origin somewhere intentional and easy to reason about.
- Avoid unexplained parent inverses or constraint offsets.
- Check the model from front, side, and top views.

For a head-and-torso avatar, it is useful for the model's root to be centered beneath the
head rather than at an arbitrary point left over from modeling.

### 2. Apply object transforms

In **Object Mode**, select the exported objects, then use:

**Object > Apply > Rotation & Scale** (`Ctrl+A`)

Afterward, objects should normally display:

```text
Rotation: 0, 0, 0
Scale:    1, 1, 1
```

Applying transforms does not mean moving the object to the world origin. Location can remain
meaningful. Applying rotation and scale makes Blender, FBX, and Unity agree about each part's
coordinate space.

Armatures are intentionally out of scope for now. If a source model has an armature, bake or
convert it into separate head/body/hand objects before bringing it into the XR Classroom avatar
pipeline.

### 3. Prepare a talking-mouth shape key, if desired

The mouth shape is optional. To support it:

1. Create a Basis shape key.
2. Create one mouth-motion shape key.
3. Give it a descriptive name. `Mouth_Open` is the current project convention.
4. Verify that changing only this shape key does not move the eyes, skull, teeth, or unrelated
   vertices.
5. Test both values `0` and `1` in Blender.

Either direction is supported:

- If `0` is closed and `1` is open, use Quiet Weight `0`, Loud Weight `100`.
- If `1` is closed and `0` is open, use Quiet Weight `100`, Loud Weight `0`.

The current avatar uses the second convention. A new avatar may use the first convention if
the `MouthMotion` weights are configured accordingly.

### 4. Materials

- Use a manageable number of material slots.
- Give materials descriptive names.
- Prefer shaders that expose `_BaseColor` when using URP.
- Avoid relying on a material's slot number as its meaning.

The inherited template visual code still contains some assumptions about player-color material
slots. Treat automatic player color as optional until an avatar-specific color target is
configured.

### 5. Export FBX

Recommended starting settings:

- Export selected objects only.
- Include Mesh object types.
- Forward: `-Z Forward`.
- Up: `Y Up`.
- Apply Transform: use consistently and verify the imported result.
- Bake Animation: off for a static avatar; on only when exporting intentional animation.
- Shape Keys: ensure they are included when mouth motion is needed.

FBX settings vary somewhat by Blender and Unity version. A clean reimport with predictable
scale and orientation matters more than copying settings blindly.

## Importing into Unity

1. Put the FBX in an avatar-specific folder under `Assets/Avatars` or another project-owned
   content folder.
2. Select it in the Project window.
3. Inspect the **Model**, **Rig**, **Animation**, and **Materials** tabs.
4. For a static model, set **Animation Type** to **None** and disable **Import Animation**.
5. Confirm the expected blend shape appears on the imported `SkinnedMeshRenderer`, if using mouth motion.
6. Drag the imported model into an empty test scene and inspect it before networking is involved.

Test the mouth blend-shape slider manually. The eyes and top of the head should remain still.

## Creating a network avatar

Do not build a network player from scratch for each visual design. Duplicate the existing owned
prefab:

`Assets/Prefabs/Player/XRClassroomAvatarBase.prefab`

Give the duplicate a clear avatar-specific name. Keep these parts:

- Root `NetworkObject`.
- `XRINetworkPlayer`.
- Networked head and hand pose transforms.
- `AvatarPositioner`.
- `AvatarVoiceSignal`.
- Any hand-replication components that the project still uses.

Replace or disable only the existing visual hierarchy, then add the new imported parts beneath
the avatar prefab.

### Configure Avatar Positioner

On `AvatarPositioner`, assign:

- **Network Player**: optional. Usually the component finds the root `XRINetworkPlayer` automatically.
- **Tracked Head / Left Hand / Right Hand**: keep the prefab's networked pose targets, or leave blank to auto-fill from `XRINetworkPlayer`.
- **Avatar Root**: keep the network avatar root.
- **Head Object**: the visible head parent object.
- **Body Object**: the visible body object.
- **Left Hand Object** and **Right Hand Object**: visible hand objects.
- **Head/Body/Hand Offsets**: model-specific alignment between tracking targets and visible parts.
- **Preserve Authored Rotation Offsets**: normally checked.
- **Body Uses Head Yaw Only**: normally checked, so the body turns left/right without pitching and rolling with the headset.
- **Body Turn Threshold/Speed**: how far and how quickly the torso follows head yaw.

### Hide local first-person face geometry

On the network avatar root, find `LocalAvatarHiddenRenderers`.

Add any whole GameObjects or individual renderers that should be invisible only to the player
wearing that avatar:

- head or face mesh;
- eyeballs, eyebrows, teeth, tongue, or inner mouth meshes;
- hats, hair, or accessories that clip into the local camera.

This is only a local comfort/visibility setting. Remote players still see those renderers.

### Configure mouth motion

Add or retain `MouthMotion` and assign:

- **Renderer**: the `SkinnedMeshRenderer` containing the mouth shape.
- **Blend Shape Name**: for example, `Mouth_Open`.
- **Quiet Weight** and **Loud Weight**: based on the shape's authored direction.

On `AvatarVoiceSignal`, find **On Loudness Changed (float)** and add a listener:

1. Drag the object containing `MouthMotion` into the event target.
2. Select `MouthMotion > SetLevel(float)` from the dynamic-float section.

The prefab currently contains this connection as a working example.

### Connect loudness to something other than a mouth

`AvatarVoiceSignal` exposes two Play-mode values:

- **Raw Loudness**: the Vivox-derived level after the template's smoothing.
- **Loudness**: the value after the editable remapping curve.

The **On Loudness Changed (float)** event can have multiple listeners. To drive another effect,
write a small component with a public method that accepts one float, then connect that method in
the event. This keeps the core voice system independent of any particular visual behavior.

Use the remapping curve to create a noise threshold, exaggerate quiet speech, or limit the
response without changing the receiving component.

## The offline preview avatar

Before you're connected to a session, you still need to see your own head and body (in a mirror,
looking down at yourself, etc.), and the Costume Closet needs something local to update the
instant you try something on. There is no separate "offline avatar" prefab to build or maintain
for this — it's handled by one object already sitting in the scene.

That object is **`OfflineAvatarPreview`**. It holds its own `AvatarPositioner` with **Is Self**
checked, which is what tells `AvatarPositioner` to read live head/hand tracking directly instead
of waiting on networked pose data — offline, there's no network connection to read from yet.
Concretely:

- Its tracked-head target resolves to your XR camera. `LocalHeadPoseSource` (rather than a
  networked transform) is what supplies that pose.
- Its **Head Object** / **Body Object** are the same transforms the Costume Closet's `Head Parent`
  / `Body Parent` point at. That's the important part for avatar authoring: the Costume Closet
  doesn't have a separate "offline" and "online" target to keep in sync — trying on a garment
  updates `OfflineAvatarPreview`'s head or body directly, so whatever you're wearing before you
  connect is exactly what `AvatarCostumeSync` tells everyone else you're wearing once you do.
- `OfflineAvatarVisibility` hides `OfflineAvatarPreview`'s visuals once you connect (at that point
  `XRClassroomAvatarBase` is what's showing you, including to yourself) and shows them again if
  you disconnect. It keeps listening for connection changes the whole time, so this handoff keeps
  working across repeated connects/disconnects, not just the first one.

If you're building a new avatar, you generally don't need to touch `OfflineAvatarPreview` directly
— it already points at the same head/body targets the Costume Closet manages. You only need to
care about it if you're changing how the *default* (not-yet-customized) look works, or debugging
why something looks right once connected but wrong before connecting (or vice versa) — in which
case, this object, not `XRClassroomAvatarBase`, is what you were actually looking at.

> One loose end from before this rewrite: the transform `OfflineAvatarPreview`'s head and body
> objects live under is still internally named `MoreBasicAvatarBase` in the scene, left over from
> before the project was renamed. It's harmless — nothing reads that name — but worth renaming
> next time you're in there, so it stops looking like a second copy of the `XRClassroomAvatarBase`
> prefab.

## Registering an avatar for spawning

At present, `XR Classroom Network Manager` has one **Player Prefab** reference. To make a new avatar
the default network avatar:

1. Open `Assets/Prefabs/Managers/XR Classroom Network Manager.prefab`.
2. Locate the active Netcode configuration's **Player Prefab** field.
3. Assign the new network-avatar prefab.
4. Save the prefab.

A runtime avatar-selection system does not exist yet. Supporting several selectable avatars
will require registering the prefabs with Netcode and synchronizing each player's selection.
Until that is implemented, swap the Player Prefab to test one avatar at a time.

## Test checklist

### In the prefab or a simple scene

- Model scale and orientation are correct.
- No unexpected animations play automatically.
- Mouth slider changes only the mouth.
- Head, body, left hand, and right hand are separate assignable objects.
- Face pieces remain parented to the head object when entering Play mode.

### Offline in the XRClassroom scene

- Avatar appears before connecting.
- Head follows the headset without tipping backward.
- Body follows below the head and turns without spinning near the 0/360-degree boundary.
- Hands follow the controllers if hand objects are assigned.
- Avatar disappears after connecting.
- Avatar reappears after disconnecting.

### With two clients

- Each client sees the other avatar in the correct location.
- Head and hands follow the correct owner.
- Speaking changes the remote avatar's loudness value.
- Mouth closes at silence and opens while speaking.
- Host/local rendering rules do not make remote geometry disappear.
- Voice-driven effects return to their quiet state after speech stops.

## Troubleshooting

### A part points the wrong direction in Play mode

- Confirm **Preserve Authored Rotation Offsets** is checked.
- Confirm the correct object is assigned as **Head Object**, **Body Object**, **Left Hand Object**,
  or **Right Hand Object**.
- Fix the part's local orientation in Blender or adjust the visible object's rotation in Unity,
  then let the component preserve that authored offset.
### Eyes or skull move when the mouth changes

- Manually set the mouth blend shape to both extremes in Unity.
- If Transform values remain unchanged, the blend shape itself contains unintended vertex
  movement.
- Repair the shape key in Blender; scripts cannot reliably distinguish intended mouth vertices.

### Mesh moves but its Transform values do not

- If this happens while a blend shape changes, the movement is inside the mesh vertices rather
  than on the GameObject Transform.
- Repair the shape key in Blender or split that rigid part into its own child GameObject.
- Armature-driven deformation is out of scope for XR Classroom avatars right now.

### Mouth moves backward

Swap **Quiet Weight** and **Loud Weight** on `MouthMotion`.

### Mouth does not move

- Watch `AvatarVoiceSignal.Loudness` in Play mode.
- Confirm Vivox is connected and producing a nonzero value.
- Confirm the event points to `MouthMotion.SetLevel(float)` as a dynamic float call.
- Confirm the blend-shape name exactly matches the imported mesh.
- Confirm the correct renderer is assigned.

### Avatar is visible locally but not remotely

- Confirm the Network Manager's Player Prefab is the intended avatar prefab.
- Do not remove the root `NetworkObject` or pose-replication components.
- Check local-only layers and renderer rules.

## Recommended handoff package from the artist

For each avatar, provide:

- The `.blend` source file.
- Exported FBX.
- Textures and a material-reference sheet.
- Names of the intended head, body, left hand, and right hand objects.
- Name and direction of each blend shape.
- Expected real-world height or head dimensions.
- A screenshot of the correct rest pose.
- Notes about any unusual object parenting or offsets.

That information makes Unity integration dramatically less archaeological.
