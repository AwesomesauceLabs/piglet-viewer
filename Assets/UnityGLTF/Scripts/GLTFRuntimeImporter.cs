using GLTF.Schema;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityGLTF
{
    public class GLTFRuntimeImporter
    {
        /// <summary>
        /// Set to true when import has finished or
        /// has been aborted by the user.
        /// </summary>
        protected bool _isDone = false;
        /// <summary>
        /// Set to true if the import was aborted
        /// by the user
        /// </summary>
        protected bool _userStopped = false;
        /// <summary>
        /// Directory of GLTF file that is being imported.
        /// </summary>
        protected string _gltfDirectoryPath;
        /// <summary>
        /// Absolute path to GLTF file that is being imported.
        /// </summary>
        protected string _glTFPath = "";
		/// <summary>
		/// Name to assign to the import GLTF model. This variable
		/// is for the name of the root GameObject and also for
		/// the name of the generated .prefab file in GLTFEditorImporter.
		/// </summary>
		protected string _currentSampleName = "";
        /// <summary>
        /// Main .gltf file as a binary blob.
        /// </summary>
        protected byte[] _glTFData;
        /// <summary>
        /// Root JSON node of the main .gltf file.
        /// </summary>
        protected GLTFRoot _root;
        /// <summary>
        /// The number of GLTF nodes that have been loaded.
        /// </summary>
        protected int _nbParsedNodes;
        /// <summary>
        /// Unity game object corresponding to root GLTF node.
        /// </summary>
        protected GameObject _sceneObject;
        /// <summary>
        /// Default Unity Material to use when a GLTF mesh does not
        /// specify a material.
        /// </summary>
        public UnityEngine.Material defaultMaterial;
        /// <summary>
        /// Manages the coroutines for concurrent import tasks
        /// (import images, import meshes, etc.).
        /// </summary>
        protected TaskManager _taskManager;

		/// <summary>
        /// Describes current stage of GLTF import process.
        /// </summary>
		public enum IMPORT_STEP
		{
			READ_FILE,
			BUFFER,
			IMAGE,
			TEXTURE,
			MATERIAL,
			MESH,
			NODE,
			ANIMATION,
			SKIN
		}

		public delegate void RefreshWindow();
		public delegate void ProgressCallback(IMPORT_STEP step, int current, int total);

		protected RefreshWindow _finishCallback;
		protected ProgressCallback _progressCallback;

        /// <summary>
        /// Regex for GTLF data URIs (inline base64-encoded data)
        /// </summary>
		protected const string Base64StringInitializer = "^data:[a-z-]+/[a-z-]+;base64,";

        /// <summary>
        /// Game objects that have been created for imported GLTF objects
        /// (e.g. GLTF nodes, GLTF meshes).
        /// </summary>
		protected Dictionary<int, GameObject> _importedObjects;
		protected Dictionary<int, List<SkinnedMeshRenderer>>_skinIndexToGameObjects;

		protected List<string> _assetsToRemove;

		/// <summary>
		/// Constructor
		/// </summary>
		public GLTFRuntimeImporter()
		{
			Initialize();
		}

		/// <summary>
		/// Initializes all the structures and objects
		/// </summary>
		public void Initialize()
		{
			_importedObjects = new Dictionary<int, GameObject>();
			_skinIndexToGameObjects = new Dictionary<int, List<SkinnedMeshRenderer>>();
			_isDone = true;
			_taskManager = new TaskManager();
			_assetsToRemove = new List<string>();
			defaultMaterial = new UnityEngine.Material(Shader.Find("Standard"));
		}

		/// <summary>
		/// Constructors setting the delegate function to call after each iteration
		/// </summary>
		/// <param name="delegateFunction">The function to call after each iteration (usually Repaint())</param>
		public GLTFRuntimeImporter(ProgressCallback progressCallback, RefreshWindow finish=null)
		{
			_progressCallback = progressCallback;
			_finishCallback = finish;
			Initialize();
		}

		/// <summary>
		/// Setup importer for an import
		/// </summary>
		/// <param name="gltfPath">Absolute path to the glTF file to import</param>
		/// <param name="importPath">Path in current project where to import the model</param>
		/// <param name="modelName">Name of the model prefab to create<param>
		public void setupForPath(string gltfPath, string modelName)
		{
			_glTFPath = gltfPath;
			_gltfDirectoryPath = Path.GetDirectoryName(_glTFPath);
			_currentSampleName = modelName.Length > 0 ? modelName : "GLTFScene";
			_importedObjects.Clear();
			_skinIndexToGameObjects.Clear();
		}
    }
}