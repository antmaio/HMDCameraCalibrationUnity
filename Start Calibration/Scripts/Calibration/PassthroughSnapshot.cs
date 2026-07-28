using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meta.XR;
using PassthroughCameraSamples;
using UnityEngine;

/// <summary>
/// Captures a passthrough camera snapshot (image + pose metadata) on demand.
/// Pose data is expressed relative to each active scene reference frame
/// provided by <see cref="SceneReferenceFrame"/>;
/// </summary>
public class PassthroughSnapshot : MonoBehaviour
{
    /// <summary>Reference to the component that exposes the passthrough camera feed and its pose/intrinsics.</summary>
    [SerializeField] private PassthroughCameraAccess m_cameraAccess;

    // ─── Pose container for a single coordinate space ───────────────────────

    /// <summary>
    /// Holds the position/rotation of the passthrough camera, the headset,
    /// and the XR origin camera, all expressed in one coordinate space
    /// (e.g. relative to a given scene reference frame).
    /// </summary>
    [System.Serializable]
    public class SpacePoseData
    {
        /// <summary>Position of the passthrough (left) camera in this space.</summary>
        public Vector3 left_camera_position;
        /// <summary>Rotation of the passthrough (left) camera in this space.</summary>
        public Quaternion left_camera_rotation;
        /// <summary>Euler-angle representation of <see cref="left_camera_rotation"/>.</summary>
        public Vector3 left_camera_eulerAngles;

        /// <summary>Position of the headset (head node) in this space.</summary>
        public Vector3 head_position;
        /// <summary>Rotation of the headset (head node) in this space.</summary>
        public Quaternion head_rotation;
        /// <summary>Euler-angle representation of <see cref="head_rotation"/>.</summary>
        public Vector3 head_eulerAngles;

        /// <summary>Position of the XR origin camera (if assigned) in this space.</summary>
        public Vector3 xrCamera_position;
        /// <summary>Rotation of the XR origin camera (if assigned) in this space.</summary>
        public Quaternion xrCamera_rotation;
    }

    /// <summary>
    /// Pairs a named scene anchor/reference frame with the pose data computed
    /// relative to it.
    /// </summary>
    [System.Serializable]
    public class SceneSpaceEntry
    {
        /// <summary>Identifying name of the scene anchor this entry is relative to.</summary>
        public string anchorName;
        /// <summary>Pose data expressed relative to the named anchor.</summary>
        public SpacePoseData poseData;
    }

    /// <summary>
    /// Top-level snapshot payload serialized to JSON alongside the captured
    /// image. Contains only scene-space pose data plus camera intrinsics.
    /// </summary>
    [System.Serializable]
    public class SnapshotPoseData
    {
        /// <summary>True if at least one scene reference frame was active and produced an entry in <see cref="sceneSpaces"/>.</summary>
        public bool hasSceneSpaces;
        /// <summary>Pose data for every active scene reference frame at the time of capture.</summary>
        public SceneSpaceEntry[] sceneSpaces = new SceneSpaceEntry[0];

        /// <summary>Pixel resolution of the captured image.</summary>
        public Vector2Int resolution;
        /// <summary>Camera focal length (fx, fy) from the passthrough camera intrinsics.</summary>
        public Vector2 focalLength;
        /// <summary>Camera principal point (cx, cy) from the passthrough camera intrinsics.</summary>
        public Vector2 principalPoint;
    }

    // ─── Inspector ───────────────────────────────────────────────────────────

    /// <summary>If true, pose data is computed relative to each active frame in <see cref="sceneReferenceFrame"/> and stored in <see cref="SnapshotPoseData.sceneSpaces"/>.</summary>
    [Header("Coordinate Space Settings")]
    public bool expressInScene = false;

    /// <summary>Source of the named scene anchors/reference frames used when <see cref="expressInScene"/> is enabled.</summary>
    public SceneReferenceFrame sceneReferenceFrame;

    /// <summary>Optional XR origin camera transform; if assigned, its pose is included in each scene-space entry.</summary>
    [Header("XR Origin Camera (optional)")]
    public Transform xrCamera;

    // ─── Runtime state ───────────────────────────────────────────────────────

    /// <summary>True once passthrough camera permission has been granted and the camera feed is playing.</summary>
    private bool _isReady = false;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Waits for passthrough camera permission and playback to become ready
    /// before allowing snapshots to be taken.
    /// </summary>
    private IEnumerator Start()
    {
        if (m_cameraAccess == null)
        {
            Debug.LogError("PassthroughSnapshot: No PassthroughCameraAccess assigned!");
            yield break;
        }

        while (!OVRPermissionsRequester.IsPermissionGranted(
                   OVRPermissionsRequester.Permission.PassthroughCameraAccess))
        {
            yield return null;
        }

        while (!m_cameraAccess.IsPlaying)
        {
            yield return null;
        }

        _isReady = true;
        Debug.Log("PassthroughSnapshot: Camera ready.");
    }

    /// <summary>Polls for the snapshot input (right controller "A"/One button) each frame.</summary>
    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            TakeSnapshot();

