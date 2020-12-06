using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class Triangle
    {
        public Point p1;
        public Point p2;
        public Point p3;

        public Point center;
        public bool isActive;
        public Vector3 normale;

        public Triangle(Point p1, Point p2, Point p3)
        {
            Vector3 firstVector = p2.GetPosition() - p1.GetPosition();
            Vector3 secondVector = p3.GetPosition() - p1.GetPosition();
            
            float det = firstVector.x * secondVector.y - firstVector.y * secondVector.x;

            Point temp;
            if (det < 0)
            {
                temp = p2;
                p2 = p3;
                p3 = temp;
            }

            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;

            isActive = true;
            center = new Point(CalculCircleCenter(p1.GetPosition(), p2.GetPosition(), p3.GetPosition()));

            normale = GetNormal();
        }

        private Vector3 GetNormal()
        {
            return Vector3.Cross(p2.GetPosition() - p1.GetPosition(), p3.GetPosition() - p1.GetPosition()).normalized;
        }
        
        private Vector3 CalculCircleCenter(Vector3 aP0, Vector3 aP1, Vector3 aP2)
        {
            // two circle chords
            var v1 = aP1 - aP0;
            var v2 = aP2 - aP0;

            Vector3 normal = Vector3.Cross(v1, v2);
            if (normal.sqrMagnitude < 0.00001f)
                return Vector3.one * float.NaN;
            normal.Normalize();

            // perpendicular of both chords
            var p1 = Vector3.Cross(v1, normal).normalized;
            var p2 = Vector3.Cross(v2, normal).normalized;
            // distance between the chord midpoints
            var r = (v1 - v2) * 0.5f;
            // center angle between the two perpendiculars
            var c = Vector3.Angle(p1, p2);
            // angle between first perpendicular and chord midpoint vector
            var a = Vector3.Angle(r, p1);
            // law of sine to calculate length of p2
            var d = r.magnitude * Mathf.Sin(a * Mathf.Deg2Rad) / Mathf.Sin(c * Mathf.Deg2Rad);
            if (Vector3.Dot(v1, aP2 - aP1) > 0)
                return aP0 + v2 * 0.5f - p2 * d;
            return aP0 + v2 * 0.5f + p2 * d;
        }

        public List<Edge> GetEdges()
        {
            List<Edge> edges = new List<Edge>
            {
                new Edge(p1, p2), 
                new Edge(p2, p3), 
                new Edge(p3, p1)
            };

            return edges;
        }
    }
}