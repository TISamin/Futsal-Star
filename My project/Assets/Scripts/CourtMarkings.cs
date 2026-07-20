using UnityEngine;

/// <summary>
/// Generates court markings (center circle, center line, penalty areas, center dot)
/// at runtime using flat primitives. Attach to an empty GameObject in the Match scene.
/// </summary>
public class CourtMarkings : MonoBehaviour
{
    [Header("Marking Settings")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private float markingY = 0.02f; // Slightly above floor to prevent z-fighting

    [Header("Court Dimensions")]
    [SerializeField] private float courtHalfLength = 21f; // X extent
    [SerializeField] private float courtHalfWidth = 12f;  // Z extent

    [Header("Circle Settings")]
    [SerializeField] private float centerCircleRadius = 4f;
    [SerializeField] private int circleSegments = 48;

    [Header("Penalty Area")]
    [SerializeField] private float penaltyAreaDepth = 6f;
    [SerializeField] private float penaltyAreaHalfWidth = 5f;

    [Header("Visual Styling (Assign these in Inspector)")]
    [SerializeField] private GameObject pitchFloor;
    [SerializeField] private GameObject leftGoal;
    [SerializeField] private GameObject rightGoal;

    private Material lineMaterial;

    private void Start()
    {
        CreateLineMaterial();
        StylePitchAndGoals();
        CreateCenterLine();
        CreateCenterCircle();
        CreateCenterDot();
        CreatePenaltyArea(-courtHalfLength); // Left (Red) goal side
        CreatePenaltyArea(courtHalfLength);  // Right (Blue) goal side
    }

    /// <summary>
    /// Applies colors so they aren't bland. Uses the assigned objects from the Inspector.
    /// </summary>
    private void StylePitchAndGoals()
    {
        // 1. Make the Pitch Futsal Green
        if (pitchFloor != null)
        {
            MeshRenderer mr = pitchFloor.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material pitchMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (pitchMat.shader == null || !pitchMat.shader.isSupported)
                    pitchMat = new Material(Shader.Find("Standard"));
                
                pitchMat.color = new Color(0.15f, 0.45f, 0.2f); // Deep Futsal Green
                pitchMat.SetFloat("_Smoothness", 0.7f); // Slightly reflective hardwood/synthetic feel
                mr.material = pitchMat;
            }
        }

        // 2. Style the Goals (Red/Blue and White Posts)
        if (leftGoal != null) StyleGoal(leftGoal, new Color(0.8f, 0.1f, 0.1f)); // Red inside
        if (rightGoal != null) StyleGoal(rightGoal, new Color(0.1f, 0.3f, 0.8f)); // Blue inside
    }

    private void StyleGoal(GameObject goal, Color netColor)
    {
        MeshRenderer mr = goal.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material goalMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (goalMat.shader == null || !goalMat.shader.isSupported)
                goalMat = new Material(Shader.Find("Standard"));
            
            // Give the goals a distinct color based on the team, or white
            goalMat.color = netColor; 
            mr.material = goalMat;
        }
    }

    private void CreateLineMaterial()
    {
        // Use an unlit material so markings look consistent regardless of lighting
        lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (lineMaterial == null)
        {
            // Fallback for non-URP
            lineMaterial = new Material(Shader.Find("Unlit/Color"));
        }
        lineMaterial.color = lineColor;
    }

    /// <summary>
    /// Full-width center line across the Z axis at X=0.
    /// </summary>
    private void CreateCenterLine()
    {
        GameObject line = CreateFlatQuad("CenterLine");
        line.transform.localScale = new Vector3(lineWidth, 1f, courtHalfWidth * 2f);
        line.transform.localPosition = new Vector3(0f, markingY, 0f);
    }

    /// <summary>
    /// Center circle using a LineRenderer ring.
    /// </summary>
    private void CreateCenterCircle()
    {
        GameObject circleObj = new GameObject("CenterCircle");
        circleObj.transform.SetParent(transform);
        circleObj.transform.localPosition = new Vector3(0f, markingY, 0f);

        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = circleSegments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = lineMaterial;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Vector3[] points = new Vector3[circleSegments];
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = (float)i / circleSegments * Mathf.PI * 2f;
            points[i] = new Vector3(Mathf.Cos(angle) * centerCircleRadius, 0f, Mathf.Sin(angle) * centerCircleRadius);
        }
        lr.SetPositions(points);
    }

    /// <summary>
    /// Small dot at center court.
    /// </summary>
    private void CreateCenterDot()
    {
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dot.name = "CenterDot";
        dot.transform.SetParent(transform);
        dot.transform.localPosition = new Vector3(0f, markingY, 0f);
        dot.transform.localScale = new Vector3(0.4f, 0.01f, 0.4f);

        Collider col = dot.GetComponent<Collider>();
        if (col != null) Destroy(col);

        MeshRenderer mr = dot.GetComponent<MeshRenderer>();
        if (mr != null) mr.material = lineMaterial;
    }

    /// <summary>
    /// Penalty area rectangle near a goal. goalX is ±courtHalfLength.
    /// </summary>
    private void CreatePenaltyArea(float goalX)
    {
        string side = goalX < 0 ? "Left" : "Right";
        float sign = Mathf.Sign(goalX);

        // The penalty area is a rectangle:
        //   - Depth along X from the goal line inward
        //   - Width along Z centered on 0

        float areaX = goalX - sign * penaltyAreaDepth / 2f; // Center of the box along X

        // Bottom line (along X)
        GameObject bottom = CreateFlatQuad($"PenaltyArea_{side}_Bottom");
        bottom.transform.localScale = new Vector3(penaltyAreaDepth, 1f, lineWidth);
        bottom.transform.localPosition = new Vector3(areaX, markingY, -penaltyAreaHalfWidth);

        // Top line (along X)
        GameObject top = CreateFlatQuad($"PenaltyArea_{side}_Top");
        top.transform.localScale = new Vector3(penaltyAreaDepth, 1f, lineWidth);
        top.transform.localPosition = new Vector3(areaX, markingY, penaltyAreaHalfWidth);

        // Front line (along Z, connecting top and bottom, facing the pitch)
        GameObject front = CreateFlatQuad($"PenaltyArea_{side}_Front");
        front.transform.localScale = new Vector3(lineWidth, 1f, penaltyAreaHalfWidth * 2f);
        front.transform.localPosition = new Vector3(goalX - sign * penaltyAreaDepth, markingY, 0f);
    }

    /// <summary>
    /// Creates a flat quad (thin cube) for line segments.
    /// </summary>
    private GameObject CreateFlatQuad(string name)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        quad.name = name;
        quad.transform.SetParent(transform);

        // Remove collider
        Collider col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Make it very flat (Y scale will be overridden per-line but kept thin)
        quad.transform.localScale = new Vector3(1f, 0.01f, 1f);

        MeshRenderer mr = quad.GetComponent<MeshRenderer>();
        if (mr != null) mr.material = lineMaterial;

        return quad;
    }
}
