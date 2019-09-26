using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityEditor;

namespace UnityGLTF
{
	/// <summary>
	/// Editor windows to load a GLTF scene in editor
	/// </summary>
	///
	public class GLTFEditorImporter : GLTFRuntimeImporter
	{
		// Import paths and options
		/// <summary>
		/// Parent directory of directory where importer will
		/// create Unity prefab and associated files
		/// (e.g. meshes, materials). Must be located inside Unity
		/// project folder.
		/// </summary>
		private string _projectDirectoryPath;
		/// <summary>
		/// If true, the generated model prefab is automatically
		/// to the current Unity scene.
		/// </summary>
		private bool _addToCurrentScene;
		/// <summary>
		/// Writes GLTF objects (images, textures, meshes, etc.) to
		/// disk as Unity assets.
		/// </summary>
		AssetManager _assetManager;

		/// <summary>
		/// Constructors setting the delegate function to call after each iteration
		/// </summary>
		/// <param name="delegateFunction">The function to call after each iteration (usually Repaint())</param>
		public GLTFEditorImporter(ProgressCallback progressCallback, RefreshWindow finish=null)
			: base(progressCallback, finish) {}

		/// <summary>
		/// Setup importer for an import
		/// </summary>
		/// <param name="gltfPath">Absolute path to the glTF file to import</param>
		/// <param name="importPath">Path in current project where to import the model</param>
		/// <param name="modelName">Name of the model prefab to create<param>
		public void setupForPath(string gltfPath, string importPath, string modelName, bool addScene=false)
		{
			base.setupForPath(gltfPath, modelName);

			_projectDirectoryPath = importPath;
			_assetManager = new AssetManager(_projectDirectoryPath, _currentSampleName);
			_addToCurrentScene = addScene;
		}

		// Private
		private void checkValidity()
		{
			if (_taskManager == null)
			{
				_taskManager = new TaskManager();
			}
		}

		override protected void AddImage(Texture2D image)
		{
			image = _assetManager.saveTexture(
				GLTFTextureUtils.flipTexture(image),
				_assetManager._parsedImages.Count, "image");

			_assetManager.registerImage(image);
		}

		override protected void AddTexture(Texture2D texture)
		{
			texture = _assetManager.saveTexture(
				texture, _assetManager._parsedTextures.Count, "texture");

			_assetManager.registerTexture(texture);
		}

		override protected void AddMaterial(UnityEngine.Material material)
		{
			material = _assetManager.saveMaterial(
				material, _assetManager._parsedMaterials.Count);

			_assetManager.registerMaterial(material);
		}

		override protected void AddMesh()
		{
			_assetManager.registerMesh();
		}

		override protected void AddMeshPrimitive(
			UnityEngine.Mesh primitive, UnityEngine.Material material)
		{
			int meshIndex = _assetManager._parsedMeshData.Count - 1;
			int primitiveIndex = _assetManager._parsedMeshData[meshIndex].Count;

			primitive = _assetManager.saveMesh(primitive,
				string.Format("mesh_{0}_{1}", meshIndex, primitiveIndex));

			_assetManager.registerMeshPrimitive(primitive, material);
		}

		override protected Texture2D getImage(int index)
		{
			return _assetManager.getImage(index);
		}

		override protected Texture2D getTexture(int index)
		{
			return _assetManager.getTexture(index);
		}

		override protected UnityEngine.Material getMaterial(int index)
		{
			return _assetManager.getMaterial(index);
		}

		/// <summary>
		/// Get RGBA color values from a texture.
		/// </summary>
		override protected Color[] GetPixelsFromTexture(Texture2D texture)
		{
			Color[] colors = new Color[texture.width * texture.height];
			GLTFUtils.getPixelsFromTexture(ref texture, out colors);
			return colors;
		}

		override public List<UnityEngine.Texture2D> splitMetalRoughTexture(
			Texture2D inputTexture, bool hasOcclusion, float metallicFactor, float roughnessFactor)
		{
			List<UnityEngine.Texture2D> splitTextures
				= base.splitMetalRoughTexture(
					inputTexture, hasOcclusion, metallicFactor, roughnessFactor);

			// save extracted textures as `.asset` files

			if (splitTextures.Count >= 1) {
				splitTextures[0] = _assetManager.saveTexture(
					splitTextures[0], -1, inputTexture.name + "_metal");
			}

			if (splitTextures.Count >= 2) {
				splitTextures[1] = _assetManager.saveTexture(
					splitTextures[1], -1, inputTexture.name + "_occlusion");
			}

			return splitTextures;
		}

