using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace IncrementalHull3D
{
    public class IncrementalHull3DScript
    {
        private List<Point> points;
        private List<Triangle> faces;
        
        public IncrementalHull3DScript(List<Point> points)
        {
            this.points = points;
            faces = new List<Triangle>();
        }

        private void InitHull()
        {
            if (points.Count > 2)
            {
                Triangle triangle1 = new Triangle(points[0], points[1], points[2]);
                Triangle triangle2 = new Triangle(points[1], points[2], points[3]);
                Triangle triangle3 = new Triangle(points[2], points[3], points[0]);
                Triangle triangle4 = new Triangle(points[3], points[0], points[1]);

                faces.Add(triangle1);
                faces.Add(triangle2);
                faces.Add(triangle3);
                faces.Add(triangle4);
            }

            points.Remove(points[0]);
            points.Remove(points[1]);
            points.Remove(points[2]);
            points.Remove(points[3]);
        }

        public List<Triangle> GetTriangles(Point p)
        {
            List<Triangle> triangleShowingP = new List<Triangle>();
            
            foreach (Triangle t in faces)
            {
                if (t.isActive)
                {
                    float dot = Vector3.Dot(t.normale, p.GetPosition());
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
            return faces.Find(triangle => triangle.p1 == t.p1 && triangle.p2 == t.p2 && triangle.p3 == t.p3);
        }

        private void AddTriangleFromPoint(Triangle t, Point p)
        {
            Triangle triangleFound;
            Triangle t1 = new Triangle(t.p1, t.p2, p);
            Triangle t2 = new Triangle(t.p2, t.p3, p);
            Triangle t3 = new Triangle(t.p3, t.p1, p);

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

            return faces.FindAll(t => t.isActive);
        }
    }
}