using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GrahamScan;
using IncrementalHull3D;
using Jarvis;
using Kobbelt;
using Triangulation;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using Voronoi;
using Edge = Utils.Edge;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameObject point;
    [SerializeField] private GameObject segment;
    [SerializeField] private GameObject voronoiSegment;

    [SerializeField] private GameObject objectToSubDiv;
    [SerializeField] private GameObject targetObjectSubDiv;
    
    [SerializeField] private bool is3D;

    [SerializeField] private bool generatePoints;
    [SerializeField] private bool jarvis;
    [SerializeField] private bool graham;
    [SerializeField] private bool incrementalTriangulationBool;
    [SerializeField] private bool delaunay;
    [SerializeField] private bool voronoi;
    [SerializeField] private bool showCenterDelaunay;
    [SerializeField] private bool subDivLoop;
    [SerializeField] private bool subDivCatmullClark;
    [SerializeField] private bool subDivKobbelt;

    [SerializeField] private int nbPoints = 100;
    
    [SerializeField, Range(1, 4)] int detailsSubDiv = 1;
    [SerializeField] bool weldSubDiv = false;

    public static List<Point> currentPointsInScene;
    private List<GameObject> goInScene;

    private IncrementalTriangulation incrementalTriangulation;

    private static Controller instance;

    private static List<Triangle> triangles;
    
    // Start is called before the first frame update
    void Start()
    {
        currentPointsInScene = new List<Point>();
        goInScene = new List<GameObject>();
        triangles = new List<Triangle>();

        instance = this;
//        RunAlgoWithParameter();
    }

    public void AddTriangle()
    {
        if (currentPointsInScene.Count < 3)
        {
            return;
        }
        
        Point p1 = currentPointsInScene[currentPointsInScene.Count - 3];
        Point p2 = currentPointsInScene[currentPointsInScene.Count - 2];
        Point p3 = currentPointsInScene[currentPointsInScene.Count - 1];
        
        triangles.Add(new Triangle(p1, p2, p3));
    }

    private void CreateTrianglesFromPoint()
    {
        List<Point> pointInTriangle = new List<Point>();
        
        foreach (Point p in currentPointsInScene.ToArray())
        {
            if (pointInTriangle.Contains(p))
            {
                continue;
            }

            IOrderedEnumerable<Point> query = currentPointsInScene.OrderBy(point1 => Vector3.Distance(point1.GetPosition(), p.GetPosition()));

            Point closestP1 = query.ElementAt(1);
            Point closestP2 = query.ElementAt(2);
            
            triangles.Add(new Triangle(p, closestP1, closestP2));
            pointInTriangle.Add(p);
            pointInTriangle.Add(closestP1);
            pointInTriangle.Add(closestP2);
        }
    }
    
    public void RunAlgoWithParameter()
    {
        if (generatePoints)
        {
            ClearScene();
            GeneratePointCloud();
        }

        if (is3D)
        {    
            IncrementalHull3DScript ic3d = new IncrementalHull3DScript(currentPointsInScene);
            List<Triangle> faces = ic3d.Compute3DHull();

            DrawTriangles(faces);
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

            if (subDivLoop)
            {
                SubdivMesh();
            }

            if (subDivCatmullClark)
            {
                Catmull.CatmullClark.StartSubdivision(objectToSubDiv, detailsSubDiv);
            }

            if (subDivKobbelt)
            {
                if (objectToSubDiv == null)
                {
                    KobbeltScript kobbeltScript = new KobbeltScript(triangles);
                    kobbeltScript.ComputeKobbelt();

                    DrawEdge(kobbeltScript.GetEdgesComputed());
                    // DrawTriangles(kobbeltScript.GetTrianglesComputed());
                }
                else
                {
                    // Used in kobbelt scene
                    Mesh mesh = objectToSubDiv.GetComponent<MeshFilter>().mesh;
                    KobbeltScript kobbeltScript = new KobbeltScript(mesh);
                    kobbeltScript.ComputeKobbelt();

                    mesh = DrawNewMesh(kobbeltScript.GetTrianglesComputed());
                }
            }
        }
    }

    private void SubdivMesh()
    {
        MeshFilter filter = objectToSubDiv.GetComponent<MeshFilter>();
        Mesh src = filter.mesh;
        Mesh mesh = LoopSubdiv.LoopSubdivSurfaces.Subdivide(LoopSubdiv.LoopSubdivSurfaces.Weld(src, float.Epsilon, src.bounds.size.x), detailsSubDiv, weldSubDiv, point, true);
        filter.sharedMesh = mesh;
    }

    private Mesh DrawNewMesh(List<Triangle> triangles)
    {
        List<Vector3> addedPoint = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        foreach (Triangle t in triangles)
        {
            int indexP0 = addedPoint.IndexOf(t.p1.GetPosition());
            int indexP1 = addedPoint.IndexOf(t.p2.GetPosition());
            int indexP2 = addedPoint.IndexOf(t.p3.GetPosition());

            if (indexP0 == -1)
            {
                indexP0 = addedPoint.Count;
                addedPoint.Add(t.p1.GetPosition());
            }
            if (indexP1 == -1)
            {
                indexP1 = addedPoint.Count;
                addedPoint.Add(t.p2.GetPosition());
            }
            if (indexP2 == -1)
            {
                indexP2 = addedPoint.Count;
                addedPoint.Add(t.p3.GetPosition());
            }

            if (t.normaleInversed)
            {
                int temp = indexP1;
                indexP1 = indexP2;
                indexP2 = temp;
            }

            newTriangles.Add(indexP0);
            newTriangles.Add(indexP1);
            newTriangles.Add(indexP2);
        }

        MeshFilter filter = targetObjectSubDiv.GetComponent<MeshFilter>();
        Mesh source = new Mesh();

        source.vertices = addedPoint.ToArray();
        source.triangles = newTriangles.ToArray();
        
        source.RecalculateNormals();
        
        filter.mesh = source;

        return source;
    }

    private void GeneratePointCloud()
    {
        for (int i = 0; i < nbPoints; ++i)
        {
            GameObject ob = Instantiate(point);
            ob.name = "Medh" + i + "_PoinG";

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

        if (delaunay || incrementalTriangulationBool)
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

        if (showCenterDelaunay && (delaunay || voronoi))
        {
            if (incrementalTriangulation.centers != null)
            {
                for (int i = 0; i < incrementalTriangulation.centers.Count; ++i)
                {
                    GameObject centerObject = Instantiate(point,incrementalTriangulation.centers[i].GetPosition(),Quaternion.identity);
                    centerObject.name = "Medh" + i + "_center";
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

    private void DrawTriangles(List<Triangle> triangles)
    {
        bool drawNormale = false;
        List<Edge> edges = new List<Edge>();
        
        triangles.ForEach(t =>
        {
            List<Edge> edgesInTriangle = t.GetEdges();
            edgesInTriangle.ForEach(edge =>
            {
                if (edge.FindSameEdges(edges) == null)
                {
                    edges.Add(edge);
                }
            });
            
            
            if (drawNormale)
            {
                Vector3 normale = t.normale;
                GameObject seg = Instantiate(segment);
                goInScene.Add(seg);

                Vector3 pos = t.interCenter.GetPosition();
                seg.transform.rotation = Quaternion.LookRotation(normale, Vector3.up);
                seg.transform.Rotate(Vector3.right * 90);

                pos += normale;
                Vector3 localScale = Vector3.one;
                localScale.y = 2;
                seg.transform.localScale = localScale;

                seg.transform.position = pos;
            }
        });
        
        DrawEdge(edges);

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
        triangles.Clear();
    }

    public static Point AddPoint(Vector3 position)
    {
        GameObject go = Instantiate(instance.point);
        go.name = "Medhi_PointedPoinG";
        go.transform.position = position;
        Point p = new Point(go);

        currentPointsInScene.Add(p);
        instance.goInScene.Add(go);

        return p;
    }
    
    public void AddPoint(Vector3 position, float posZ = 0)
    {
        position.z = posZ;
        GameObject go = Instantiate(point);
        go.name = "Medhi_PointedPoinG";
        go.transform.position = position;
        Point p = new Point(go);

        currentPointsInScene.Add(p);
        goInScene.Add(go);
    }
}