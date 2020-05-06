using Piglet;
using UnityEngine;

namespace PigletViewer
{
    public class CameraBehaviour : Singleton<CameraBehaviour>
    {
        /// <summary>
        /// Move the camera as per the given displacement vector.
        /// </summary>
        public void PanCamera(Vector3 pan)
        {
            transform.Translate(pan * GameManager.Instance.MousePanSpeed,
                Space.Self);
        }

        /// <summary>
        /// Move the camera along the Z-axis, towards/away from the model.
        /// </summary>
        public void ZoomCamera(float deltaZ)
        {
            Vector3 zoom = new Vector3(0, 0, deltaZ);
            transform.Translate(zoom * GameManager.Instance.MouseZoomSpeed,
                Space.Self);
        }
    }
}
