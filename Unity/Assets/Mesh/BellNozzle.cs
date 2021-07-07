using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class NozzleData
{
    // wall-angle empirical data
    public const int DataLength = 8;
    public static readonly float[] Ratios = new float[DataLength]   {4f,    5f,    10f,   20f,   30f,   40f,   50f,   100f };
    public static readonly float[] ThetaN60 = new float[DataLength] {20.5f, 20.5f, 16.0f, 14.5f, 14.0f, 13.5f, 13.0f, 11.2f};
    public static readonly float[] ThetaN80 = new float[DataLength] {21.5f, 23.0f, 26.3f, 28.8f, 30.0f, 31.0f, 31.5f, 33.5f};
    public static readonly float[] ThetaN90 = new float[DataLength] {20.0f, 21.0f, 24.0f, 27.0f, 28.5f, 29.5f, 30.2f, 32.0f};
    public static readonly float[] ThetaE60 = new float[DataLength] {26.5f, 28.0f, 32.0f, 35.0f, 36.2f, 37.1f, 35.0f, 40.0f};
    public static readonly float[] ThetaE80 = new float[DataLength] {14.0f, 13.0f, 11.0f,  9.0f,  8.5f,  8.0f,  7.5f,  7.0f};
    public static readonly float[] ThetaE90 = new float[DataLength] {11.5f, 10.5f,  8.0f,  7.0f,  6.5f,  6.0f,  6.0f,  6.0f};
}

public enum NozzleLength
{
    Short = 60,
    Normal = 80,
    Long = 90
}

public class BellNozzle : MonoBehaviour
{
    /// Exit area / Throat area
    [Range(2, 500)]
    public float expansionRatio;
    [Range(1, 10)]
    public float exitRadius;
    public NozzleLength nozzleLength = NozzleLength.Normal;
    public Vector2 positionOffset;
    [Range(2, 50)]
    public int throatVertexNumber = 50;
    [Range(2, 50)]
    public int bellVertexNumber = 50;
    public CurveData curveData;
    [Range(0.001f, 0.1f)]
    public float nozzleThickness = 0.1f;

    public float TotalNozzleLength => bellLength + throatLength;
    private float throatRadius;
    private float eps; // Exit radius / Throat radius
    private float bellLength;
    private float entrantAngle = -135 * Mathf.Deg2Rad; // "typical" value
    private (float thetaN, float thetaE) bellAngles;
    private float exitAngle;
    private float throatLength;

    private void Update()
    {
        eps = Mathf.Sqrt(expansionRatio);
        throatRadius = exitRadius / eps;
        
        FindWallAngles(expansionRatio, throatRadius, (int) nozzleLength);
        // stitch curve sections together
        // external section
        List<Vector2> points = GenerateThroatSection(throatVertexNumber, new Vector2(0, nozzleThickness));
        points.AddRange(GenerateBellSection(bellVertexNumber, new Vector2(0, nozzleThickness)));
        // internal section of the nozzle (0 offset here due to design spec)
        points.AddRange(GenerateBellSectionReverse(bellVertexNumber, Vector2.zero));
        points.AddRange(GenerateThroatSectionReverse(throatVertexNumber, Vector2.zero));
        
        //TODO: remove duplicate vertices
        // should hopefully fix the black sections

        var contour = points.ToArray();

        // add offset before converting to world space and rotating
        for (int i = 0; i < contour.Length; i++)
        {
            contour[i] -= positionOffset;
        }
        
        // finally, generate curve
        curveData = new CurveData(contour, true);
    }

    private void FindWallAngles(float aRatio, float Rt, int lPercentage)
    {
        // choose correct length fraction
        if (lPercentage != 60 && lPercentage != 90)
            lPercentage = 80;
        (float[] thetaNs, float[] thetaEs) = ChooseAngleDataFromLengthFraction(lPercentage);
        
        // calculate absolute nozzle length
        bellLength = (lPercentage / 100f) * ((Mathf.Sqrt(aRatio) - 1) * Rt) / Mathf.Tan(15 * Mathf.Deg2Rad);
        var nearestARatio = float.MaxValue;
        // find the nearest ar in the aratio list
        foreach (float x in NozzleData.Ratios)
        {
            if (Mathf.Abs(aRatio - x) < Mathf.Abs(nearestARatio - aRatio))
                nearestARatio = x;
        }
        
        // if the value at the index is close to input, set it and return
        for (int i = 0; i < NozzleData.Ratios.Length; i++)
        {
            if (Mathf.Round(Mathf.Abs(NozzleData.Ratios[i] - nearestARatio)) > 1) continue;
            
            bellAngles = (thetaNs[i] * Mathf.Deg2Rad, thetaEs[i] * Mathf.Deg2Rad);

            return;
        }
        
        // if no exact match is found, interpolate from 2 nearest elements
        int nearestIndex = Array.IndexOf(NozzleData.Ratios, nearestARatio);
        int l = Math.Max(0, nearestIndex - 1);
        int h = Math.Min(NozzleData.DataLength, nearestIndex + 1);
        bellAngles.thetaE = Interpolate(aRatio, NozzleData.Ratios[l], NozzleData.Ratios[h], thetaNs[l], thetaNs[h]) * Mathf.Deg2Rad;
        bellAngles.thetaN = Interpolate(aRatio, NozzleData.Ratios[l], NozzleData.Ratios[h], thetaEs[l], thetaEs[h]) * Mathf.Deg2Rad;
    }

