using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Keeps players from wandering away. 
/// This one is written by claude, which makes it much more complicated than it needs to be. 
/// What it's doing is basically "if the player touches the boundary, move them back to the spawn point"
/// Unfortunately, networked code is kind of complicated, so this script has a lot of moving parts, 
/// some of which may be important. --Aubrey, August 2026
///
/// Put this on a trigger volume that surrounds everywhere the player is NOT supposed to be
/// (or on the "walls"/"floor" of a room, whichever is easier to author). When the local player's
/// rig enters the trigger, they are moved back to <see cref="respawnPoint"/>.
///
/// Why this is more than "set one transform":
///
/// * There is one XR rig per client and it is a plain scene object, not a networked one. Only the
///   local player should ever be moved, and only that client moves it. We never touch a remote
///   avatar and we never send anything over the network -- the existing pose replication on
///   <c>XRINetworkPlayer</c> carries the new position to everyone else automatically (remotes will
///   see a fast slide rather than a hard cut, which is fine for a rare rescue).
/// * The rig has a <c>CharacterController</c> plus XRI locomotion/gravity. Writing
///   <c>transform.position</c> directly fights the CharacterController and leaves accumulated fall
///   velocity, so the player slams downward on arrival. We go through the same
///   <c>TeleportationProvider</c> the rest of the template uses (see <c>CharacterResetter</c> and
///   <c>MiniGameManager</c>), then clear the fall force once the teleport lands.
/// * Remote avatars replicate as colliders too (hands have SphereColliders). If we reacted to any
///   collider we would rescue ourselves every time someone else reached across the boundary, so we
///   only react to the local rig's CharacterController.
/// * If the player is holding something, a large single-frame jump can fling the item or desync its
///   networked owner. We cancel local grabs first so held items are dropped cleanly instead.
///
/// Setup: this GameObject needs a Collider with Is Trigger enabled, on a layer that collides with
/// the player layer in the Physics matrix.
/// </summary>
public class Boundary : MonoBehaviour
{
    [Tooltip("Where the player is placed when they cross the boundary. Position and yaw are both used.")]
    public Transform respawnPoint;

    [Tooltip("How the rig is oriented on arrival. TargetUpAndForward faces the player the way the " +
             "respawn point faces (matches the rest of the template). WorldSpaceUp only levels them " +
             "out and is less disorienting if you don't care which way they end up facing.")]
    [SerializeField] MatchOrientation orientationMode = MatchOrientation.TargetUpAndForward;

    [Tooltip("Ignore repeat triggers for this long after a return, so lingering in the volume " +
             "doesn't queue a burst of teleports.")]
    [SerializeField] float retriggerCooldownSeconds = 1.0f;

    [Tooltip("Drop anything the player is holding before returning them, instead of dragging it " +
             "across the map with them.")]
    [SerializeField] bool dropHeldItems = true;

    XROrigin m_Origin;
    TeleportationProvider m_TeleportationProvider;
    GravityProvider m_GravityProvider;
    CharacterController m_RigCharacterController;
    XRInteractionManager m_InteractionManager;

    float m_LastReturnTime = -999f;
    bool m_SubscribedToLocomotionEnded;
    bool m_RetryPending;

    void OnTriggerEnter(Collider other)
    {
        TryReturn(other);
    }

    void OnTriggerStay(Collider other)
    {
        // Backup: if the first trigger fired before the rig was ready, or the player is resting
        // against the boundary, keep trying (the cooldown keeps this cheap).
        TryReturn(other);
    }

    void OnDisable()
    {
        UnsubscribeFromLocomotionEnded();
    }

    [ContextMenu("Return Local Player Now")]
    void ReturnLocalPlayerNow()
    {
        if (ResolveRigReferences())
            ReturnPlayer();
        else
            Debug.LogWarning("Boundary: no XR rig found to return.", this);
    }

    void TryReturn(Collider other)
    {
        if (Time.time - m_LastReturnTime < retriggerCooldownSeconds)
            return;

        if (!IsLocalPlayerRig(other))
            return;

        if (respawnPoint == null)
        {
            Debug.LogWarning("Boundary: respawnPoint is not assigned; cannot return the player.", this);
            return;
        }

        if (!ResolveRigReferences())
        {
            // Rig not spawned yet. Try again shortly rather than dropping the rescue.
            if (isActiveAndEnabled && !m_RetryPending)
                StartCoroutine(RetryReturn());
            return;
        }

        ReturnPlayer();
    }