		override protected IEnumerator LoadScene(int sceneIndex = -1)
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

		override protected IEnumerator LoadAnimations()
		{
			for (int i = 0; i < _root.Animations.Count; ++i)
			{
				AnimationClip clip = new AnimationClip();
				clip.wrapMode = UnityEngine.WrapMode.Loop;
				LoadAnimation(_root.Animations[i], i, clip);
				setProgress(IMPORT_STEP.ANIMATION, (i + 1), _root.Animations.Count);
				_assetManager.saveAnimationClip(clip);
				yield return null;
			}
		}

		private void LoadAnimation(GLTF.Schema.Animation gltfAnimation, int index, AnimationClip clip)
		{
			clip.name = gltfAnimation.Name != null && gltfAnimation.Name.Length > 0 ? gltfAnimation.Name : "GLTFAnimation_" + index;
			for(int i=0; i < gltfAnimation.Channels.Count; ++i)
			{
				AnimationChannel channel = gltfAnimation.Channels[i];
				addGLTFChannelDataToClip(gltfAnimation.Channels[i], clip);
			}

			clip.EnsureQuaternionContinuity();
		}

		private void addGLTFChannelDataToClip(GLTF.Schema.AnimationChannel channel, AnimationClip clip)
		{
			int animatedNodeIndex = channel.Target.Node.Id;
			if (!_importedObjects.ContainsKey(animatedNodeIndex))
			{
				Debug.Log("Node '" + animatedNodeIndex + "' found for animation, aborting.");
			}

			Transform animatedNode = _importedObjects[animatedNodeIndex].transform;
			string nodePath = AnimationUtility.CalculateTransformPath(animatedNode, _sceneObject.transform);

			bool isStepInterpolation = channel.Sampler.Value.Interpolation != InterpolationType.LINEAR;

			byte[] timeBufferData = _assetCache.Buffers[channel.Sampler.Value.Output.Value.BufferView.Value.Buffer.Id];
			float[] times = GLTFHelpers.ParseKeyframeTimes(channel.Sampler.Value.Input.Value, timeBufferData);

			if (channel.Target.Path == GLTFAnimationChannelPath.translation || channel.Target.Path == GLTFAnimationChannelPath.scale)
			{
				byte[] bufferData = _assetCache.Buffers[channel.Sampler.Value.Output.Value.BufferView.Value.Buffer.Id];
				GLTF.Math.Vector3[] keyValues = GLTFHelpers.ParseVector3Keyframes(channel.Sampler.Value.Output.Value, bufferData);
				if (keyValues == null)
					return;

				Vector3[] values = keyValues.ToUnityVector3();
				AnimationCurve[] vector3Curves = GLTFUtils.createCurvesFromArrays(times, values, isStepInterpolation, channel.Target.Path == GLTFAnimationChannelPath.translation);

				if (channel.Target.Path == GLTFAnimationChannelPath.translation)
					GLTFUtils.addTranslationCurvesToClip(vector3Curves, nodePath, ref clip);
				else
					GLTFUtils.addScaleCurvesToClip(vector3Curves, nodePath, ref clip);
			}
			else if (channel.Target.Path == GLTFAnimationChannelPath.rotation)
			{
				byte[] bufferData = _assetCache.Buffers[channel.Sampler.Value.Output.Value.BufferView.Value.Buffer.Id];
				Vector4[] values = GLTFHelpers.ParseRotationKeyframes(channel.Sampler.Value.Output.Value, bufferData).ToUnityVector4();
				AnimationCurve[] rotationCurves = GLTFUtils.createCurvesFromArrays(times, values, isStepInterpolation);

				GLTFUtils.addRotationCurvesToClip(rotationCurves, nodePath, ref clip);
			}
			else if(channel.Target.Path == GLTFAnimationChannelPath.weights)
			{
				List<string> morphTargets = new List<string>();
				int meshIndex = _root.Nodes[animatedNodeIndex].Mesh.Id;
				for(int i=0; i<  _root.Meshes[meshIndex].Primitives[0].Targets.Count; ++i)
				{
					morphTargets.Add(GLTFUtils.buildBlendShapeName(meshIndex, i));
				}

				byte[] bufferData = _assetCache.Buffers[channel.Sampler.Value.Output.Value.BufferView.Value.Buffer.Id];
				float[] values = GLTFHelpers.ParseKeyframeTimes(channel.Sampler.Value.Output.Value, bufferData);
				AnimationCurve[] morphCurves = GLTFUtils.buildMorphAnimationCurves(times, values, morphTargets.Count);

				GLTFUtils.addMorphAnimationCurvesToClip(morphCurves, nodePath, morphTargets.ToArray(), ref clip);
			}
			else
			{
				Debug.Log("Unsupported animation channel target: " + channel.Target.Path);
			}
		}

