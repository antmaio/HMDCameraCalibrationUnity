using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws a set of local-space X/Y/Z axis lines (and optionally an origin
/// sphere) as children of this GameObject, so the visualization automatically
/// follows the GameObject's own transform. Drawing happens once, at startup,
/// using whatever transform the GameObject already has.
/// </summary>
public class AxisDrawerForGO : MonoBehaviour
{
    /// <summary>Length, in local units, of each drawn axis line.</summary>
    [Header("Axis Settings")]
    public float axisLength = 1f;

    // ── Sphere settings ───────────────────────────────────────────────────────

    /// <summary>If true, a sphere is drawn at the GameObject's local origin.</summary>
    [Header("Sphere Settings")]
    public bool drawSphere = false;
    /// <summary>Radius of the origin sphere, used when <see cref="drawSphere"/> is true.</summary>
    public float sphereRadius = 0.03f;
    /// <summary>Color of the origin sphere.</summary>
    public Color sphereColor = Color.white;

    /// <summary>Shader used for axis lines and the origin sphere; falls back to "Unlit/Color" if unassigned.</summary>
    [SerializeField] private Shader lineShader;

    /// <summary>True once the axes/sphere have been drawn for this GameObject.</summary>
    private bool _hasDrawn = false;
    /// <summary>Tracks all GameObjects created by this component so they can be cleaned up on redraw.</summary>
    private List<GameObject> _drawnObjects = new List<GameObject>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>Draws the axis visualization using the GameObject's current transform.</summary>
    void Start()
    {
        DrawVisualization();
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the axis lines (and optional origin sphere) as children of this
    /// GameObject in local space. If a visualization already exists, it is
    /// destroyed and rebuilt first.
    /// </summary>
    private void DrawVisualization()
    {
        if (_hasDrawn) CleanupExisting();

        // Sphere at the GO's own origin (local zero).
        if (drawSphere)
            CreateSphere("Origin_Sphere", sphereColor, sphereRadius);

        // Lines along local axes. useWorldSpace = false so they move and rotate
        // with the GO's transform automatically — no manual update ever needed.
        CreateLine("X_Axis", Vector3.right * axisLength, Color.red);
        CreateLine("Y_Axis", Vector3.up * axisLength, Color.green);
        CreateLine("Z_Axis", Vector3.forward * axisLength, Color.blue);

        _hasDrawn = true;

        //Debug.Log($"[AxisDrawerFromGO] Drawn at GO transform — " +
        //          $"pos: {transform.position:F3}, euler: {transform.eulerAngles:F1}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Destroys all previously drawn axis/sphere GameObjects and clears tracking state.</summary>
    private void CleanupExisting()
    {
        foreach (GameObject obj in _drawnObjects)
            if (obj != null) Destroy(obj);
        _drawnObjects.Clear();
        _hasDrawn = false;
    }

    /// <summary>Creates a sphere primitive at this GameObject's local origin.</summary>
    /// <param name="objName">Name to assign to the created sphere GameObject.</param>
    /// <param name="color">Color applied to the sphere's material.</param>
    /// <param name="radius">Radius of the sphere.</param>
    private void CreateSphere(string objName, Color color, float radius)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = objName;
        sphere.transform.SetParent(transform);
        sphere.transform.localPosition = Vector3.zero;   // sits at the GO's origin
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = Vector3.one * (radius * 2f);

        Destroy(sphere.GetComponent<SphereCollider>());

        Shader shader = lineShader != null ? lineShader : Shader.Find("Unlit/Color");
        Material mat = new Material(shader) { color = color };
        sphere.GetComponent<MeshRenderer>().sharedMaterial = mat;

        _drawnObjects.Add(sphere);
    }

    /// <summary>Creates a local-space line, from this GameObject's origin to <paramref name="localEnd"/>, that follows the GameObject's transform.</summary>
    /// <param name="objName">Name to assign to the created line GameObject.</param>
    /// <param name="localEnd">Local-space endpoint of the line (the start is always local zero).</param>
    /// <param name="color">Color applied to the line's material.</param>
    private void CreateLine(string objName, Vector3 localEnd, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;                        // local space — follows GO
        lr.positionCount = 2;
        lr.SetPositions(new Vector3[] { Vector3.zero, localEnd });
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;

        Shader shader = lineShader != null ? lineShader : Shader.Find("Unlit/Color");
        lr.material = new Material(shader) { color = color };

        _drawnObjects.Add(obj);
    }
}