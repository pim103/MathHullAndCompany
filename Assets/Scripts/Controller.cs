using System.Collections;
using System.Collections.Generic;
using GrahamScan;
using IncrementalHull3D;
using Jarvis;
using Triangulation;
using UnityEngine;
using Utils;
using Voronoi;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameObject point;
    [SerializeField] private GameObject segment;
    [SerializeField] private GameObject voronoiSegment;

    [SerializeField] private bool is3D;

    [SerializeField] private bool generatePoints;
    [SerializeField] private bool jarvis;
    [SerializeField] private bool graham;
    [SerializeField] private bool incrementalTriangulationBool;
    [SerializeField] private bool delaunay;
    [SerializeField] private bool voronoi;

    [SerializeField] private int nbPoints = 100;

    public static List<Point> currentPointsInScene;
    private List<GameObject> goInScene;

    private IncrementalTriangulation incrementalTriangulation;
    
    // Start is called before the first frame update
    void Start()
    {
        currentPointsInScene = new List<Point>();
        goInScene = new List<GameObject>();

//        RunAlgoWithParameter();
    }

    public void RunAlgoWithParameter()
    {
        if (generatePoints)
        {
            ClearScene();
            GeneratePointCloud();
//            AddPoint(Vector3.zero + Vector3.right * -34 + Vector3.up * -17, 0);
//            AddPoint(Vector3.zero + Vector3.right * -5 + Vector3.up * -30, 5);
//            AddPoint(Vector3.zero + Vector3.right * 1 + Vector3.up * -11, 15);
//            AddPoint(Vector3.zero + Vector3.right * -15 + Vector3.up * 15, -20);
        }

        if (is3D)
        {    
            IncrementalHull3DScript ic3d = new IncrementalHull3DScript(currentPointsInScene);
            List<Triangle> faces = ic3d.Compute3DHull();

            foreach (Triangle face in faces)
            {
                DrawEdge(face.GetEdges());
            }
        }
        else
        {
            if (jarvis)
            {
                DrawJarvis();
            }

            if (graham)
            {
                DrawGraham();
            }

            if (incrementalTriangulationBool || delaunay || voronoi)
            {
                DrawIncrementalTriangulation();
            }
        }   
    }

    private void GeneratePointCloud()
    {
        for (int i = 0; i < nbPoints; ++i)
        {
            GameObject ob = Instantiate(point);

            Vector3 pos = new Vector3();
            pos.x = Random.Range(-50, 50);
            pos.y = Random.Range(-50, 50);
            pos.z = is3D ? Random.Range(-50, 50) : 0;

            if (currentPointsInScene.Exists(p => p.GetPosition() == pos))
            {
                --i;
                continue;
            }

            ob.transform.position = pos;
            currentPointsInScene.Add(new Point(ob));
            goInScene.Add(ob);
        }
    }

    private void DrawIncrementalTriangulation()
    {
        incrementalTriangulation = new IncrementalTriangulation(currentPointsInScene);
        if (delaunay || voronoi)
        {
            incrementalTriangulation.flipping = true;
        }

        List<Edge> delaunayEdges = incrementalTriangulation.ComputeAndGetEdges();

        if (delaunay)
        {
            DrawEdge(delaunayEdges);
        }

        if (voronoi)
        {
            VoronoiScript v = new VoronoiScript(incrementalTriangulation.triangles, incrementalTriangulation.edges);
            DrawEdge(v.ComputeAndGetEdges(), true);
        }
    }
    
    private void DrawJarvis()
    {
        JarvisScript jarvis = new JarvisScript(currentPointsInScene);
        DrawSegment(jarvis.ComputeAndPrintJarvis());
    }
    
    private void DrawGraham()
    {
        GrahamScanScript graham = new GrahamScanScript(currentPointsInScene);
        DrawSegment(graham.ComputeAndDisplayGraham());
    }

    private void DrawSegment(List<Point> calculatedPoint)
    {
        if (calculatedPoint == null || calculatedPoint.Count == 0)
        {
            return;
        }

        for (int i = 0; i < calculatedPoint.Count; ++i)
        {
            GameObject seg = Instantiate(segment);
            goInScene.Add(seg);

            int nextIndex = (i + 1) % calculatedPoint.Count;

            Vector3 pos = (calculatedPoint[i].GetPosition() + calculatedPoint[nextIndex].GetPosition())/2;
            seg.transform.rotation = Quaternion.LookRotation(calculatedPoint[i].GetPosition() - calculatedPoint[nextIndex].GetPosition(), Vector3.up);
            seg.transform.Rotate(Vector3.right * 90);

            Vector3 localScale = Vector3.one;
            localScale.y = Vector3.Distance(calculatedPoint[i].GetPosition(), calculatedPoint[nextIndex].GetPosition()) / 2;
            seg.transform.localScale = localScale;

            seg.transform.position = pos;
        }
    }

    private void DrawEdge(List<Edge> calculatedEdge, bool drawWithVoronoiColor = false, int offsetZ = 0)
    {
        if (calculatedEdge == null || calculatedEdge.Count == 0)
        {
            return;
        }

        if (delaunay)
        {
            if (incrementalTriangulation.centers != null)
            {
                for (int i = 0; i < incrementalTriangulation.centers.Count; ++i)
                {
                    GameObject centerObject = Instantiate(point,incrementalTriangulation.centers[i].GetPosition(),Quaternion.identity);
                    goInScene.Add(centerObject);
                    //centerObject.GetComponent<Material>().color = Color.blue;
                    centerObject.AddComponent<CircleCollider2D>();
                    CircleCollider2D circleCollider2D = centerObject.GetComponent<CircleCollider2D>();

                    circleCollider2D.radius = incrementalTriangulation.radiuses[i];
                }
            }
        }

        foreach (Edge eachEdge in calculatedEdge)
        {
            DrawOneEdge(eachEdge, drawWithVoronoiColor, offsetZ);
        }
    }

    private void DrawOneEdge(Edge edge, bool drawWithVoronoiColor = false, int offsetZ = 0)
    {
        bool drawNormale = false;

        GameObject seg;
        if (drawWithVoronoiColor)
        {
            seg = Instantiate(voronoiSegment);
        }
        else
        {
            seg = Instantiate(segment);
        }

        goInScene.Add(seg);

        Vector3 pos = (edge.p1.GetPosition() + edge.p2.GetPosition())/2;

        pos.z += offsetZ;

        seg.transform.rotation = Quaternion.LookRotation(edge.p1.GetPosition() - edge.p2.GetPosition(), Vector3.up);
        seg.transform.Rotate(Vector3.right * 90);

        Vector3 localScale = Vector3.one;
        localScale.y = Vector3.Distance(edge.p1.GetPosition(), edge.p2.GetPosition()) / 2;
        seg.transform.localScale = localScale;

        seg.transform.position = pos;

        if (drawNormale)
        {
            Vector3 normale = edge.GetNormal();
            seg = Instantiate(segment);

            pos = (edge.p1.GetPosition() + edge.p2.GetPosition())/2;
            seg.transform.rotation = Quaternion.LookRotation(normale, Vector3.up);
            seg.transform.Rotate(Vector3.right * 90);

            pos += normale;
            localScale = Vector3.one;
            localScale.y = 2;
            seg.transform.localScale = localScale;

            seg.transform.position = pos;
        }
    }

    public void ClearScene()
    {
        while (currentPointsInScene.Count > 0)
        {
            GameObject go = currentPointsInScene[0].GetGameObject();
            go.SetActive(false);
            currentPointsInScene.RemoveAt(0);
        }

        while (goInScene.Count > 0)
        {
            Destroy(goInScene[0]);
            goInScene.RemoveAt(0);
        }
        
        currentPointsInScene.Clear();
        goInScene.Clear();
    }

    public void AddPoint(Vector3 position, float posZ = 0)
    {
        position.z = posZ;
        GameObject go = Instantiate(point);
        go.transform.position = position;
        Point p = new Point(go);

        currentPointsInScene.Add(p);
        goInScene.Add(go);
    }
}