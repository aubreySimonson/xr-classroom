using UnityEngine;

namespace XRClassroom
{
    /// <summary>
    /// Written by a mix of codex and Aubrey (mostly codex), summer 2026
    /// A deliberately simple world-space mirror.
    /// Add this to a flat visible object such as a Quad. The quad's forward direction is
    /// treated as the mirror normal.
    /// 
    /// Still more complicated than it needs to be but w/e
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class SimpleWorldMirror : MonoBehaviour
    {
        const int k_TextureSize = 1024;
        const float k_NearClipPlane = 0.03f;

        Renderer m_Renderer;
        Material m_RuntimeMaterial;
        RenderTexture m_RenderTexture;
        Camera m_MirrorCamera;

        void OnEnable()
        {
            m_Renderer = GetComponent<Renderer>();
            CreateMirrorResources();
        }

        void OnDisable()
        {
            ReleaseMirrorResources();
        }

        void LateUpdate()
        {
            Camera sourceCamera = Camera.main;
            if (sourceCamera == null || m_MirrorCamera == null || m_RenderTexture == null)
                return;

            UpdateMirrorCameraFrom(sourceCamera);

            bool rendererWasEnabled = m_Renderer.enabled;
            m_Renderer.enabled = false;
            m_MirrorCamera.Render();
            m_Renderer.enabled = rendererWasEnabled;
        }

        void CreateMirrorResources()
        {
            m_RenderTexture = new RenderTexture(k_TextureSize, k_TextureSize, 16)
            {
                name = $"{name} Reflection Texture",
                hideFlags = HideFlags.HideAndDontSave
            };

            GameObject cameraObject = new GameObject($"{name} Reflection Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            m_MirrorCamera = cameraObject.AddComponent<Camera>();
            m_MirrorCamera.enabled = false;
            m_MirrorCamera.targetTexture = m_RenderTexture;
            m_MirrorCamera.nearClipPlane = k_NearClipPlane;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");//try to use URP unlit, assuming we're using URP
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader != null)
            {
                m_RuntimeMaterial = new Material(shader);
            }
            else if (m_Renderer.sharedMaterial != null)
            {
                m_RuntimeMaterial = new Material(m_Renderer.sharedMaterial);
            }
            else
            {
                Debug.LogError("SimpleWorldMirror could not find a shader or material to display the reflection.", this);
                enabled = false;
                return;
            }

            m_RuntimeMaterial.name = $"{name} Mirror Material";
            m_RuntimeMaterial.hideFlags = HideFlags.HideAndDontSave;
            SetMaterialTexture(m_RuntimeMaterial, m_RenderTexture);

            m_Renderer.material = m_RuntimeMaterial;
        }

        void ReleaseMirrorResources()
        {
            if (m_MirrorCamera != null)
                Destroy(m_MirrorCamera.gameObject);

            if (m_RenderTexture != null)
                m_RenderTexture.Release();

            if (m_RuntimeMaterial != null)
                Destroy(m_RuntimeMaterial);
            if (m_RenderTexture != null)
                Destroy(m_RenderTexture);
        }

        void UpdateMirrorCameraFrom(Camera sourceCamera)
        {
            Vector3 mirrorPosition = transform.position;
            Vector3 mirrorNormal = transform.forward;

            Transform sourceTransform = sourceCamera.transform;
            Vector3 reflectedPosition = ReflectPoint(sourceTransform.position, mirrorPosition, mirrorNormal);
            Vector3 reflectedForward = Vector3.Reflect(sourceTransform.forward, mirrorNormal);
            Vector3 reflectedUp = Vector3.Reflect(sourceTransform.up, mirrorNormal);

            m_MirrorCamera.transform.SetPositionAndRotation(
                reflectedPosition,
                Quaternion.LookRotation(reflectedForward, reflectedUp));

            m_MirrorCamera.fieldOfView = sourceCamera.fieldOfView;
            m_MirrorCamera.aspect = 1f;
            m_MirrorCamera.farClipPlane = sourceCamera.farClipPlane;
            m_MirrorCamera.clearFlags = sourceCamera.clearFlags;
            m_MirrorCamera.backgroundColor = sourceCamera.backgroundColor;
            m_MirrorCamera.cullingMask = sourceCamera.cullingMask;
        }

        static Vector3 ReflectPoint(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
        {
            float distance = Vector3.Dot(point - planePoint, planeNormal);
            return point - 1f * distance * planeNormal;
        }

        static void SetMaterialTexture(Material material, Texture texture)
        {
            material.mainTexture = texture;

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
        }
    }
}
