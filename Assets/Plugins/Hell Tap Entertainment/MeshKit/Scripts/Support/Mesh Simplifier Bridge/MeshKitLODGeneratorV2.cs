////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MeshKitLODGeneratorV2.cs
//
//	This is a modified version of the LODGenerator.cs script to work in a similar way to the
//	previous MeshKit system.
//
//	MODS:
//	- Changed LOD Parent GameObject Name to "_LOD_" ( to match previous version)
//	- Objects and meshes use the GameObject Name to name themselves ( to match previous version)
//	- Added showProgressBarInEditor so we can display progress bars
//	- Removed saveAssetsPath, MeshKit will be handling all asset management now.
//	- Removed huge chunks of unneeded code
//	- Cleaned up code and added comments
//
//	© 2024 Melli Georgiou.
//	Hell Tap Entertainment LTD
//
////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HellTap.UnityMeshSimplifier {

	/// Contains methods for generating LODs (level of details) for game objects.
	public static class MeshKitLODGeneratorV2 {

		// Static Strings
		public static readonly string LODParentGameObjectName = "_LOD_"; 	// <- Changed to work the same way as previous MeshKit system.

		// RendererInfo Helper Struct
		private struct RendererInfo {
			public string name;
			public bool isStatic;
			public bool isNewMesh;
			public Transform transform;
			public Mesh mesh;
			public Material[] materials;
			public Transform rootBone;
			public Transform[] bones;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	GENERATE LODS
		//	Generates the LODs and sets up a LOD Group for the specified game object.
		//	--------------------------------------------------------------------------------------------------------------------
		//	gameObject = The game object to set up.
		//	levels = The LOD levels to set up.
		//	autoCollectRenderers = If the renderers under the game object and any children should be automatically collected.
		//	simplificationOptions = The mesh simplification options.
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static LODGroup GenerateLODs(
			GameObject gameObject, 
			LODLevel[] levels, 
			bool autoCollectRenderers, 
			SimplificationOptions simplificationOptions,
			bool showProgressBarInEditor = false 			 // <- Added to get Progress Bar popups in Editor
		){

			// Make sure GameObject and Levels are valid
			if (gameObject == null){
				throw new System.ArgumentNullException(nameof(gameObject));
			} else if (levels == null){
				throw new System.ArgumentNullException(nameof(levels));
			}

			// Cache References
			var transform = gameObject.transform;
			var existingLodParent = transform.Find(LODParentGameObjectName);

			// Throw an error if this GameObject already has an AutoLOD setup
			if (existingLodParent != null){
				throw new System.InvalidOperationException("The game object already appears to have LODs. Please remove them first.");
			}

			// Check for existing LODGroup
			var existingLodGroup = gameObject.GetComponent<LODGroup>();
			if (existingLodGroup != null){
				throw new System.InvalidOperationException("The game object already appears to have a LOD Group. Please remove it first.");
			}

			// Validate Simplification Options
			MeshSimplifier.ValidateOptions(simplificationOptions);

			// Setup a new GameObject to be the LOD Parent
			var lodParentGameObject = new GameObject(LODParentGameObjectName);
			var lodParent = lodParentGameObject.transform;
			ParentAndResetTransform(lodParent, transform);

			// Create and cache the LODGroup
			var lodGroup = gameObject.AddComponent<LODGroup>();

			// Automatically collect all enabled renderers under the game object if enabled (it will be in MeshKit)
			Renderer[] allRenderers = null;
			if (autoCollectRenderers){
				allRenderers = GetChildRenderersForLOD(gameObject);
			}

			// Setup Renderers to disable and LODs
			var renderersToDisable = new List<Renderer>((allRenderers != null ? allRenderers.Length : 10));
			var lods = new LOD[levels.Length];

			// Setup The LODs
			int levelsLength = levels.Length;
			for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++ ){

				// NEW: Show Progress Bars In Editor
				#if UNITY_EDITOR
					if( Application.isPlaying == false && showProgressBarInEditor == true ){
						UnityEditor.EditorUtility.DisplayProgressBar("Generating LODs", "Creating LOD Level (" + levelIndex + " / " + (levelsLength-1) + ") ...", 
							(float)levelIndex / (float)(levelsLength-1) );
					}
				#endif

				// Cache the level index
				var level = levels[levelIndex];

				// Setup GameObject To Parent Each LOD Level
				//var levelGameObject = new GameObject(string.Format("Level{0:00}", levelIndex));
				var levelGameObject = new GameObject( "Level" + levelIndex.ToString() );	 // <- Edited to match old version

				// Cache the LOD Level transform. Then match it to its parent settings
				var levelTransform = levelGameObject.transform;
				ParentAndResetTransform(levelTransform, lodParent);

				// Setup the original renderers for this LOD Level
				Renderer[] originalLevelRenderers = allRenderers ?? level.Renderers;
				var levelRenderers = new List<Renderer>((originalLevelRenderers != null ? originalLevelRenderers.Length : 0));

				// Make sure its not null or empty
				if (originalLevelRenderers != null && originalLevelRenderers.Length > 0){

					// Get Mesh Renderers
					var meshRenderers = (from renderer in originalLevelRenderers
										 let meshFilter = renderer.GetComponent<MeshFilter>()
										 where renderer.enabled && renderer as MeshRenderer != null
										 && meshFilter != null
										 && meshFilter.sharedMesh != null
										 select renderer as MeshRenderer).ToArray();

					// Get Skinned Mesh Renderers
					var skinnedMeshRenderers = (from renderer in originalLevelRenderers
												where renderer.enabled && renderer as SkinnedMeshRenderer != null
												&& (renderer as SkinnedMeshRenderer).sharedMesh != null
												select renderer as SkinnedMeshRenderer).ToArray();


					// Setup RendererInfo Arrays for static and skinned Renderers
					RendererInfo[] staticRenderers;
					RendererInfo[] skinnedRenderers;
					
					// If we should combine meshes at this level do it now (MeshKit doesn't do this)
					if (level.CombineMeshes){
						
						staticRenderers = CombineStaticMeshes(transform, levelIndex, meshRenderers);
						skinnedRenderers = CombineSkinnedMeshes(transform, levelIndex, skinnedMeshRenderers);
					
					// Otherwise, populate the renderers normally
					} else {

						staticRenderers = GetStaticRenderers(meshRenderers);
						skinnedRenderers = GetSkinnedRenderers(skinnedMeshRenderers);
					}

					// Make sure our static renderers array isn't null
					if ( staticRenderers != null ){

						// Loop through them...
						for ( int rendererIndex = 0; rendererIndex < staticRenderers.Length; rendererIndex++ ){

							// Create a GameObject for each Renderer (Simplified Meshes get built through this)
							var renderer = staticRenderers[rendererIndex];
							var levelRenderer = CreateLevelRenderer(gameObject, levelIndex, level, levelTransform, rendererIndex, renderer, simplificationOptions);

							// Add this to the level Renderers array
							levelRenderers.Add(levelRenderer);
						}
					}

					// Make sure our static renderers array isn't null
					if (skinnedRenderers != null){

						// Loop through them
						for (int rendererIndex = 0; rendererIndex < skinnedRenderers.Length; rendererIndex++ ){

							// Create a GameObject for each Skinned Renderer (Simplified Meshes get built through this)
							var renderer = skinnedRenderers[rendererIndex];
							var levelRenderer = CreateLevelRenderer(gameObject, levelIndex, level, levelTransform, rendererIndex, renderer, simplificationOptions);

							// Add this to the level Renderers array
							levelRenderers.Add(levelRenderer);
						}
					}

					// Loop through all the renderers and setup the renderers to disable array
					foreach (var renderer in originalLevelRenderers ){
						if ( !renderersToDisable.Contains(renderer) ){
							renderersToDisable.Add(renderer);
						}
					}
				}

				// Add a new LOD to the lods array
				lods[levelIndex] = new LOD(level.ScreenRelativeTransitionHeight, levelRenderers.ToArray());
			}

			// Create backup (not sure how this works?)
			CreateBackup( gameObject, renderersToDisable.ToArray() );

			// Disable all renderers that we should disable
			foreach (var renderer in renderersToDisable){
				renderer.enabled = false;
			}

			// Setup the LODGroup and return it
			lodGroup.animateCrossFading = false;
			lodGroup.SetLODs(lods);
			return lodGroup;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	DESTROY LODS (via GameObject)
		//	Destroys the generated LODs and LOD Group for the specified game object. Returns true if successful.
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool DestroyLODs(GameObject gameObject){

			// Throw and error if a gameObject wasn't setup
			if (gameObject == null){
				throw new System.ArgumentNullException(nameof(gameObject));
			}

			// Restore Backup from the supplied GameObject
			RestoreBackup(gameObject);

			var transform = gameObject.transform;
			var lodParent = transform.Find(LODParentGameObjectName);
			if (lodParent == null)
				return false;

			// Destroy the LOD parent
			DestroyObject(lodParent.gameObject);

			// Destroy the LOD Group if there is one
			var lodGroup = gameObject.GetComponent<LODGroup>();
			if (lodGroup != null)
			{
				DestroyObject(lodGroup);
			}

			return true;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	GET STATIC RENDERERS
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static RendererInfo[] GetStaticRenderers(MeshRenderer[] renderers){

			var newRenderers = new List<RendererInfo>(renderers.Length);
			for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
			{
				var renderer = renderers[rendererIndex];
				var meshFilter = renderer.GetComponent<MeshFilter>();
				if (meshFilter == null)
				{
					Debug.LogWarning("A renderer was missing a mesh filter and was ignored.", renderer);
					continue;
				}

				var mesh = meshFilter.sharedMesh;
				if (mesh == null)
				{
					Debug.LogWarning("A renderer was missing a mesh and was ignored.", renderer);
					continue;
				}

				newRenderers.Add(new RendererInfo
				{
					name = renderer.name,
					isStatic = true,
					isNewMesh = false,
					transform = renderer.transform,
					mesh = mesh,
					materials = renderer.sharedMaterials
				});
			}
			return newRenderers.ToArray();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	GET SKINNED RENDERERS
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static RendererInfo[] GetSkinnedRenderers(SkinnedMeshRenderer[] renderers){

			var newRenderers = new List<RendererInfo>(renderers.Length);
			for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
			{
				var renderer = renderers[rendererIndex];

				var mesh = renderer.sharedMesh;
				if (mesh == null)
				{
					Debug.LogWarning("A renderer was missing a mesh and was ignored.", renderer);
					continue;
				}

				newRenderers.Add(new RendererInfo
				{
					name = renderer.name,
					isStatic = false,
					isNewMesh = false,
					transform = renderer.transform,
					mesh = mesh,
					materials = renderer.sharedMaterials,
					rootBone = renderer.rootBone,
					bones = renderer.bones
				});
			}
			return newRenderers.ToArray();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	COMBINE STATIC MESHES
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static RendererInfo[] CombineStaticMeshes(Transform transform, int levelIndex, MeshRenderer[] renderers){

			if (renderers.Length == 0){
				return null;
			}

			// TODO: Support to merge sub-meshes and atlas textures

			var newRenderers = new List<RendererInfo>(renderers.Length);

			Material[] combinedMaterials;
			var combinedMesh = MeshCombiner.CombineMeshes(transform, renderers, out combinedMaterials);
			combinedMesh.name = string.Format("{0}_static{1:00}", transform.name, levelIndex);
			string rendererName = string.Format("{0}_combined_static", transform.name);
			newRenderers.Add(new RendererInfo
			{
				name = rendererName,
				isStatic = true,
				isNewMesh = true,
				transform = null,
				mesh = combinedMesh,
				materials = combinedMaterials,
				rootBone = null,
				bones = null
			});

			return newRenderers.ToArray();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	COMBINE SKINNED MESHES
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static RendererInfo[] CombineSkinnedMeshes(Transform transform, int levelIndex, SkinnedMeshRenderer[] renderers ){
			
			if (renderers.Length == 0){
				return null;
			}

			// TODO: Support to merge sub-meshes and atlas textures

			var newRenderers = new List<RendererInfo>(renderers.Length);
			var blendShapeRenderers = (from renderer in renderers
									   where renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0
									   select renderer);
			var renderersWithoutMesh = (from renderer in renderers
										where renderer.sharedMesh == null
										select renderer);
			var combineRenderers = (from renderer in renderers
									where renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount == 0
									select renderer).ToArray();

			// Warn about renderers without a mesh
			foreach (var renderer in renderersWithoutMesh)
			{
				Debug.LogWarning("A renderer was missing a mesh and was ignored.", renderer);
			}

			// Don't combine meshes with blend shapes
			foreach (var renderer in blendShapeRenderers)
			{
				newRenderers.Add(new RendererInfo
				{
					name = renderer.name,
					isStatic = false,
					isNewMesh = false,
					transform = renderer.transform,
					mesh = renderer.sharedMesh,
					materials = renderer.sharedMaterials,
					rootBone = renderer.rootBone,
					bones = renderer.bones
				});
			}

			if (combineRenderers.Length > 0)
			{
				Material[] combinedMaterials;
				Transform[] combinedBones;
				var combinedMesh = MeshCombiner.CombineMeshes(transform, combineRenderers, out combinedMaterials, out combinedBones);
				combinedMesh.name = string.Format("{0}_skinned{1:00}", transform.name, levelIndex);

				var rootBone = FindBestRootBone(transform, combineRenderers);
				string rendererName = string.Format("{0}_combined_skinned", transform.name);
				newRenderers.Add(new RendererInfo
				{
					name = rendererName,
					isStatic = false,
					isNewMesh = false,
					transform = null,
					mesh = combinedMesh,
					materials = combinedMaterials,
					rootBone = rootBone,
					bones = combinedBones
				});
			}

			return newRenderers.ToArray();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	PARENT AND RESET TRANSFORM
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static void ParentAndResetTransform(Transform transform, Transform parentTransform){

			transform.SetParent(parentTransform);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	PARENT AND OFFSET TRANSFORM
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static void ParentAndOffsetTransform(Transform transform, Transform parentTransform, Transform originalTransform){

			transform.position = originalTransform.position;
			transform.rotation = originalTransform.rotation;
			transform.localScale = originalTransform.lossyScale;
			transform.SetParent(parentTransform, true);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	CREATE LEVEL RENDERER
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static Renderer CreateLevelRenderer (
			
			GameObject gameObject, 
			int levelIndex, 
			in LODLevel level, 
			Transform levelTransform, 
			int rendererIndex, 
			in RendererInfo renderer, 
			in SimplificationOptions simplificationOptions
		){

			// NEW:
			//Debug.LogWarning( "rendererName = " + gameObject.name );
			string rendererName = gameObject.name;	// <- Use the same name as the GameObject, which is what we do in previous system.

			// Cache the renderer's mesh
			var mesh = renderer.mesh;

			// Simplify the mesh if necessary
			if (level.Quality < 1f){

				// Simplify the mesh if we've set the quality to less than 1
				mesh = SimplifyMesh(mesh, level.Quality, simplificationOptions);

				// If this renderer is a new mesh, destroy it
				if (renderer.isNewMesh){
					DestroyObject(renderer.mesh);
				}

				mesh.name = rendererName;
			}

			// If the renderer is static
			if (renderer.isStatic){

				//string rendererName = string.Format("{0:000}_static_{1}", rendererIndex, renderer.name);
				return CreateStaticLevelRenderer(rendererName, levelTransform, renderer.transform, mesh, renderer.materials, level);
			
			// Otherwise ...
			} else {

				//string rendererName = string.Format("{0:000}_skinned_{1}", rendererIndex, renderer.name);
				return CreateSkinnedLevelRenderer(rendererName, levelTransform, renderer.transform, mesh, renderer.materials, renderer.rootBone, renderer.bones, level);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	CREATE STATIC LEVEL RENDERER
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static MeshRenderer CreateStaticLevelRenderer(string name, Transform parentTransform, Transform originalTransform, Mesh mesh, Material[] materials, in LODLevel level)
		{
			var levelGameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
			var levelTransform = levelGameObject.transform;
			if (originalTransform != null)
			{
				ParentAndOffsetTransform(levelTransform, parentTransform, originalTransform);
			}
			else
			{
				ParentAndResetTransform(levelTransform, parentTransform);
			}

			var meshFilter = levelGameObject.GetComponent<MeshFilter>();
			meshFilter.sharedMesh = mesh;

			var meshRenderer = levelGameObject.GetComponent<MeshRenderer>();
			meshRenderer.sharedMaterials = materials;
			SetupLevelRenderer(meshRenderer, level);
			return meshRenderer;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	CREATE SKINNED LEVEL RENDERER
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static SkinnedMeshRenderer CreateSkinnedLevelRenderer(string name, Transform parentTransform, Transform originalTransform, Mesh mesh, Material[] materials, Transform rootBone, Transform[] bones, in LODLevel level)
		{
			var levelGameObject = new GameObject(name, typeof(SkinnedMeshRenderer));
			var levelTransform = levelGameObject.transform;
			if (originalTransform != null)
			{
				ParentAndOffsetTransform(levelTransform, parentTransform, originalTransform);
			}
			else
			{
				ParentAndResetTransform(levelTransform, parentTransform);
			}

			var skinnedMeshRenderer = levelGameObject.GetComponent<SkinnedMeshRenderer>();
			skinnedMeshRenderer.sharedMesh = mesh;
			skinnedMeshRenderer.sharedMaterials = materials;
			skinnedMeshRenderer.rootBone = rootBone;
			skinnedMeshRenderer.bones = bones;
			SetupLevelRenderer(skinnedMeshRenderer, level);
			return skinnedMeshRenderer;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	FIND BEST ROOT BONE
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static Transform FindBestRootBone(Transform transform, SkinnedMeshRenderer[] skinnedMeshRenderers){

			if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0){
				return null;
			}

			Transform bestBone = null;
			float bestDistance = float.MaxValue;
			for (int i = 0; i < skinnedMeshRenderers.Length; i++){

				if (skinnedMeshRenderers[i] == null || skinnedMeshRenderers[i].rootBone == null){
					continue;
				}

				var rootBone = skinnedMeshRenderers[i].rootBone;
				var distance = (rootBone.position - transform.position).sqrMagnitude;
				if (distance < bestDistance)
				{
					bestBone = rootBone;
					bestDistance = distance;
				}
			}

			return bestBone;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	SETUP LEVEL RENDERER
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static void SetupLevelRenderer(Renderer renderer, in LODLevel level){

			renderer.shadowCastingMode = level.ShadowCastingMode;
			renderer.receiveShadows = level.ReceiveShadows;
			renderer.motionVectorGenerationMode = level.MotionVectorGenerationMode;
			renderer.lightProbeUsage = level.LightProbeUsage;
			renderer.reflectionProbeUsage = level.ReflectionProbeUsage;

			var skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
			if (skinnedMeshRenderer != null)
			{
				skinnedMeshRenderer.quality = level.SkinQuality;
				skinnedMeshRenderer.skinnedMotionVectors = level.SkinnedMotionVectors;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	GET CHILD RENDERERS FOR LOD
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static Renderer[] GetChildRenderersForLOD(GameObject gameObject){

			// Setup a new list of Renderers
			var resultRenderers = new List<Renderer>();

			// Get All Child Renderers To Use With This LOD
			CollectChildRenderersForLOD(gameObject.transform, resultRenderers);

			// Return the result as an array
			return resultRenderers.ToArray();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	COLLECT CHILD RENDERERS FOR LOD
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static void CollectChildRenderersForLOD(Transform transform, List<Renderer> resultRenderers){

			// Collect the rendererers of this transform
			var childRenderers = transform.GetComponents<Renderer>();
			resultRenderers.AddRange(childRenderers);

			// Loop through the child count
			int childCount = transform.childCount;
			for (int i = 0; i < childCount; i++){

				// Skip children that are not active
				var childTransform = transform.GetChild(i);

				// Skip if the gameObject is not active
				if (!childTransform.gameObject.activeSelf){
					continue;
				}

				// If the transform have the identical name as to our LOD Parent GO name, then we also skip it
				if (string.Equals(childTransform.name, LODParentGameObjectName)){
					continue;
				}

				// Skip children that has a LOD Group or a LOD Generator Helper component
				if (childTransform.GetComponent<LODGroup>() != null){
					continue;
				}

				// Skip children that have a LODGeneratorHelper
				else if (childTransform.GetComponent<LODGeneratorHelper>() != null ){
					continue;
				}

				// Continue recursively through the children of this transform
				CollectChildRenderersForLOD(childTransform, resultRenderers);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	SIMPLIFY MESH
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static Mesh SimplifyMesh(Mesh mesh, float quality, in SimplificationOptions options){

			// Make a copy of the mesh and simplify it
			var meshSimplifier = new MeshSimplifier();
			meshSimplifier.SimplificationOptions = options;
			meshSimplifier.Initialize(mesh);
			meshSimplifier.SimplifyMesh(quality);

			var simplifiedMesh = meshSimplifier.ToMesh();
			simplifiedMesh.bindposes = mesh.bindposes;
			return simplifiedMesh;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	DESTROY OBJECT
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static void DestroyObject(Object obj){

			// If the object we're supposed to destroy is null, show an error
			if (obj == null){ throw new System.ArgumentNullException(nameof(obj)); }

			#if UNITY_EDITOR
			
				// If Game is playing ...
				if (Application.isPlaying){
				
					// Destroy the object normally
					Object.Destroy(obj);
				
				// Otherwise
				} else {

					// Destroy Immediately, but don't destroy any assets ( = false setting)
					Object.DestroyImmediate(obj, false);
				}
			
			#else

				// Destroy the object normally
				Object.Destroy(obj);
			
			#endif
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	CREATE BACKUP
		//	This adds an invisible LODBackupComponent to the GameObject to track the original renderers
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static void CreateBackup(GameObject go, Renderer[] originalRenderers){

			// Add the LODBackupComponent
			var backupComponent = go.AddComponent<LODBackupComponent>();

			// Make it invisible in the Inspector
			backupComponent.hideFlags = HideFlags.HideInInspector;

			// Save the original Renderers 
			backupComponent.OriginalRenderers = originalRenderers;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	RESTORE BACKUP
		//	Uses the invisible LODBackupComponent to restore the original renderers
		////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		private static void RestoreBackup(GameObject go){

			// Cache the LODBackupComponents 
			var backupComponents = go.GetComponents<LODBackupComponent>();

			// Loop through them
			foreach (var backupComponent in backupComponents){

				// Cache the original renderers array from the component
				var originalRenderers = backupComponent.OriginalRenderers;

				// Loop through them if it's valid ...
				if (originalRenderers != null){
					foreach (var renderer in originalRenderers){

						// Enable the original renderers
						if (renderer != null){ renderer.enabled = true; }
					}
				}

				// Destroy the Backup Components when we're done
				DestroyObject(backupComponent);
			}
		}

	}
}