		override protected IEnumerator LoadSkins()
		{
			setProgress(IMPORT_STEP.SKIN, 0, _root.Skins.Count);
			for (int i = 0; i < _root.Skins.Count; ++i)
			{
				LoadSkin(_root.Skins[i], i);
				setProgress(IMPORT_STEP.SKIN, (i + 1), _root.Skins.Count);
				yield return null;
			}
		}

		private void LoadSkin(GLTF.Schema.Skin skin, int index)
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

		private void BuildSkinnedMesh(GameObject nodeObj, GLTF.Schema.Skin skin, int meshIndex, int primitiveIndex)
		{
			if(skin.InverseBindMatrices.Value.Count == 0)
				return;

			SkinnedMeshRenderer skinMesh = nodeObj.AddComponent<SkinnedMeshRenderer>();
			skinMesh.sharedMesh = _assetManager.getMesh(meshIndex, primitiveIndex);
			skinMesh.sharedMaterial = _assetManager.getMaterial(meshIndex, primitiveIndex);

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
					smr.sharedMesh = _assetManager.getMesh(node.Mesh.Id, 0);
					smr.sharedMaterial = _assetManager.getMaterial(node.Mesh.Id, 0);
				}
				else
				{
					// If several primitive, create several nodes and add them as child of this current Node
					MeshFilter meshFilter = nodeObj.AddComponent<MeshFilter>();
					meshFilter.sharedMesh = _assetManager.getMesh(node.Mesh.Id, 0);

					MeshRenderer meshRenderer = nodeObj.AddComponent<MeshRenderer>();
					meshRenderer.material = _assetManager.getMaterial(node.Mesh.Id, 0);
				}

				for(int i = 1; i < _assetManager._parsedMeshData[node.Mesh.Id].Count; ++i)
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
						smr.sharedMesh = _assetManager.getMesh(node.Mesh.Id, i);
						smr.sharedMaterial = _assetManager.getMaterial(node.Mesh.Id, i);
					}
					else
					{
						MeshFilter mf = go.AddComponent<MeshFilter>();
						mf.sharedMesh = _assetManager.getMesh(node.Mesh.Id, i);
						MeshRenderer mr = go.AddComponent<MeshRenderer>();
						mr.material = _assetManager.getMaterial(node.Mesh.Id, i);
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

		private GameObject createGameObject(string name)
		{
			name = GLTFUtils.cleanName(name);
			return _assetManager.createGameObject(name);
		}

		private bool isValidSkin(int skinIndex)
		{
			if (skinIndex >= _root.Skins.Count)
				return false;

			Skin glTFSkin = _root.Skins[skinIndex];

			return glTFSkin.Joints.Count > 0 && glTFSkin.Joints.Count == glTFSkin.InverseBindMatrices.Value.Count;
		}

		override protected void finishImport()
		{
			GameObject prefab = _assetManager.savePrefab(_sceneObject, _projectDirectoryPath, _addToCurrentScene);
			base.finishImport();

			Clear();

			if (_addToCurrentScene == true)
			{
				// Select and focus imported object
				_sceneObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
				GameObject[] obj = new GameObject[1];
				obj[0] = _sceneObject;
				Selection.objects = obj;
#if UNITY_2017
				EditorApplication.ExecuteMenuItem("Edit/Frame Selected");
#endif
			}
		}

		/// <summary>
		/// Cleans all generated files and structures
		/// </summary>
		///
		public void softClean()
		{
			if (_assetManager != null)
				_assetManager.softClean();

			_taskManager.clear();
			Resources.UnloadUnusedAssets();
		}

		override public void Clear()
		{
			base.Clear();

			if (_assetManager != null)
				_assetManager.softClean();

			Resources.UnloadUnusedAssets();
		}

		private void cleanObjects()
		{
			foreach (GameObject ob in _importedObjects.Values)
			{
				GameObject.DestroyImmediate(ob);
			}
			GameObject.DestroyImmediate(_sceneObject);
			_sceneObject = null;
			_assetManager.softClean();
		}

		public void OnDestroy()
		{
			Clear();
		}
	}
}