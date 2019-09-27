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

		override protected List<KeyValuePair<UnityEngine.Mesh, UnityEngine.Material>>
		getMesh(int meshIndex)
		{
			return _assetManager._parsedMeshData[meshIndex];
		}

		override protected KeyValuePair<UnityEngine.Mesh, UnityEngine.Material>
		getMeshPrimitive(int meshIndex, int primitiveIndex)
		{
			UnityEngine.Mesh mesh
				= _assetManager.getMesh(meshIndex, primitiveIndex);
			UnityEngine.Material material
				= _assetManager.getMaterial(meshIndex, primitiveIndex);

			return new KeyValuePair<UnityEngine.Mesh, UnityEngine.Material>(mesh, material);
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

		override protected GameObject createGameObject(string name)
		{
			name = GLTFUtils.cleanName(name);
			return _assetManager.createGameObject(name);
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