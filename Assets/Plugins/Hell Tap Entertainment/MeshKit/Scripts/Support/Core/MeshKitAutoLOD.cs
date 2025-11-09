////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MeshKitAutoLOD.cs
//
//	This component allows us to create LODs. This is a modified version of DecimateObject.cs
//	specifically designed for MeshKit.
//
//	© 2018-2024 Melli Georgiou.
//	Hell Tap Entertainment LTD
//
////////////////////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace HellTap.MeshKit {

	[DisallowMultipleComponent]
	[AddComponentMenu("MeshKit/Automatic LOD")]
	public sealed class MeshKitAutoLOD : MonoBehaviour {

		// Should we use advanced mode in the editor?
		[HideInInspector]
		public bool advancedMode = false;

		// Decimation Options
		[HideInInspector]
		public bool preserveBorders = false;
		[HideInInspector]
		public bool preserveSeams = false;
		[HideInInspector]
		public bool preserveFoldovers = false;
		[HideInInspector]
		public bool preserveSurfaceCurvature = false;
		[HideInInspector]
		public bool enableSmartLink = true;				// <- This should always be true, as some other options rely on it.

		// ADD OPTIONS FOR LOD GROUP (Fade Mode and CrossFading)
		//lodGroup.animateCrossFading = generatorHelper.AnimateCrossFading;
        //lodGroup.fadeMode = generatorHelper.FadeMode;
		
		// Variables (these are public so it can be modified by the MeshKit GUI)
		[HideInInspector]
		public LODSettings[] levels = null;
		
		/*
		// Used In Decimator System V1
		[HideInInspector]
		[Range(0f,99.9f)]
		public float cullingDistance = 1f;	// The LOD Distance where the object is culled (old system)
		*/

		[HideInInspector]
		public bool generated = false;

		////////////////////////////////////////////////////////////////////////////////////////////////
		//	LEVELS
		//	Gets or sets the LOD levels of this object.
		////////////////////////////////////////////////////////////////////////////////////////////////

		public LODSettings[] Levels {

			get { 
			
				// If we're using the "Easy" mode, always use the default settings
				if( advancedMode == false ){

					/*

					// DECIMATOR SYSTEM V1
					return new LODSettings[]{
						new LODSettings(0.8f,  50f, SkinQuality.Auto, true, ShadowCastingMode.On),
						new LODSettings(0.65f, 16f, SkinQuality.Bone2, true, ShadowCastingMode.Off, MotionVectorGenerationMode.Object, false),
						new LODSettings(0.4f,  7f, SkinQuality.Bone1, false, ShadowCastingMode.Off, MotionVectorGenerationMode.Object, false)
					};

					*/

					// DECIMATOR SYSTEM V2
					return new LODSettings[]{
						new LODSettings(1f,  	60f,	SkinQuality.Auto,	true,	ShadowCastingMode.On),	// <- First one should have full quality
						new LODSettings(0.8f, 	35f,	SkinQuality.Bone2,	true,	ShadowCastingMode.On,	MotionVectorGenerationMode.Object,	false),
						new LODSettings(0.5f,	15f,	SkinQuality.Bone1,	false,	ShadowCastingMode.Off,	MotionVectorGenerationMode.Object,	false),
						new LODSettings(0.25f,	1f,		SkinQuality.Bone1,	false,	ShadowCastingMode.Off,	MotionVectorGenerationMode.Object,	false) 	
						// <- The last entry culls below the distance value.
					};

				// Otherwise, use cutom setup
				} else {
					return levels; 
				}
			}

			set { levels = value; }
		}

		////////////////////////////////////////////////////////////////////////////////////////////////
		//	IS GENERATED
		//	Gets if this decimated object has been generated.
		////////////////////////////////////////////////////////////////////////////////////////////////

		public bool IsGenerated{

			get { return generated; }
		}

		////////////////////////////////////////////////////////////////////////////////////////////////
		//	RESET
		//	Resets the levels and settings to default
		////////////////////////////////////////////////////////////////////////////////////////////////

		public void Reset(){

			/*

			// DECIMATOR SYSTEM V1
			levels = new LODSettings[]{
				new LODSettings(0.8f,  50f, SkinQuality.Auto, true, ShadowCastingMode.On),
				new LODSettings(0.65f, 16f, SkinQuality.Bone2, true, ShadowCastingMode.Off, MotionVectorGenerationMode.Object, false),
				new LODSettings(0.4f,  7f, SkinQuality.Bone1, false, ShadowCastingMode.Off, MotionVectorGenerationMode.Object, false)
			};

			cullingDistance = 1f;

			*/

			// DECIMATOR SYSTEM V2
			levels = new LODSettings[]{
				new LODSettings(1f,  	60f,	SkinQuality.Auto,	true,	ShadowCastingMode.On),	// <- First one should have full quality
				new LODSettings(0.8f, 	35f,	SkinQuality.Bone2,	true,	ShadowCastingMode.On,	MotionVectorGenerationMode.Object,	false),
				new LODSettings(0.5f,	15f,	SkinQuality.Bone1,	false,	ShadowCastingMode.Off,	MotionVectorGenerationMode.Object,	false),
				new LODSettings(0.25f,	1f,		SkinQuality.Bone1,	false,	ShadowCastingMode.Off,	MotionVectorGenerationMode.Object,	false) 	
				// <- The last entry culls below the distance value.
			};

			// Reset LODs
			ResetLODs();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////
		//	GENERATE LODs
		//	Generates the LODs for this object. The statusCallback variable is a status report callback.
		////////////////////////////////////////////////////////////////////////////////////////////////

		public void GenerateLODs( /*LODStatusReportCallback statusCallback = null*/ bool showProgressBarInEditor = false ){

			// Make sure levels is setup
			if (levels != null){

				/*

				// DECIMATOR SYSTEM V1
				LODGenerator.GenerateLODs(gameObject, Levels, statusCallback, preserveBorders, preserveSeams, preserveFoldovers );	// <- Changed so easy / advanced mode works dynamically
				
				*/

				// DECIMATOR SYSTEM V2

				// Destroy the current AutoLOD setup before re-applying it
				if( generated == true ){
					HellTap.UnityMeshSimplifier.MeshKitLODGeneratorV2.DestroyLODs(gameObject);
				}

				// Cache the simplification options and mofify them
				var simplificationOptions = UnityMeshSimplifier.SimplificationOptions.Default;

				// Always override Enable Smart Link to true
				simplificationOptions.EnableSmartLink = true;

				// New Settings in V2 - Do defaults
				if( advancedMode == false ){

					simplificationOptions.PreserveBorderEdges = false;
					simplificationOptions.PreserveUVSeamEdges = false;
					simplificationOptions.PreserveUVFoldoverEdges = false;
					simplificationOptions.PreserveSurfaceCurvature = false;

				// Use Custom Setting If Advanced Mode Is ON
				} else {

					simplificationOptions.PreserveBorderEdges = preserveBorders;
					simplificationOptions.PreserveUVSeamEdges = preserveSeams;
					simplificationOptions.PreserveUVFoldoverEdges = preserveFoldovers;
					simplificationOptions.PreserveSurfaceCurvature = preserveSurfaceCurvature;
				}

				// New Settings (not implemented in MeshKit yet so use defaults)
				simplificationOptions.VertexLinkDistance = double.Epsilon;
				simplificationOptions.MaxIterationCount = 100;
				simplificationOptions.Agressiveness = 7.0;
				simplificationOptions.ManualUVComponentCount = false;
				simplificationOptions.UVComponentCount = 2;

				// Tell the new Decimation System to generate the LODs
				LODGroup createdLODGroup = HellTap.UnityMeshSimplifier.MeshKitLODGeneratorV2.GenerateLODs( gameObject, ConvertLODSettingsToLODLevels(), true, simplificationOptions, showProgressBarInEditor );

				// ----------------------------------------------
				// APPLY GAMEOBJECT AND LAYER ADDITIONS MANUALLY
				// ----------------------------------------------

				// Make sure we recieved a created LODGroup
				if( createdLODGroup != null ){

					// Cache the created LODs
					LOD[] lods = createdLODGroup.GetLODs();

					// Cache the LODSettings we had at first
					var lodSettings = Levels;

					// Setup GameObject tags and layer overrides for each level
					if( lods.Length == lodSettings.Length ){
						for( int i = 0; i < lods.Length; i++ ){
							foreach( Renderer r in lods[i].renderers ){
								if( r != null ){
									r.gameObject.layer = lodSettings[i].layer;
									r.gameObject.tag = lodSettings[i].tag;
								}
							}
						}
					} else {
						Debug.LogWarning( "MESHKIT AUTOLOD: The Created LOD Levels and LODSettings don't match. Can't apply GameObject and Layer overrides.");
					}
				}

				// Set Generated to true
				generated = true;

			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////
		//	CONVERT LODSETTINGS TO LODLEVELS
		//	Converts the structs so they can be used in the new Decimation System
		////////////////////////////////////////////////////////////////////////////////////////////////

		public UnityMeshSimplifier.LODLevel[] ConvertLODSettingsToLODLevels(){

			// Cache the LODSettings
			var lodSettings = Levels;

			// Create a list to store the list of LODLevels
			var lodLevelList = new List<UnityMeshSimplifier.LODLevel>();

			// Convert each of the LODSettings into LODLevels and put them in the list
			foreach( var lodSetting in lodSettings ){ 
				lodLevelList.Add( lodSetting.ToLODLevel() );
			}

			// return the list as an array
			return lodLevelList.ToArray();

		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		//	RESET LODS
		//	Resets the LODs for this object.
		////////////////////////////////////////////////////////////////////////////////////////////////

		public void ResetLODs(){

			/*
				
			// DECIMATOR SYSTEM V1	
			LODGenerator.DestroyLODs(gameObject);

			*/

			// DECIMATOR SYSTEM V2
			HellTap.UnityMeshSimplifier.MeshKitLODGeneratorV2.DestroyLODs(gameObject);
			

			// Update status
			generated = false;
			advancedMode = false;
		}

	}
}