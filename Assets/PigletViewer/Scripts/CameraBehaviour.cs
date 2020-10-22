using Piglet;
using UnityEngine;

namespace PigletViewer
{
    /// <summary>
    /// MonoBehaviour for controlling camera movement in response
    /// to the user's mouse/touch input.
    /// </summary>
    public class CameraBehaviour : SingletonBehaviour<CameraBehaviour>
    {
        public Vector3 DefaultOffsetFromModel;

        /// <summary>
        /// Move the camera as per the given displacement vector.
        /// </summary>
        public void PanCamera(Vector3 pan)
        {
            transform.Translate(pan, Space.Self);
        }

        /// <summary>
        /// Move the camera along the Z-axis, towards/away from the model.
        /// </summary>
        public void ZoomCamera(float deltaZ)
        {
            Vector3 zoom = new Vector3(0, 0, deltaZ);
            transform.Translate(zoom, Space.Self);
        }

        /// <summary>
        /// Reset the camera transform so that it is pointing
        /// towards the model and is positioned at a standard
        /// offset from the model.
        /// </summary>
        public void ResetTransformRelativeToModel()
        {
            var model = ModelManager.Instance.GetModel();
            if (model == null)
                return;

            // rotate camera to face model

            transform.up = model.transform.up ;
            transform.forward = model.transform.forward;

            // position camera at standard offset from model

            var bounds = HierarchyUtil.GetRendererBoundsForHierarchy(model);
            if (!bounds.HasValue)
                return;

            transform.position = bounds.Value.center + DefaultOffsetFromModel;
        }
    }
}
