using System.Data.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace Manipulation
{
    public class MouseInteraction : MonoBehaviour
    {
        [SerializeField] private Controller controller;
        
        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Camera.main != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hitInfo;

                    if (Physics.Raycast(ray, out hitInfo, 1000, LayerMask.GetMask("grid")))
                    {
                        controller.AddPoint(hitInfo.point);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                controller.RunAlgoWithParameter();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                controller.ClearScene();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                controller.AddTriangle();
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Catmull.CatmullClark.StartSubdivision(controller.meshToSubdivide,1);
            }
        }
    }
}
