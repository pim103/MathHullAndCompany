using System.Collections;
using System.Collections.Generic;
using GrahamScan;
using Jarvis;
using Triangulation;
using UnityEngine;
using Utils;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameObject point;
    [SerializeField] private GameObject segment;

    [SerializeField] private bool is3D;

    [SerializeField] private bool jarvis;
    [SerializeField] private bool graham;
    [SerializeField] private bool incrementalTriangulation;
    [SerializeField] private bool delaunay;
    [SerializeField] private bool voronoi;

    [SerializeField] private int nbPoints = 100;

    private List<Point> currentPointsInScene;
    
    // Start is called before the first frame update
    void Start()
    {
        currentPointsInScene = new List<Point>();

        RunAlgoWithParameter();

//        Edge edge = new Edge
//        {
//            p1 = new Point(Vector3.right),
//            p2 = new Point(Vector3.right * 10)
//        };
        
//        Debug.Log(edge.GetNormal());
//        DrawOneEdge(edge);
    }

    private void RunAlgoWithParameter()
    {
        GeneratePointCloud();

        if (jarvis)
        {
            DrawJarvis();
        }

        if (graham)
        {
            DrawGraham();
        }

        if (incrementalTriangulation)
        {
            DrawIncrementalTriangulation();
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
        }
    }

    private void DrawIncrementalTriangulation()
    {
        IncrementalTriangulation incrementalTriangulation = new IncrementalTriangulation(currentPointsInScene);
        DrawEdge(incrementalTriangulation.ComputeAndGetEdges());
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

    private void DrawEdge(List<Edge> calculatedEdge)
    {
        if (calculatedEdge == null || calculatedEdge.Count == 0)
        {
            return;
        }


        foreach (Edge eachEdge in calculatedEdge)
        {
            DrawOneEdge(eachEdge);
        }
    }

    private void DrawOneEdge(Edge edge)
    {
        bool drawNormale = false;
        GameObject seg = Instantiate(segment);

        Vector3 pos = (edge.p1.GetPosition() + edge.p2.GetPosition())/2;
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

    private void ClearScene()
    {
        while (currentPointsInScene.Count > 0)
        {
            GameObject go = currentPointsInScene[0].GetGameObject();
            go.SetActive(false);
            currentPointsInScene.RemoveAt(0);
        }
    }
}