        //Debug.Log($"[PassthroughSnapshot] {m_cameraAccess.GetCameraPose().position} {m_cameraAccess.GetCameraPose().rotation}");
    }

    /// <summary>
    /// Captures a timestamped passthrough image and its corresponding
    /// scene-space pose JSON, writing both to <see cref="Application.persistentDataPath"/>.
    /// Does nothing (and logs an error) if the camera is not yet ready.
    /// </summary>
    public void TakeSnapshot()
    {
        if (!_isReady || !m_cameraAccess.IsPlaying)
        {
            Debug.LogError("PassthroughSnapshot: Camera is not ready.");
            return;
        }

        string timeStamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string basePath = Application.persistentDataPath;

        CaptureImage(timeStamp, basePath);
        CaptureTransform(timeStamp, basePath);
    }

    // ─── Capture helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Reads the current passthrough render target into a <see cref="Texture2D"/>
    /// and writes it to disk as a PNG named "Snapshot_&lt;timeStamp&gt;.png".
    /// </summary>
    /// <param name="timeStamp">Timestamp string used to name the output file.</param>
    /// <param name="basePath">Directory the PNG will be written into.</param>
    private void CaptureImage(string timeStamp, string basePath)
    {
        RenderTexture renderTex = m_cameraAccess.GetTexture() as RenderTexture;
        if (renderTex == null)
        {
            Debug.LogError("PassthroughSnapshot: GetTexture() did not return a RenderTexture.");
            return;
        }

        Vector2Int res = m_cameraAccess.CurrentResolution;
        Texture2D snapshot = new Texture2D(res.x, res.y, TextureFormat.RGB24, false);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = renderTex;
        snapshot.ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0);
        snapshot.Apply();
        RenderTexture.active = prev;

        byte[] bytes = snapshot.EncodeToPNG();
        string path = Path.Combine(basePath, $"Snapshot_{timeStamp}.png");
        File.WriteAllBytes(path, bytes);
        Destroy(snapshot);

        Debug.Log($"<b><color=green>SUCCESS:</color></b> Saved Image → {path}");
    }

    /// <summary>
    /// Builds the <see cref="SnapshotPoseData"/> for the current frame — camera
    /// intrinsics plus one <see cref="SpacePoseData"/> entry per active scene
    /// reference frame — and writes it to disk as "Snapshot_&lt;timeStamp&gt;.json".
    /// </summary>
    /// <param name="timeStamp">Timestamp string used to name the output file.</param>
    /// <param name="basePath">Directory the JSON file will be written into.</param>
    private void CaptureTransform(string timeStamp, string basePath)
    {
        // ── New API: everything lives on the component instance ──
        PassthroughCameraAccess.CameraIntrinsics intrinsics = m_cameraAccess.Intrinsics;
        Pose camPose = m_cameraAccess.GetCameraPose();
        Vector2Int res = m_cameraAccess.CurrentResolution;

        OVRPose headPoseRaw = OVRPlugin.GetNodePoseStateImmediate(
            OVRPlugin.Node.Head).Pose.ToOVRPose();

        Vector3 xrWorldPos = xrCamera != null ? xrCamera.position : Vector3.zero;
        Quaternion xrWorldRot = xrCamera != null ? xrCamera.rotation : Quaternion.identity;

        bool useScene = expressInScene && sceneReferenceFrame != null
                                         && sceneReferenceFrame.IsReady;

        SceneSpaceEntry[] sceneEntries = BuildSceneSpaceEntries(
            useScene, camPose, headPoseRaw, xrWorldPos, xrWorldRot);

        SnapshotPoseData poseData = new SnapshotPoseData
        {
            hasSceneSpaces = sceneEntries.Length > 0,
            sceneSpaces = sceneEntries,

            resolution = res,
            focalLength = intrinsics.FocalLength,
            principalPoint = intrinsics.PrincipalPoint
        };

        string path = Path.Combine(basePath, $"Snapshot_{timeStamp}.json");
        File.WriteAllText(path, JsonUtility.ToJson(poseData, true));

        Debug.Log($"<b><color=green>SUCCESS:</color></b> Saved Pose JSON → {path}");
        DebugLogSnapshot(poseData);
    }

    // ─── Scene space builder ─────────────────────────────────────────────────

    /// <summary>
    /// Computes one <see cref="SceneSpaceEntry"/> per active frame in
    /// <see cref="sceneReferenceFrame"/>, expressing the camera/head/XR-camera
    /// poses relative to each frame.
    /// </summary>
    /// <param name="useScene">If false, returns an empty array without touching <see cref="sceneReferenceFrame"/>.</param>
    /// <param name="camPose">World-space pose of the passthrough camera.</param>
    /// <param name="headPoseRaw">World-space pose of the headset (head node).</param>
    /// <param name="xrWorldPos">World-space position of the XR origin camera.</param>
    /// <param name="xrWorldRot">World-space rotation of the XR origin camera.</param>
    /// <returns>An array of scene-space pose entries, or an empty array if scene expression is disabled/unavailable.</returns>
    private SceneSpaceEntry[] BuildSceneSpaceEntries(
        bool useScene, Pose camPose, OVRPose headPoseRaw,
        Vector3 xrWorldPos, Quaternion xrWorldRot)
    {
        if (!useScene) return new SceneSpaceEntry[0];

        var entries = new List<SceneSpaceEntry>();
        foreach (var kvp in sceneReferenceFrame.ActiveFrames)
        {
            entries.Add(new SceneSpaceEntry
            {
                anchorName = kvp.Key,
                poseData = BuildSpacePoseData(
                    camPose, headPoseRaw, xrWorldPos, xrWorldRot, kvp.Value)
            });
        }
        return entries.ToArray();
    }

    // ─── Pose math ───────────────────────────────────────────────────────────

    /// <summary>
    /// Converts the camera, head, and XR-camera world poses into a single
    /// <see cref="SpacePoseData"/> expressed relative to <paramref name="reference"/>.
    /// </summary>
    /// <param name="camPose">World-space pose of the passthrough camera.</param>
    /// <param name="headPoseRaw">World-space pose of the headset (head node).</param>
    /// <param name="xrWorldPos">World-space position of the XR origin camera.</param>
    /// <param name="xrWorldRot">World-space rotation of the XR origin camera.</param>
    /// <param name="reference">Transform to express all poses relative to (e.g. a scene anchor).</param>
    /// <returns>Pose data for all three tracked points, relative to <paramref name="reference"/>.</returns>
    private SpacePoseData BuildSpacePoseData(
        Pose camPose, OVRPose headPoseRaw,
        Vector3 xrWorldPos, Quaternion xrWorldRot,
        Transform reference)
    {
        GetRelativePose(camPose.position, camPose.rotation,
            reference, out var camPos, out var camRot);
        GetRelativePose(headPoseRaw.position, headPoseRaw.orientation,
            reference, out var headPos, out var headRot);
        GetRelativePose(xrWorldPos, xrWorldRot,
            reference, out var xrPos, out var xrRot);

        return new SpacePoseData
        {
            left_camera_position = camPos,
            left_camera_rotation = camRot,
            left_camera_eulerAngles = camRot.eulerAngles,
            head_position = headPos,
            head_rotation = headRot,
            head_eulerAngles = headRot.eulerAngles,
            xrCamera_position = xrPos,
            xrCamera_rotation = xrRot
        };
    }

    /// <summary>
    /// Transforms a world-space position/rotation into the local space of
    /// <paramref name="reference"/>. If <paramref name="reference"/> is null,
    /// the world-space values are returned unchanged.
    /// </summary>
    /// <param name="worldPos">World-space position to convert.</param>
    /// <param name="worldRot">World-space rotation to convert.</param>
    /// <param name="reference">Transform to express the pose relative to, or null for world space.</param>
    /// <param name="outPos">Resulting position relative to <paramref name="reference"/>.</param>
    /// <param name="outRot">Resulting rotation relative to <paramref name="reference"/>.</param>
    private static void GetRelativePose(
        Vector3 worldPos, Quaternion worldRot, Transform reference,
        out Vector3 outPos, out Quaternion outRot)
    {
        // H->Hf
        if (reference != null)
        {
            outPos = reference.InverseTransformPoint(worldPos);
            outRot = Quaternion.Inverse(reference.rotation) * worldRot;
        }
        else
        {
            outPos = worldPos;
            outRot = worldRot;
        }
    }

    // ─── Debug logging ───────────────────────────────────────────────────────

    /// <summary>Logs a human-readable summary of every captured scene-space pose entry.</summary>
    /// <param name="data">The snapshot pose data produced by <see cref="CaptureTransform"/>.</param>
    private void DebugLogSnapshot(SnapshotPoseData data)
    {
        var sb = new System.Text.StringBuilder();

        if (data.hasSceneSpaces)
            foreach (var entry in data.sceneSpaces)
                AppendSpaceBlock(sb, $"SCENE SPACE — {entry.anchorName}", entry.poseData);

        Debug.Log(sb.ToString());
    }

    /// <summary>Appends a formatted block of pose values for one coordinate space to a log string builder.</summary>
    /// <param name="sb">String builder to append to.</param>
    /// <param name="label">Header label identifying the space (e.g. a scene anchor name).</param>
    /// <param name="d">Pose data for that space.</param>
    private static void AppendSpaceBlock(
        System.Text.StringBuilder sb, string label, SpacePoseData d)
    {
        sb.AppendLine($"<b>── {label} ──</b>");
        sb.AppendLine($"  Head Pos:       {d.head_position:F3}");
        sb.AppendLine($"  Head Rot:       {d.head_eulerAngles:F1}");
        sb.AppendLine($"  Cam Pos:        {d.left_camera_position:F3}");
        sb.AppendLine($"  Cam Rot:        {d.left_camera_eulerAngles:F1}");
        sb.AppendLine($"  XR Camera Pos:  {d.xrCamera_position:F3}");
        sb.AppendLine($"  XR Camera Rot:  {d.xrCamera_rotation.eulerAngles:F1}");
    }
}