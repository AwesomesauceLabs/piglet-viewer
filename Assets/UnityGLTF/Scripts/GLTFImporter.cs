using GLTF;
using GLTF.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
	public class GLTFImporter
	{
		public bool _useGLTFMaterial = false;

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
		/// Caches data (e.g. buffers) in memory during import.
		/// </summary>
		protected GLTFRuntimeImporterCache _assetCache;


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
		public GLTFImporter()
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
		public GLTFImporter(ProgressCallback progressCallback, RefreshWindow finish=null)
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

		/// <summary>
		/// Pump import tasks and check status of GLTF import.
		/// </summary>
		public void Update()
		{
			if(!_isDone)
			{
				if (_userStopped)
				{
					_userStopped = false;
					Clear();
					_isDone = true;
				}
				else
				{
					if (_taskManager != null && _taskManager.play())
					{
						// Do stuff
					}
					else
					{
						_isDone = true;
						finishImport();
					}
				}
			}
		}

		/// <summary>
		/// Abort all currently running import tasks (coroutines).
		/// </summary>
		virtual public void Clear()
		{
			_taskManager.clear();
		}

		/// <summary>
		/// Run user-specified callback after finishing GLTF import.
		/// </summary>
		virtual protected void finishImport()
		{
			if (_finishCallback != null)
				_finishCallback();
		}

		/// <summary>
		/// Call this to abort current import
		/// </summary>
		public void abortImport()
		{
			if (!_isDone)
			{
				_userStopped = true;
			}
		}

		/// <summary>
		/// Start the import process.
		/// </summary>
		/// <param name="useGLTFMaterial"></param>
		public void Load(bool useGLTFMaterial=false)
		{
			_isDone = false;
			_userStopped = false;
			_useGLTFMaterial = useGLTFMaterial;
			LoadFile();
			LoadGLTFScene();
		}

		/// <summary>
		/// Load contents of main GLTF file into memory.
		/// </summary>
		/// <param name="sceneIndex"></param>
		protected void LoadFile(int sceneIndex = -1)
		{
			_glTFData = File.ReadAllBytes(_glTFPath);
			_root = GLTFParser.ParseJson(_glTFData);
		}

		/// <summary>
		/// Import a scene from the GLTF file.
		/// </summary>
		/// <param name="sceneIndex"></param>
		protected void LoadGLTFScene(int sceneIndex = -1)
		{
			Scene scene;
			if (sceneIndex >= 0 && sceneIndex < _root.Scenes.Count)
			{
				scene = _root.Scenes[sceneIndex];
			}
			else
			{
				scene = _root.GetDefaultScene();
			}

			if (scene == null)
			{
				throw new Exception("No default scene in gltf file.");
			}

			_assetCache = new GLTFRuntimeImporterCache();

			// Load dependencies
			LoadBuffersEnum();
			if (_root.Images != null)
				LoadImagesEnum();
			if (_root.Textures != null)
				SetupTexturesEnum();
			if (_root.Materials != null)
				LoadMaterialsEnum();
			LoadMeshesEnum();
			LoadSceneEnum();

			if (_root.Animations != null && _root.Animations.Count > 0)
				LoadAnimationsEnum();

			if (_root.Skins != null && _root.Skins.Count > 0)
				LoadSkinsEnum();
		}

		protected void LoadBuffersEnum()
		{
			_taskManager.addTask(LoadBuffers());
		}

		protected void LoadImagesEnum()
		{
			_taskManager.addTask(LoadImages());
		}

		protected void SetupTexturesEnum()
		{
			_taskManager.addTask(SetupTextures());
		}

		protected void LoadMaterialsEnum()
		{
			_taskManager.addTask(LoadMaterials());
		}

		protected void LoadMeshesEnum()
		{
			_taskManager.addTask(LoadMeshes());
		}

		protected void LoadSceneEnum()
		{
			_taskManager.addTask(LoadScene());
		}

		protected void LoadAnimationsEnum()
		{
			_taskManager.addTask(LoadAnimations());
		}

		protected void LoadSkinsEnum()
		{
			_taskManager.addTask(LoadSkins());
		}

		protected void setProgress(IMPORT_STEP step, int current, int total)
		{
			if (_progressCallback != null)
				_progressCallback(step, current, total);
		}

		virtual protected void AddImage(Texture2D image)
		{
			_assetCache.Images.Add(image);
		}

		virtual protected void AddTexture(Texture2D image)
		{
			_assetCache.Textures.Add(image);
		}

		virtual protected void AddMaterial(UnityEngine.Material material)
		{
			_assetCache.Materials.Add(material);
		}

		virtual protected void AddMesh()
		{
			_assetCache.Meshes.Add(new List<KeyValuePair<UnityEngine.Mesh, UnityEngine.Material>>());
		}

		virtual protected void AddMeshPrimitive(
			UnityEngine.Mesh primitive, UnityEngine.Material material)
		{
			List<KeyValuePair<UnityEngine.Mesh, UnityEngine.Material>>
				mesh = _assetCache.Meshes[_assetCache.Meshes.Count - 1];

			mesh.Add(new KeyValuePair<UnityEngine.Mesh, UnityEngine.Material>(
				primitive, material));
		}

		protected IEnumerator LoadBuffers()
		{
			if (_root.Buffers != null)
			{
				// todo add fuzzing to verify that buffers are before uri
				setProgress(IMPORT_STEP.BUFFER, 0, _root.Buffers.Count);
				for (int i = 0; i < _root.Buffers.Count; ++i)
				{
					GLTF.Schema.Buffer buffer = _root.Buffers[i];
					if (buffer.Uri != null)
					{
						_assetCache.Buffers.Add(LoadBuffer(_gltfDirectoryPath, buffer, i));
					}
					else //null buffer uri indicates GLB buffer loading
					{
						byte[] glbBuffer;
						GLTFParser.ExtractBinaryChunk(_glTFData, i, out glbBuffer);
						_assetCache.Buffers.Add(glbBuffer);
					}
					setProgress(IMPORT_STEP.BUFFER, (i + 1), _root.Buffers.Count);
					yield return null;
				}
			}
		}

		protected byte[] LoadBuffer(string sourceUri, GLTF.Schema.Buffer buffer, int bufferIndex)
		{
			if (buffer.Uri == null)
				return null;

			var uri = buffer.Uri;
			var bufferPath = Path.Combine(sourceUri, uri);
			return File.ReadAllBytes(bufferPath);
		}

		protected IEnumerator LoadImages()
		{
			for (int i = 0; i < _root.Images.Count; ++i)
			{
				Image gltfImage = _root.Images[i];
				Texture2D image = LoadImage(_gltfDirectoryPath, gltfImage, i);
				AddImage(GLTFTextureUtils.flipTexture(image));
				setProgress(IMPORT_STEP.IMAGE, (i + 1), _root.Images.Count);
				yield return null;
			}
		}

		protected Texture2D LoadImage(string rootPath, Image image, int imageID)
		{
			// Note: Initial texture size does not matter,
			// as the size will be updated by Texture2D.LoadImage().
			var texture = new Texture2D(1, 1);

			if (image.Uri != null)
			{
				// Is base64 uri ?
				var uri = image.Uri;

				Regex regex = new Regex(Base64StringInitializer);
				Match match = regex.Match(uri);
				if (match.Success)
				{
					var base64Data = uri.Substring(match.Length);
					var textureData = Convert.FromBase64String(base64Data);
					texture.LoadImage(textureData);
					return texture;
				}
				else if(File.Exists(Path.Combine(rootPath, uri))) // File is a real file
				{
					string imagePath = Path.Combine(rootPath, uri);
					var textureData = File.ReadAllBytes(imagePath);
					texture.LoadImage(textureData);
					return texture;
				}
				else
				{
					Debug.Log("Image not found / Unknown image buffer");
					return null;
				}
			}
			else
			{
				var bufferView = image.BufferView.Value;
				var buffer = bufferView.Buffer.Value;
				var data = new byte[bufferView.ByteLength];

				var bufferContents = _assetCache.Buffers[bufferView.Buffer.Id];
				System.Buffer.BlockCopy(bufferContents, bufferView.ByteOffset, data, 0, data.Length);
				texture.LoadImage(data);
				return texture;
			}
		}

		protected IEnumerator SetupTextures()
		{
			for(int i = 0; i < _root.Textures.Count; ++i)
			{
				Texture2D texture = SetupTexture(_root.Textures[i], i);
				AddTexture(texture);
				setProgress(IMPORT_STEP.TEXTURE, (i + 1), _root.Textures.Count);
				yield return null;
			}
		}

		protected Texture2D SetupTexture(GLTF.Schema.Texture def, int textureIndex)
		{
			Texture2D image = getImage(def.Source.Id);
			Texture2D texture = TextureUtil.DuplicateTexture(image);

			// Default values
			var desiredFilterMode = FilterMode.Bilinear;
			var desiredWrapMode = UnityEngine.TextureWrapMode.Repeat;

			if (def.Sampler != null)
			{
				var sampler = def.Sampler.Value;
				switch (sampler.MinFilter)
				{
					case MinFilterMode.Nearest:
						desiredFilterMode = FilterMode.Point;
						break;
					case MinFilterMode.Linear:
					default:
						desiredFilterMode = FilterMode.Bilinear;
						break;
				}

				switch (sampler.WrapS)
				{
					case GLTF.Schema.WrapMode.ClampToEdge:
						desiredWrapMode = UnityEngine.TextureWrapMode.Clamp;
						break;
					case GLTF.Schema.WrapMode.Repeat:
					default:
						desiredWrapMode = UnityEngine.TextureWrapMode.Repeat;
						break;
				}
			}

			texture.filterMode = desiredFilterMode;
			texture.wrapMode = desiredWrapMode;

			return texture;
		}

		virtual protected Texture2D getImage(int index)
		{
			return _assetCache.Images[index];
		}

		virtual protected Texture2D getTexture(int index)
		{
			return _assetCache.Textures[index];
		}

		virtual protected UnityEngine.Material getMaterial(int index)
		{
			return _assetCache.Materials[index];
		}

		virtual protected List<KeyValuePair<UnityEngine.Mesh, UnityEngine.Material>>
		getMesh(int meshIndex)
		{
			return _assetCache.Meshes[meshIndex];
		}

		virtual protected KeyValuePair<UnityEngine.Mesh, UnityEngine.Material>
		getMeshPrimitive(int meshIndex, int primitiveIndex)
		{
			return _assetCache.Meshes[meshIndex][primitiveIndex];
		}

		public GameObject GetSceneObject()
		{
			return _sceneObject;
		}

		protected IEnumerator LoadMaterials()
		{
			for(int i = 0; i < _root.Materials.Count; ++i)
			{
				UnityEngine.Material material = CreateUnityMaterial(_root.Materials[i], i);
				AddMaterial(material);
				setProgress(IMPORT_STEP.MATERIAL, (i + 1), _root.Materials.Count);
				yield return null;
			}
		}

		virtual protected UnityEngine.Material CreateUnityMaterial(GLTF.Schema.Material def, int materialIndex)
		{
			Extension specularGlossinessExtension = null;
			bool isSpecularPBR = def.Extensions != null && def.Extensions.TryGetValue("KHR_materials_pbrSpecularGlossiness", out specularGlossinessExtension);

			if (specularGlossinessExtension != null)
				Debug.LogFormat("material {0}: pbr = SpecularGlossiness", materialIndex);
			else if (def.PbrMetallicRoughness != null)
				Debug.LogFormat("material {0}: pbr = MetallicRoughness", materialIndex);
			else
				Debug.LogFormat("material {0}: pbr = None", materialIndex);

			Shader shader = isSpecularPBR ? Shader.Find("Standard (Specular setup)") : Shader.Find("Standard");

			var material = new UnityEngine.Material(shader);
			material.hideFlags = HideFlags.DontUnloadUnusedAsset; // Avoid material to be deleted while being built
			material.name = def.Name;

			Debug.LogFormat("material {0}: name = (\"{1}\")", materialIndex, material.name);
			Debug.LogFormat("material {0}: pbr = {1}", materialIndex, isSpecularPBR ? "specular" : "metallic");
			Debug.LogFormat("material {0}: AlphaMode = {1}", materialIndex, def.AlphaMode);

			//Transparency
			if (def.AlphaMode == AlphaMode.MASK)
			{
				GLTFMaterialUtils.SetupMaterialWithBlendMode(material, GLTFMaterialUtils.BlendMode.Cutout);
				material.SetFloat("_Mode", 1);
				material.SetFloat("_Cutoff", (float)def.AlphaCutoff);
			}
			else if (def.AlphaMode == AlphaMode.BLEND)
			{
				GLTFMaterialUtils.SetupMaterialWithBlendMode(material, GLTFMaterialUtils.BlendMode.Fade);
				material.SetFloat("_Mode", 3);
			}

			Debug.LogFormat("material {0}: def.NormalTexture? {1}", materialIndex, def.NormalTexture != null);

			if (def.NormalTexture != null)
			{
				var texture = def.NormalTexture.Index.Id;
				Texture2D normalTexture = getTexture(texture) as Texture2D;
				material.SetTexture("_BumpMap", getTexture(texture));
				material.SetFloat("_BumpScale", (float)def.NormalTexture.Scale);
			}

			Debug.LogFormat("material {0}: def.EmissiveTexture? {1}", materialIndex, def.EmissiveTexture != null);

			if (def.EmissiveTexture != null)
			{
				material.EnableKeyword("EMISSION_MAP_ON");
				var texture = def.EmissiveTexture.Index.Id;
				material.SetTexture("_EmissionMap", getTexture(texture));
				material.SetInt("_EmissionUV", def.EmissiveTexture.TexCoord);
			}

			// PBR channels
			if (specularGlossinessExtension != null)
			{
				KHR_materials_pbrSpecularGlossinessExtension pbr = (KHR_materials_pbrSpecularGlossinessExtension)specularGlossinessExtension;
				material.SetColor("_Color", pbr.DiffuseFactor.ToUnityColor().gamma);
				if (pbr.DiffuseTexture != null)
				{
					var texture = pbr.DiffuseTexture.Index.Id;
					material.SetTexture("_MainTex", getTexture(texture));
				}

				Debug.LogFormat("material {0}: pbr.SpecularGlossinessTexture? {1}", materialIndex, pbr.SpecularGlossinessTexture != null);

				if (pbr.SpecularGlossinessTexture != null)
				{
					var texture = pbr.SpecularGlossinessTexture.Index.Id;
					material.SetTexture("_SpecGlossMap", getTexture(texture));
					material.SetFloat("_GlossMapScale", (float)pbr.GlossinessFactor);
					material.SetFloat("_Glossiness", (float)pbr.GlossinessFactor);
				}
				else
				{
					material.SetFloat("_Glossiness", (float)pbr.GlossinessFactor);
				}

				Vector3 specularVec3 = pbr.SpecularFactor.ToUnityVector3();
				material.SetColor("_SpecColor", new Color(specularVec3.x, specularVec3.y, specularVec3.z, 1.0f));

				Debug.LogFormat("material {0}: def.OcclusionTexture? {1}", materialIndex, def.OcclusionTexture != null);

				if (def.OcclusionTexture != null)
				{
					var texture = def.OcclusionTexture.Index.Id;
					material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);
					material.SetTexture("_OcclusionMap", getTexture(texture));
				}

				GLTFMaterialUtils.SetMaterialKeywords(material, GLTFMaterialUtils.WorkflowMode.Specular);
			}
			else if (def.PbrMetallicRoughness != null)
			{
				var pbr = def.PbrMetallicRoughness;

				material.SetColor("_Color", pbr.BaseColorFactor.ToUnityColor().gamma);
				if (pbr.BaseColorTexture != null)
				{
					var texture = pbr.BaseColorTexture.Index.Id;
					material.SetTexture("_MainTex", getTexture(texture));
				}

				material.SetFloat("_Metallic", (float)pbr.MetallicFactor);
				material.SetFloat("_Glossiness", 1.0f - (float)pbr.RoughnessFactor);

				Debug.LogFormat("material {0}: pbr.MetallicRoughnessTexture? {1}", materialIndex, pbr.MetallicRoughnessTexture != null);

				if (pbr.MetallicRoughnessTexture != null)
				{
					var texture = pbr.MetallicRoughnessTexture.Index.Id;
					UnityEngine.Texture2D inputTexture = getTexture(texture) as Texture2D;
					List<Texture2D> splitTextures = splitMetalRoughTexture(inputTexture, def.OcclusionTexture != null, (float)pbr.MetallicFactor, (float)pbr.RoughnessFactor);
					material.SetTexture("_MetallicGlossMap", splitTextures[0]);

					Debug.LogFormat("material {0}: def.OcclusionTexture? {1}", materialIndex, def.OcclusionTexture != null);

					if (def.OcclusionTexture != null)
					{
						material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);
						material.SetTexture("_OcclusionMap", splitTextures[1]);
					}
				}

				GLTFMaterialUtils.SetMaterialKeywords(material, GLTFMaterialUtils.WorkflowMode.Metallic);
			}

			material.SetColor("_EmissionColor", def.EmissiveFactor.ToUnityColor().gamma);
			material.hideFlags = HideFlags.None;

			return material;
		}

		/// <summary>
		/// Get RGBA color values from a texture.
		/// </summary>
		virtual protected Color[] GetPixelsFromTexture(Texture2D texture)
		{
			return texture.GetPixels();
		}

		/// <summary>
		/// Extract separate metallic and occlusion textures from
		/// the channels of a combined metal-roughness texture,
		/// where metal/roughness/occlusion values are stored on
		/// different RGBA channels.
		/// </summary>
		/// <param name="inputTexture"></param>
		/// <param name="hasOcclusion"></param>
		/// <param name="metallicFactor"></param>
		/// <param name="roughnessFactor"></param>
		/// <returns></returns>
		virtual public List<UnityEngine.Texture2D> splitMetalRoughTexture(
			Texture2D inputTexture, bool hasOcclusion, float metallicFactor, float roughnessFactor)
		{
			List<UnityEngine.Texture2D> outputs = new List<UnityEngine.Texture2D>();

			int width = inputTexture.width;
			int height = inputTexture.height;

			Color[] occlusion = new Color[width * height];
			Color[] metalRough = new Color[width * height];
			Color[] textureColors = GetPixelsFromTexture(inputTexture);

			for (int i = 0; i < height; ++i)
			{
				for (int j = 0; j < width; ++j)
				{
					float occ = textureColors[i * width + j].r;
					float rough = textureColors[i * width + j].g;
					float met = textureColors[i * width + j].b;

					occlusion[i * width + j] = new Color(occ, occ, occ, 1.0f);
					metalRough[i * width + j] = new Color(met * metallicFactor, met * metallicFactor, met * metallicFactor, (1.0f - rough) * roughnessFactor);
				}
			}

			Texture2D metalRoughTexture = new Texture2D(width, height, TextureFormat.ARGB32, true);
			metalRoughTexture.name = inputTexture.name + "_metal";
			metalRoughTexture.SetPixels(metalRough);
			metalRoughTexture.Apply();

			outputs.Add(metalRoughTexture);

			if (hasOcclusion)
			{
				Texture2D occlusionTexture = new Texture2D(width, height);
				occlusionTexture.name = inputTexture.name + "_occlusion";
				occlusionTexture.SetPixels(occlusion);
				occlusionTexture.Apply();

				outputs.Add(occlusionTexture);
			}

			return outputs;
		}

		protected IEnumerator LoadMeshes()
		{
			for(int i = 0; i < _root.Meshes.Count; ++i)
			{
				CreateMeshObject(_root.Meshes[i], i);
				setProgress(IMPORT_STEP.MESH, (i + 1), _root.Meshes.Count);
				yield return null;
			}
		}

		protected virtual void CreateMeshObject(GLTF.Schema.Mesh meshDef, int meshId)
		{
			AddMesh();

			for (int i = 0; i < meshDef.Primitives.Count; ++i)
			{
				var primitive = meshDef.Primitives[i];

				UnityEngine.Mesh meshPrimitive
					= CreateMeshPrimitive(primitive, meshDef.Name, meshId, i); // Converted to mesh

				UnityEngine.Material material = primitive.Material != null && primitive.Material.Id >= 0
					? getMaterial(primitive.Material.Id) : defaultMaterial;

				AddMeshPrimitive(meshPrimitive, material);
			}
		}

		virtual protected UnityEngine.Mesh
		CreateMeshPrimitive(MeshPrimitive primitive, string meshName, int meshID, int primitiveIndex)
		{
			var meshAttributes = BuildMeshAttributes(primitive, meshID, primitiveIndex);
			var vertexCount = primitive.Attributes[SemanticProperties.POSITION].Value.Count;

			UnityEngine.Mesh mesh = new UnityEngine.Mesh
			{
				vertices = primitive.Attributes.ContainsKey(SemanticProperties.POSITION)
					? meshAttributes[SemanticProperties.POSITION].AccessorContent.AsVertices.ToUnityVector3()
					: null,
				normals = primitive.Attributes.ContainsKey(SemanticProperties.NORMAL)
					? meshAttributes[SemanticProperties.NORMAL].AccessorContent.AsNormals.ToUnityVector3()
					: null,

				uv = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(0))
					? meshAttributes[SemanticProperties.TexCoord(0)].AccessorContent.AsTexcoords.ToUnityVector2()
					: null,

				uv2 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(1))
					? meshAttributes[SemanticProperties.TexCoord(1)].AccessorContent.AsTexcoords.ToUnityVector2()
					: null,

				uv3 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(2))
					? meshAttributes[SemanticProperties.TexCoord(2)].AccessorContent.AsTexcoords.ToUnityVector2()
					: null,

				uv4 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(3))
					? meshAttributes[SemanticProperties.TexCoord(3)].AccessorContent.AsTexcoords.ToUnityVector2()
					: null,

				colors = primitive.Attributes.ContainsKey(SemanticProperties.Color(0))
					? meshAttributes[SemanticProperties.Color(0)].AccessorContent.AsColors.ToUnityColor()
					: null,

				triangles = primitive.Indices != null
					? meshAttributes[SemanticProperties.INDICES].AccessorContent.AsTriangles
					: MeshPrimitive.GenerateTriangles(vertexCount),

				tangents = primitive.Attributes.ContainsKey(SemanticProperties.TANGENT)
					? meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents.ToUnityVector4(true)
					: null
			};


			if (primitive.Attributes.ContainsKey(SemanticProperties.JOINT) && primitive.Attributes.ContainsKey(SemanticProperties.WEIGHT))
			{
				Vector4[] bones = new Vector4[1];
				Vector4[] weights = new Vector4[1];

				LoadSkinnedMeshAttributes(meshID, primitiveIndex, ref bones, ref weights);
				if(bones.Length != mesh.vertices.Length || weights.Length != mesh.vertices.Length)
				{
					Debug.LogError("Not enough skinning data (bones:" + bones.Length + " weights:" + weights.Length + "  verts:" + mesh.vertices.Length + ")");
					return mesh;
				}

				BoneWeight[] bws = new BoneWeight[mesh.vertices.Length];
				int maxBonesIndex = 0;
				for (int i = 0; i < bws.Length; ++i)
				{
					// Unity seems expects the the sum of weights to be 1.
					float[] normalizedWeights =  GLTFUtils.normalizeBoneWeights(weights[i]);

					bws[i].boneIndex0 = (int)bones[i].x;
					bws[i].weight0 = normalizedWeights[0];

					bws[i].boneIndex1 = (int)bones[i].y;
					bws[i].weight1 = normalizedWeights[1];

					bws[i].boneIndex2 = (int)bones[i].z;
					bws[i].weight2 = normalizedWeights[2];

					bws[i].boneIndex3 = (int)bones[i].w;
					bws[i].weight3 = normalizedWeights[3];

					maxBonesIndex = (int)Mathf.Max(maxBonesIndex, bones[i].x, bones[i].y, bones[i].z, bones[i].w);
				}

				mesh.boneWeights = bws;

				// initialize inverseBindMatrix array with identity matrix in order to output a valid mesh object
				Matrix4x4[] bindposes = new Matrix4x4[maxBonesIndex];
				for(int j=0; j < maxBonesIndex; ++j)
				{
					bindposes[j] = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
				}
				mesh.bindposes = bindposes;
			}

			if(primitive.Targets != null && primitive.Targets.Count > 0)
			{
				for (int b = 0; b < primitive.Targets.Count; ++b)
				{
					Vector3[] deltaVertices = new Vector3[primitive.Targets[b]["POSITION"].Value.Count];
					Vector3[] deltaNormals = new Vector3[primitive.Targets[b]["POSITION"].Value.Count];
					Vector3[] deltaTangents = new Vector3[primitive.Targets[b]["POSITION"].Value.Count];

					if(primitive.Targets[b].ContainsKey("POSITION"))
					{
						NumericArray num = new NumericArray();
						deltaVertices = primitive.Targets[b]["POSITION"].Value.AsVector3Array(ref num, _assetCache.Buffers[0], false).ToUnityVector3(true);
					}
					if (primitive.Targets[b].ContainsKey("NORMAL"))
					{
						NumericArray num = new NumericArray();
						deltaNormals = primitive.Targets[b]["NORMAL"].Value.AsVector3Array(ref num, _assetCache.Buffers[0], true).ToUnityVector3(true);
					}
					//if (primitive.Targets[b].ContainsKey("TANGENT"))
					//{
					//	deltaTangents = primitive.Targets[b]["TANGENT"].Value.AsVector3Array(ref num, _assetCache.BufferCache[0], true).ToUnityVector3(true);
					//}

					mesh.AddBlendShapeFrame(GLTFUtils.buildBlendShapeName(meshID, b), 1.0f, deltaVertices, deltaNormals, deltaTangents);
				}
			}

			mesh.RecalculateBounds();
			mesh.RecalculateTangents();

			return mesh;
		}

		protected virtual Dictionary<string, AttributeAccessor> BuildMeshAttributes(MeshPrimitive primitive, int meshID, int primitiveIndex)
		{
			Dictionary<string, AttributeAccessor> attributeAccessors = new Dictionary<string, AttributeAccessor>(primitive.Attributes.Count + 1);
			foreach (var attributePair in primitive.Attributes)
			{
				AttributeAccessor AttributeAccessor = new AttributeAccessor()
				{
					AccessorId = attributePair.Value,
					Buffer = _assetCache.Buffers[attributePair.Value.Value.BufferView.Value.Buffer.Id]
				};

				attributeAccessors[attributePair.Key] = AttributeAccessor;
			}

			if (primitive.Indices != null)
			{
				AttributeAccessor indexBuilder = new AttributeAccessor()
				{
					AccessorId = primitive.Indices,
					Buffer = _assetCache.Buffers[primitive.Indices.Value.BufferView.Value.Buffer.Id]
				};

				attributeAccessors[SemanticProperties.INDICES] = indexBuilder;
			}

			GLTFHelpers.BuildMeshAttributes(ref attributeAccessors);
			return attributeAccessors;
		}

		protected virtual void LoadSkinnedMeshAttributes(int meshIndex, int primitiveIndex, ref Vector4[] boneIndexes, ref Vector4[] weights)
		{
			GLTF.Schema.MeshPrimitive prim = _root.Meshes[meshIndex].Primitives[primitiveIndex];
			if (!prim.Attributes.ContainsKey(SemanticProperties.JOINT) || !prim.Attributes.ContainsKey(SemanticProperties.WEIGHT))
				return;

			parseAttribute(ref prim, SemanticProperties.JOINT, ref boneIndexes);
			parseAttribute(ref prim, SemanticProperties.WEIGHT, ref weights);
			foreach(Vector4 wei in weights)
			{
				wei.Normalize();
			}
		}

		private void parseAttribute(ref GLTF.Schema.MeshPrimitive prim, string property, ref Vector4[] values)
		{
			byte[] bufferData = _assetCache.Buffers[prim.Attributes[property].Value.BufferView.Value.Buffer.Id];
			NumericArray num = new NumericArray();
			GLTF.Math.Vector4[] gltfValues = prim.Attributes[property].Value.AsVector4Array(ref num, bufferData);
			values = new Vector4[gltfValues.Length];

			for (int i = 0; i < gltfValues.Length; ++i)
			{
				values[i] = gltfValues[i].ToUnityVector4();
			}
		}

		virtual protected GameObject createGameObject(string name)
		{
			return new GameObject(GLTFUtils.cleanName(name));
		}

		protected IEnumerator LoadScene(int sceneIndex = -1)
		{
			Scene scene;
			_nbParsedNodes = 0;

			if (sceneIndex >= 0 && sceneIndex < _root.Scenes.Count)
			{
				scene = _root.Scenes[sceneIndex];
			}
			else
			{
				scene = _root.GetDefaultScene();
			}

			if (scene == null)
			{
				throw new Exception("No default scene in gltf file.");
			}

			_sceneObject = createGameObject(_currentSampleName);

			foreach (var node in scene.Nodes)
			{
				var nodeObj = CreateNode(node.Value, node.Id);
				nodeObj.transform.SetParent(_sceneObject.transform, false);
			}

			yield return null;
		}

		protected virtual GameObject CreateNode(Node node, int index)
		{
			var nodeObj = createGameObject(node.Name != null && node.Name.Length > 0 ? node.Name : "GLTFNode_" + index);

			_nbParsedNodes++;
			setProgress(IMPORT_STEP.NODE, _nbParsedNodes, _root.Nodes.Count);
			Vector3 position;
			Quaternion rotation;
			Vector3 scale;
			node.GetUnityTRSProperties(out position, out rotation, out scale);
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;

			bool isSkinned = node.Skin != null && isValidSkin(node.Skin.Id);
			bool hasMorphOnly = node.Skin == null && node.Mesh != null && node.Mesh.Value.Weights != null && node.Mesh.Value.Weights.Count != 0;
			if (node.Mesh != null)
			{
				if (isSkinned) // Mesh is skinned (it can also have morph)
				{
					if (!_skinIndexToGameObjects.ContainsKey(node.Skin.Id))
						_skinIndexToGameObjects[node.Skin.Id] = new List<SkinnedMeshRenderer>();

					BuildSkinnedMesh(nodeObj, node.Skin.Value, node.Mesh.Id, 0);
					_skinIndexToGameObjects[node.Skin.Id].Add(nodeObj.GetComponent<SkinnedMeshRenderer>());
				}
				else if (hasMorphOnly)
				{
					SkinnedMeshRenderer smr = nodeObj.AddComponent<SkinnedMeshRenderer>();
					var primitive = getMeshPrimitive(node.Mesh.Id, 0);
					smr.sharedMesh = primitive.Key;
					smr.sharedMaterial = primitive.Value;
				}
				else
				{
					// If several primitive, create several nodes and add them as child of this current Node
					var primitive = getMeshPrimitive(node.Mesh.Id, 0);

					MeshFilter meshFilter = nodeObj.AddComponent<MeshFilter>();
					meshFilter.sharedMesh = primitive.Key;

					MeshRenderer meshRenderer = nodeObj.AddComponent<MeshRenderer>();
					meshRenderer.material = primitive.Value;
				}

				for(int i = 1; i < getMesh(node.Mesh.Id).Count; ++i)
				{
					GameObject go = createGameObject(node.Name ?? "GLTFNode_" + i);
					if (isSkinned)
					{
						BuildSkinnedMesh(go, node.Skin.Value, node.Mesh.Id, i);
						_skinIndexToGameObjects[node.Skin.Id].Add(go.GetComponent<SkinnedMeshRenderer>());
					}
					else if (hasMorphOnly)
					{
						SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
						var primitive = getMeshPrimitive(node.Mesh.Id, i);
						smr.sharedMesh = primitive.Key;
						smr.sharedMaterial = primitive.Value;
					}
					else
					{
						var primitive = getMeshPrimitive(node.Mesh.Id, i);

						MeshFilter meshFilter = nodeObj.AddComponent<MeshFilter>();
						meshFilter.sharedMesh = primitive.Key;

						MeshRenderer meshRenderer = nodeObj.AddComponent<MeshRenderer>();
						meshRenderer.material = primitive.Value;
					}

					go.transform.SetParent(nodeObj.transform, false);
				}
			}

			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					var childObj = CreateNode(child.Value, child.Id);
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			_importedObjects.Add(index, nodeObj);
			return nodeObj;
		}

		private void BuildSkinnedMesh(GameObject nodeObj, GLTF.Schema.Skin skin, int meshIndex, int primitiveIndex)
		{
			if(skin.InverseBindMatrices.Value.Count == 0)
				return;

			SkinnedMeshRenderer skinMesh = nodeObj.AddComponent<SkinnedMeshRenderer>();
			var primitive = getMeshPrimitive(meshIndex, primitiveIndex);
			skinMesh.sharedMesh = primitive.Key;
			skinMesh.sharedMaterial = primitive.Value;

			byte[] bufferData = _assetCache.Buffers[skin.InverseBindMatrices.Value.BufferView.Value.Buffer.Id];
			NumericArray content = new NumericArray();
			List<Matrix4x4> bindPoseMatrices = new List<Matrix4x4>();
			GLTF.Math.Matrix4x4[] inverseBindMatrices = skin.InverseBindMatrices.Value.AsMatrixArray(ref content, bufferData);
			foreach (GLTF.Math.Matrix4x4 mat in inverseBindMatrices)
			{
				bindPoseMatrices.Add(mat.ToUnityMatrix().switchHandedness());
			}

			skinMesh.sharedMesh.bindposes = bindPoseMatrices.ToArray();
			if(skin.Skeleton != null && _importedObjects.ContainsKey(skin.Skeleton.Id))
				skinMesh.rootBone = skin.Skeleton == null ? _importedObjects[skin.Skeleton.Id].transform : null;
		}

		private bool isValidSkin(int skinIndex)
		{
			if (skinIndex >= _root.Skins.Count)
				return false;

			Skin glTFSkin = _root.Skins[skinIndex];

			return glTFSkin.Joints.Count > 0 && glTFSkin.Joints.Count == glTFSkin.InverseBindMatrices.Value.Count;
		}

		protected IEnumerator LoadSkins()
		{
			setProgress(IMPORT_STEP.SKIN, 0, _root.Skins.Count);
			for (int i = 0; i < _root.Skins.Count; ++i)
			{
				LoadSkin(_root.Skins[i], i);
				setProgress(IMPORT_STEP.SKIN, (i + 1), _root.Skins.Count);
				yield return null;
			}
		}

		protected void LoadSkin(GLTF.Schema.Skin skin, int index)
		{
			Transform[] boneList = new Transform[skin.Joints.Count];
			for (int i = 0; i < skin.Joints.Count; ++i)
			{
				boneList[i] = _importedObjects[skin.Joints[i].Id].transform;
			}

			foreach (SkinnedMeshRenderer skinMesh in _skinIndexToGameObjects[index])
			{
				skinMesh.bones = boneList;
			}
		}

		virtual protected IEnumerator LoadAnimations() { yield break; }

	}
}
