using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace IncrementalHull3D
{
    public class IncrementalHull3DScript
    {
        private List<Point> points;
        private List<Triangle> faces;

        private Point centerPoint;
        
        public IncrementalHull3DScript(List<Point> points)
        {
            this.points = new List<Point>(points);
            faces = new List<Triangle>();
        }

        private void InitHull()
        {
            Point p1 = points.OrderBy(p => p.GetPosition().x).ThenBy(p => p.GetPosition().y).ThenBy(p => p.GetPosition().z).First();
            p1.SetGameObjectName("Point_1");
            points.Remove(p1);

            Point p2 = points.OrderBy(p => p.GetPosition().y).ThenBy(p => p.GetPosition().x).ThenBy(p => p.GetPosition().z).First();
            p2.SetGameObjectName("Point_2");
            points.Remove(p2);

            Point p3 = points.OrderBy(p => p.GetPosition().z).ThenBy(p => p.GetPosition().x).ThenBy(p => p.GetPosition().y).First();
            p3.SetGameObjectName("Point_3");
            points.Remove(p3);

            Point p4 = points.OrderByDescending(p => p.GetPosition().x).ThenByDescending(p => p.GetPosition().y).ThenByDescending(p => p.GetPosition().z).First();
            p4.SetGameObjectName("Point_4");
            points.Remove(p4);

            Point centerPoint = CreateCenterPoint(p1.GetPosition(), p2.GetPosition(), p3.GetPosition(), p4.GetPosition());

            Triangle triangle1 = new Triangle(p1, p2, p3, centerPoint);
            Triangle triangle2 = new Triangle(p2, p3, p4, centerPoint);
            Triangle triangle3 = new Triangle(p3, p4, p1, centerPoint);
            Triangle triangle4 = new Triangle(p4, p1, p2, centerPoint);

            faces.Add(triangle1);
            faces.Add(triangle2);
            faces.Add(triangle3);
            faces.Add(triangle4);
        }

        public Point CreateCenterPoint(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            Vector3 centerPointPos = Vector3.zero;

            centerPointPos.x = (p1.x + p2.x + p3.x + p4.x) / 4;
            centerPointPos.y = (p1.y + p2.y + p3.y + p4.y) / 4;
            centerPointPos.z = (p1.z + p2.z + p3.z + p4.z) / 4;

            centerPoint = Controller.AddPoint(centerPointPos);
            centerPoint.SetGameObjectName("CENTER");

            return centerPoint;
        }

        public List<Triangle> GetTriangles(Point p)
        {
            List<Triangle> triangleShowingP = new List<Triangle>();
            
            foreach (Triangle t in faces)
            {
                if (t.isActive)
                {
                    float dot = Vector3.Dot(t.normale, p.GetPosition() - t.p1.GetPosition());
                    if (dot > 0)
                    {
                        t.isActive = false;
                        triangleShowingP.Add(t);
                    }
                }
            }

            return triangleShowingP;
        }

        private Triangle FindSameTriangle(Triangle t)
        {
            return faces.Find(triangle =>
                (triangle.p1 == t.p1 || triangle.p1 == t.p2 || triangle.p1 == t.p3) &&
                (triangle.p2 == t.p1 || triangle.p2 == t.p2 || triangle.p2 == t.p3) &&
                (triangle.p3 == t.p1 || triangle.p3 == t.p2 || triangle.p3 == t.p3));
        }

        private void AddTriangleFromPoint(Triangle t, Point p)
        {
            Triangle triangleFound;
            Triangle t1 = new Triangle(t.p1, t.p2, p, centerPoint);
            Triangle t2 = new Triangle(t.p2, t.p3, p, centerPoint);
            Triangle t3 = new Triangle(t.p3, t.p1, p, centerPoint);

            if ((triangleFound = FindSameTriangle(t1)) != null)
            {
                triangleFound.isActive = false;
            }
            else
            {
                faces.Add(t1);
            }
            
            if ((triangleFound = FindSameTriangle(t2)) != null)
            {
                triangleFound.isActive = false;
            }
            else
            {
                faces.Add(t2);
            }
            
            if ((triangleFound = FindSameTriangle(t3)) != null)
            {
                triangleFound.isActive = false;
            }
            else
            {
                faces.Add(t3);
            }
        }

        public List<Triangle> Compute3DHull()
        {
            InitHull();

            foreach (Point p in points)
            {
                List<Triangle> visibleFaces = GetTriangles(p);

                if (visibleFaces.Count != 0)
                {
                    foreach (Triangle visibleFace in visibleFaces)
                    {
                        AddTriangleFromPoint(visibleFace, p);
                    }
                }
            }

//            List<Triangle> externFaces = new List<Triangle>();
//
//            faces.ForEach(f =>
//            {
//                
//            });
            
            return faces.FindAll(f => f.isActive);
        }
    }
}