    private static (float[], float[]) ChooseAngleDataFromLengthFraction(int lPercentage)
    {
        var thetaE = new float[]{};
        switch (lPercentage)
        {
            case 60:
                thetaE = NozzleData.ThetaE60;
                break;
            case 90:
                thetaE = NozzleData.ThetaE90;
                break;
            case 80:
                thetaE = NozzleData.ThetaE80;
                break;
        }
        var thetaN = new float[]{};
        switch (lPercentage)
        {
            case 60:
                thetaN = NozzleData.ThetaN60;
                break;
            case 90:
                thetaN = NozzleData.ThetaN90;
                break;
            case 80:
                thetaN = NozzleData.ThetaN80;
                break;
        }

        return (thetaN, thetaE);
    }
    
    private static float Interpolate(float x, float x0, float x1, float y0, float y1)
    {
        float m = (y1 - y0) / (x1 - x0);
        return y0 + m * (x - x0);
    }

    private List<Vector2> GenerateThroatSection(int vertexNumber, Vector2 offset)
    {
        var v = new List<Vector2>();
        float finalAngle = bellAngles.thetaN - Mathf.PI/2f;

        // entrant section (-135° < θ < -90°)
        foreach (float t in LinSpace(entrantAngle, -Mathf.PI/2f, vertexNumber - 1))
        {
            float x = 1.5f * throatRadius * Mathf.Cos(t);
            float y = 1.5f * throatRadius * Mathf.Sin(t) + 2.5f * throatRadius;
            v.Add(new Vector2(x, y) + offset);
        }
        
        // remove duplicate
        v.RemoveAt(v.Count - 1);
        
        // exit section (-90° < θ < θₙ-90°)
        foreach (float t in LinSpace(-Mathf.PI/2f, finalAngle, vertexNumber - 1))
        {
            float x = 0.382f * throatRadius * Mathf.Cos(t);
            float y = 0.382f * throatRadius * Mathf.Sin(t) + 1.382f * throatRadius;
            v.Add(new Vector2(x, y) + offset);
        }
        
        // remove duplicate
        v.RemoveAt(v.Count - 1);

        throatLength = v.Last().x - v.First().x;
        
        

        return v;
    }

    private List<Vector2> GenerateBellSection(int vertexNumber, Vector2 offset)
    {
        var v = new List<Vector2>(vertexNumber);

        Vector2[] bezier = GenerateBellBezierPoints();

        foreach (float t in LinSpace(0, 1, vertexNumber -1))
        {
            v.Add(PlotBezierPoint(bezier, t) + offset);
        }

        return v;
    }

    /// <summary>
    /// Generate the inside of the nozzle, from exit to throat
    /// </summary>
    /// <param name="vertexNumber"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    private List<Vector2> GenerateThroatSectionReverse(int vertexNumber, Vector2 offset)
    {
        var v = new List<Vector2>(vertexNumber + 1);
        float initialAngle = bellAngles.thetaN - Mathf.PI/2f;
        float finalAngle = entrantAngle;

        // exit section (-90° < θ < θₙ-90°)
        foreach (float t in LinSpace(initialAngle, -Mathf.PI / 2f, vertexNumber -1))
        {
            float x = 0.382f * throatRadius * Mathf.Cos(t);
            float y = 0.382f * throatRadius * Mathf.Sin(t) + 1.382f * throatRadius;
            v.Add(new Vector2(x, y) + offset);
        }
        
        // remove duplicate
        v.RemoveAt(v.Count - 1);

        // entrant section (-135° < θ < -90°)
        foreach (float t in LinSpace(-Mathf.PI/2f, finalAngle, vertexNumber -1))  
        {
            float x = 1.5f * throatRadius * Mathf.Cos(t);
            float y = 1.5f * throatRadius * Mathf.Sin(t) + 2.5f * throatRadius;
            v.Add(new Vector2(x, y) + offset);
        }

        throatLength = v.First().x - v.Last().x;

        return v;
    }
    
    private List<Vector2> GenerateBellSectionReverse(int vertexNumber, Vector2 offset)
    {
        var v = new List<Vector2>(vertexNumber + 1);
        
        Vector2[] bezier = GenerateBellBezierPoints();
        
        foreach (var t in LinSpace(1, 0, vertexNumber -1 ))
        {
            v.Add(PlotBezierPoint(bezier, t) + offset);
        }
        
        // remove duplicate
        v.RemoveAt(v.Count - 1);

        return v;
    }
    
    private Vector2[] GenerateBellBezierPoints()
    {
        // ending point of the throat
        var N = new Vector2(0.382f * throatRadius * Mathf.Cos(bellAngles.thetaN - Mathf.PI / 2f),
            0.382f * throatRadius * Mathf.Sin(bellAngles.thetaN - Mathf.PI / 2f) + 1.382f * throatRadius);
        // exit point
        var E = new Vector2(bellLength, exitRadius);
        // common tangent
        float m1 = Mathf.Tan(bellAngles.thetaN);
        float m2 = Mathf.Tan(bellAngles.thetaE);
        float c1 = N.y - m1 * N.x;
        float c2 = E.y - m2 * E.x;
        var Q = new Vector2((c2 - c1)/(m1 - m2), (m1*c2 - m2*c1) / (m1 - m2));

        return new Vector2[3] {N, Q, E};
    }
    
    private static Vector2 PlotBezierPoint(Vector2[] pts, float t)
    {
        if (pts.Length != 3) return Vector2.zero;
            
        float omt = 1f - t;
        return pts[0] * (omt * omt) +
               pts[1] * (2f * omt * t) +
               pts[2] * (t * t);
    }

    private static IEnumerable<float> LinSpace(float start, float end, int partitions)
    {
        return Enumerable.Range(0, partitions + 1)
            .Select(idx => idx != partitions
                ? start + (end - start) / partitions * idx
                : end);
    }
}