    IEnumerator RetryReturn()
    {
        m_RetryPending = true;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            yield return new WaitForSeconds(0.25f);

            if (ResolveRigReferences())
            {
                ReturnPlayer();
                m_RetryPending = false;
                yield break;
            }
        }

        m_RetryPending = false;
        Debug.LogWarning("Boundary: gave up waiting for an XR rig to return.", this);
    }

    /// <summary>
    /// True only when <paramref name="other"/> belongs to this client's local XR rig. Remote avatars
    /// have colliders but no CharacterController, so they never pass this check.
    /// </summary>
    bool IsLocalPlayerRig(Collider other)
    {
        if (other == null)
            return false;

        CharacterController characterController = other.GetComponentInParent<CharacterController>();
        if (characterController == null)
            return false;

        // If we've already resolved the rig, make sure it's the same CharacterController.
        return m_RigCharacterController == null || characterController == m_RigCharacterController;
    }

    bool ResolveRigReferences()
    {
        if (m_TeleportationProvider != null)
            return true;

        if (m_Origin == null)
            m_Origin = FindFirstObjectByType<XROrigin>();

        if (m_Origin == null)
            return false;

        // The rig's locomotion lives on the XR Origin or a child of it, but be forgiving about the
        // exact layout and fall back to a scene-wide search (matches MiniGameManager).
        m_TeleportationProvider = m_Origin.GetComponentInChildren<TeleportationProvider>();
        if (m_TeleportationProvider == null)
            m_TeleportationProvider = FindFirstObjectByType<TeleportationProvider>();

        m_GravityProvider = m_Origin.GetComponentInChildren<GravityProvider>();
        if (m_GravityProvider == null)
            m_GravityProvider = FindFirstObjectByType<GravityProvider>();

        m_RigCharacterController = m_Origin.GetComponentInParent<CharacterController>();
        if (m_RigCharacterController == null)
            m_RigCharacterController = m_Origin.GetComponentInChildren<CharacterController>();

        if (m_InteractionManager == null)
            m_InteractionManager = FindFirstObjectByType<XRInteractionManager>();

        return m_TeleportationProvider != null;
    }

    void ReturnPlayer()
    {
        if (dropHeldItems)
            DropLocalHeldItems();

        TeleportRequest request = new()
        {
            destinationPosition = respawnPoint.position,
            destinationRotation = respawnPoint.rotation,
            matchOrientation = orientationMode,
        };

        if (!m_TeleportationProvider.QueueTeleportRequest(request))
        {
            Debug.LogWarning("Boundary: TeleportationProvider refused the return request.", this);
            return;
        }

        m_LastReturnTime = Time.time;

        // The teleport is applied by the provider on a later frame. Clear any accumulated fall
        // velocity once it lands so the player doesn't drop hard on arrival.
        if (m_GravityProvider != null)
            SubscribeToLocomotionEnded();
    }

    void DropLocalHeldItems()
    {
        if (m_InteractionManager == null || m_Origin == null)
            return;

        List<IXRSelectInteractor> interactors = new();
        m_Origin.GetComponentsInChildren(true, interactors);

        foreach (IXRSelectInteractor interactor in interactors)
        {
            if (interactor.hasSelection)
                m_InteractionManager.CancelInteractorSelection(interactor);
        }
    }

    void SubscribeToLocomotionEnded()
    {
        if (m_SubscribedToLocomotionEnded)
            return;

        m_TeleportationProvider.locomotionEnded += OnLocomotionEnded;
        m_SubscribedToLocomotionEnded = true;
    }

    void UnsubscribeFromLocomotionEnded()
    {
        if (!m_SubscribedToLocomotionEnded || m_TeleportationProvider == null)
        {
            m_SubscribedToLocomotionEnded = false;
            return;
        }

        m_TeleportationProvider.locomotionEnded -= OnLocomotionEnded;
        m_SubscribedToLocomotionEnded = false;
    }

    void OnLocomotionEnded(LocomotionProvider provider)
    {
        UnsubscribeFromLocomotionEnded();

        if (m_GravityProvider != null)
            m_GravityProvider.ResetFallForce();
    }
}
