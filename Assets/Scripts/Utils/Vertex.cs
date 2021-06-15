using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{

    public class Vertex
    {
        public Vector3 p;
        public List<Edge3> edges;
        public List<Triangle3> triangles;
        public Vertex updated;

        // reference index to original vertex
        public int index;

        public Vertex(Vector3 p) : this(p, -1)
        {
        }

        public Vertex(Vector3 p, int index, GameObject pointGameObject = null, bool display = false)
        {
            //Debug.Log("Bruh");
            this.p = p;
            this.index = index;
            this.edges = new List<Edge3>();
            this.triangles = new List<Triangle3>();
            /*if (display)
            {
                //GameObject point = Object.Instantiate(pointGameObject);
                //point.transform.position = p;
                //Debug.Log(point.transform.position.ToString());
            }*/
        }

        public void AddEdge(Edge3 e)
        {
            edges.Add(e);
        }

        public void AddTriangle(Triangle3 f)
        {
            triangles.Add(f);
        }

    }

}