using System.Collections;
using System.Collections.Generic;
using Piglet;
using UnityEngine;

namespace PigletViewer
{
    /// <summary>
    /// Store a reference to the most recently loaded glTF model.
    /// This application (PigletViewer) can only load/view a
    /// single model at any given time.
    ///
    /// This class initializes transform of a model when it is
    /// first loaded, so that models always have a standard size,
    /// distance, and rotation relative to the camera.  It
    /// also provides methods for rotating the model about
    /// the center of its bounding box.
    /// </summary>
    public class ModelManager : SingletonBehaviour<ModelManager>
    {
        /// <summary>
        /// Determines the initial position of a model
        /// relative to the camera.
        /// </summary>
        public Vector3 DefaultModelOffsetFromCamera;

        /// <summary>
        /// Determines the default size of a model when it is first
        /// loaded.  More specifically, this sets the length
        /// of the longest dimension of the model's
        /// world-space-axis-aligned bounding box.
        /// </summary>
        public float DefaultModelSize;

        /// <summary>
        /// Reference to the Animation component for the
        /// currently loaded model, which handles storage
        /// and playback of Legacy animation clips.  This
        /// variable will be null if either: (1) no model is
        /// currently loaded, or (2) the currently loaded
        /// model has no animations.
        /// </summary>
        public Animation Animation;

        /// <summary>
        /// The root GameObject of the most recently loaded model,
        /// i.e. the model that is currently being viewed by
        /// the user. (This application only allows viewing one
        /// model at a time.)
        /// </summary>
        private GameObject _model;

        /// <summary>
        /// Unity callback that is invoked once per frame.
        /// </summary>
        void Update()
        {
            SpinModel();
        }

        /// <summary>
        /// Destroy the current model (if any) and update the
        /// current model to the given game object.
        ///
        /// Adjust the root transform of the game object so that the
        /// model has a standard size, distance, and rotation
        /// relative to the camera.
        /// </summary>
        /// <param name="model"></param>
        public void SetModel(GameObject model)
        {
            if (_model != null)
            {
                if (Application.isPlaying)
                    Destroy(_model);
                else
                    DestroyImmediate(_model);
            }

            _model = model;

            // Initialize the transform of the model so that:
            //
            // (1) the model is a standard size
            // (2) the model is a standard distance from the main camera
            // (3) the model is facing the camera

            InitModelTransformRelativeToCamera();

            // Store a reference to the current model's
            // Animation component, which handles storage and
            // playback of Legacy of animation clips. The
            // model will not have an Animation component
            // if the source glTF file did not contain
            // any valid animations.

            Animation = _model.GetComponent<Animation>();

            // Automatically play the default animation clip.
            // (Animation.clip), if any.
            if (Animation != null)
                Animation.Play();
        }

        /// <summary>
        /// Return a reference to the root GameObject of the currently
        /// loaded glTF model. If no model is currently loaded, return
        /// null.
        /// </summary>
        public GameObject GetModel()
        {
            return _model;
        }

        /// <summary>
        /// Auto-rotate model as per "Spin X" / "Spin Y" sliders in GUI.
        /// </summary>
        public void SpinModel()
        {
            Gui gui = Gui.Instance;

            Vector3 rotation = new Vector3(gui.SpinY, -gui.SpinX, 0)
                               * Time.deltaTime * Gui.Instance.SpinSpeed;

            RotateAboutCenter(rotation);
        }

        /// <summary>
        /// Rotate a GameObject hierarchy about its center, as determined
        /// by the MeshRenderer bounds of the GameObjects in the hierarchy.
        /// </summary>
        public void RotateAboutCenter(Vector3 rotation)
        {
            if (_model == null)
                return;

            Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(_model);
            if (!bounds.HasValue)
                return;

            GameObject pivot = new GameObject("pivot");
            pivot.transform.position = bounds.Value.center;
            _model.transform.SetParent(pivot.transform, true);

            pivot.transform.Rotate(rotation);

            _model.transform.SetParent(null, true);
            Destroy(pivot);
        }

        /// <summary>
        /// Adjust the root transform of the model when it is
        /// first loaded, so that every model: (1) has the same
        /// initial size, (2) has the same initial distance from
        /// the camera, and (3) is initially rotated to face the
        /// camera.
        /// </summary>
        public void InitModelTransformRelativeToCamera()
        {
            if (_model == null)
                return;

            Transform cameraTransform = CameraBehaviour.Instance.transform;

            // Scale model up/down to a standard size, so that the
            // largest dimension of its bounding box is equal to `DefaultModelSize`.

            Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(_model);
            if (!bounds.HasValue)
                return;

            float size = bounds.Value.extents.MaxComponent();
            if (size < 0.000001f)
                return;

            Vector3 scale = _model.transform.localScale;
            float scaleFactor = DefaultModelSize / size;
            _model.transform.localScale = scale * scaleFactor;

            // Rotate model to face camera.

            _model.transform.up = cameraTransform.up;
            _model.transform.forward = cameraTransform.forward;

            // Translate model at standard offset from camera.

            bounds = BoundsUtil.GetRendererBoundsForHierarchy(_model);
            if (!bounds.HasValue)
                return;

            _model.transform.Translate(cameraTransform.position
                                       + DefaultModelOffsetFromCamera
                                       - bounds.Value.center);
        }

    }
}