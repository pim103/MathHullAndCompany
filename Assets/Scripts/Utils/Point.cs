using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class Point
    {
        private Vector3 position;
        private GameObject objectInScene;
        public bool someProp = false;

        public Point(GameObject go)
        {
            objectInScene = go;
            position = go.transform.position;
        }

        public Point(Vector3 pos)
        {
            position = pos;
            objectInScene = null;
        }

        public GameObject GetGameObject()
        {
            return objectInScene;
        }

        public Vector3 GetPosition()
        {
            return position;
        }
        public void SetPosition(float x, float y, float z)
        {
            position.x = x;
            position.y = y;
            position.z = z;

            if (objectInScene)
            {
                objectInScene.transform.position = position;
            }
        }

        public void SetGameObjectName(string name)
        {
            objectInScene.name = name;
        }

        public List<Edge> FindEdgeWithPoint(List<Edge> edges)
        {
            return edges.FindAll(edge =>
                edge.p1.GetPosition() == position || edge.p2.GetPosition() == position);
        }
    }
}