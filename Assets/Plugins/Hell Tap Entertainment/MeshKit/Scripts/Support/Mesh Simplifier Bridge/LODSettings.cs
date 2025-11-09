////////////////////////////////////////////////////////////////////////////////////////////////
//
//  LODSettings.cs
//
//	This is a bridge between MeshKit's inspector and the Decimation System. The older system
//	used this, so in order to transition to the new one and not break existing setups, we need
//	this sytem to bridge the gap.
//
//	© 2024 Melli Georgiou.
//	Hell Tap Entertainment LTD
//
////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HellTap.MeshKit {

	/// <summary>
	/// LOD Level Settings.
	/// </summary>
	[System.Serializable]
	public struct LODSettings {

		// Helpers
		const string _UNTAGGED = "Untagged";

		////////////////////////////////////////////////////////////////////////////////////////////////
		//	VALUES
		////////////////////////////////////////////////////////////////////////////////////////////////
		
		// LOD DISTANCE
		[Header("LOD Distance")]
		
		/// The LOD Percentage. 1 = 100.
		[Range(0.01f, 100f)]
		[Tooltip("At what distance should this LOD be shown? 100 is used for the best quality mesh.")]
		public float lodDistancePercentage; // <- Included For MeshKit.
		
		
		// DECIMATION
		[Header("Decimation")]
		
		/// The LOD level quality between 0 and 1.
		[Range(0.01f, 1f)]
		[Tooltip("When decimating, a value of 0 will reduce mesh complexity as much as possible. 1 will preserve it.")]
		public float quality;

		/// If the meshes should be combined into one.
		[HideInInspector]
		[Tooltip("Combining Meshes should always be false in MeshKit.")]
		public bool combineMeshes;  // <- this is hidden and should always be false
		

		// RENDERERS
		[Header("Renderers")]

		/// The LOD level skin quality.
		[Tooltip("The Skin Quality setting used in the Renderer.")]
		public SkinQuality skinQuality;

		/// If the LOD level receives shadows.
		[Tooltip("The Recieve Shadows setting used in the Renderer.")]
		public bool receiveShadows;

		/// The LOD level shadow casting mode.
		[Tooltip("The Shadow Casting setting used in the Renderer.")]
		public ShadowCastingMode shadowCasting;

		/// The LOD level motion vectors generation mode.
		[Tooltip("The Motion Vectors setting used in the Renderer.")]
		public MotionVectorGenerationMode motionVectors;

		/// If the LOD level uses skinned motion vectors.
		[Tooltip("The Skinned Motion Vectors setting used in the Renderer.")]
		public bool skinnedMotionVectors;

		/// The LOD level light probe usage.
		[Tooltip("The Light Probe Usage setting found in the Renderer.")]
		public LightProbeUsage lightProbeUsage;

		/// The LOD level reflection probe usage.
		[Tooltip("The Reflection Probe Usage setting found in the Renderer.")]
		public ReflectionProbeUsage reflectionProbeUsage;


		// GAMEOBJECT
		[Header("GameObject")]

		/// The tag to use on the GameObject.
		[Tooltip("The tag to use on the GameObject.")]
		public string tag;

		/// The layer to use on the GameObject.
		[Tooltip("The layer to use on the GameObject.")]
		public int layer;


		////////////////////////////////////////////////////////////////////////////////////////////////
		//	CONSTRUCTORS FOR LODSETTINGS
		////////////////////////////////////////////////////////////////////////////////////////////////

		/// Creates new LOD Level Settings.
		public LODSettings( 
			float quality, 
			float lodDistancePercentage = 0.8f
		){

			this.quality = quality;
			this.lodDistancePercentage = lodDistancePercentage;
			this.combineMeshes = false;
			this.skinQuality = SkinQuality.Auto;
			this.receiveShadows = true;
			this.shadowCasting = ShadowCastingMode.On;
			this.motionVectors = MotionVectorGenerationMode.Object;
			this.skinnedMotionVectors = true;

			this.lightProbeUsage = LightProbeUsage.BlendProbes;
			this.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

			// Use default tag and layer
			this.tag = _UNTAGGED;   // "Untagged"
			this.layer = 0;         // 0 = default layer
		}

		/// Creates new LOD Level Settings.
		public LODSettings(
			float quality, 
			float lodDistancePercentage, 
			SkinQuality skinQuality 
		){

			this.quality = quality;
			this.lodDistancePercentage = lodDistancePercentage;
			this.combineMeshes = false;
			this.skinQuality = skinQuality;
			this.receiveShadows = true;
			this.shadowCasting = ShadowCastingMode.On;
			this.motionVectors = MotionVectorGenerationMode.Object;
			this.skinnedMotionVectors = true;

			this.lightProbeUsage = LightProbeUsage.BlendProbes;
			this.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

			// Use default tag and layer
			this.tag = _UNTAGGED;   // "Untagged"
			this.layer = 0;         // 0 = default layer
		}

		/// Creates new LOD Level Settings.
		public LODSettings(
			float quality, 
			float lodDistancePercentage, 
			SkinQuality skinQuality, 
			bool receiveShadows, 
			ShadowCastingMode shadowCasting 
		){

			this.quality = quality;
			this.lodDistancePercentage = lodDistancePercentage;
			this.combineMeshes = false;
			this.skinQuality = skinQuality;
			this.receiveShadows = receiveShadows;
			this.shadowCasting = shadowCasting;
			this.motionVectors = MotionVectorGenerationMode.Object;
			this.skinnedMotionVectors = true;

			this.lightProbeUsage = LightProbeUsage.BlendProbes;
			this.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

			// Use default tag and layer
			this.tag = _UNTAGGED;   // "Untagged"
			this.layer = 0;         // 0 = default layer
		}

		/// Creates new LOD Level Settings.
		public LODSettings(
			float quality, 
			float lodDistancePercentage, 
			SkinQuality skinQuality, 
			bool receiveShadows, 
			ShadowCastingMode shadowCasting, 
			MotionVectorGenerationMode motionVectors, 
			bool skinnedMotionVectors
		){

			this.quality = quality;
			this.lodDistancePercentage = lodDistancePercentage;
			this.combineMeshes = false;
			this.skinQuality = skinQuality;
			this.receiveShadows = receiveShadows;
			this.shadowCasting = shadowCasting;
			this.motionVectors = motionVectors;
			this.skinnedMotionVectors = skinnedMotionVectors;

			this.lightProbeUsage = LightProbeUsage.BlendProbes;
			this.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

			// Use default tag and layer
			this.tag = _UNTAGGED;   // "Untagged"
			this.layer = 0;         // 0 = default layer
		}

		/// Creates new LOD Level Settings.
		public LODSettings(
			float quality, 
			float lodDistancePercentage, 
			SkinQuality skinQuality, 
			bool receiveShadows, 
			ShadowCastingMode shadowCasting, 
			MotionVectorGenerationMode motionVectors, 
			bool skinnedMotionVectors, 
			LightProbeUsage lightProbeUsage, 
			ReflectionProbeUsage reflectionProbeUsage 
		){

			this.quality = quality;
			this.lodDistancePercentage = lodDistancePercentage;
			this.combineMeshes = false;
			this.skinQuality = skinQuality;
			this.receiveShadows = receiveShadows;
			this.shadowCasting = shadowCasting;
			this.motionVectors = motionVectors;
			this.skinnedMotionVectors = skinnedMotionVectors;
			this.lightProbeUsage = lightProbeUsage;
			this.reflectionProbeUsage = reflectionProbeUsage;

			// Use default tag and layer
			this.tag = _UNTAGGED;   // "Untagged"
			this.layer = 0;         // 0 = default layer
		}

		/// Creates new LOD Level Settings.
		public LODSettings(
			float quality, 
			float lodDistancePercentage, 
			SkinQuality skinQuality, 
			bool receiveShadows, 
			ShadowCastingMode shadowCasting, 
			MotionVectorGenerationMode motionVectors, 
			bool skinnedMotionVectors, 
			LightProbeUsage lightProbeUsage, 
			ReflectionProbeUsage reflectionProbeUsage, 
			string tag, 
			int layer
		){

			this.quality = quality;
			this.lodDistancePercentage = lodDistancePercentage;
			this.combineMeshes = false;
			this.skinQuality = skinQuality;
			this.receiveShadows = receiveShadows;
			this.shadowCasting = shadowCasting;
			this.motionVectors = motionVectors;
			this.skinnedMotionVectors = skinnedMotionVectors;
			this.lightProbeUsage = lightProbeUsage;
			this.reflectionProbeUsage = reflectionProbeUsage;
			this.tag = tag;
			this.layer = layer;
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		//	CONVERT TO LODLEVEL (for UnityMeshSimplifier Decimation System)
		//	Convert the LODSettings into a LODLevel that can be used by the new system
		////////////////////////////////////////////////////////////////////////////////////////////////

		public UnityMeshSimplifier.LODLevel ToLODLevel(){

			// NOTE: We had to make the fields of LODLevel.cs public for this to work!
			var lod = new UnityMeshSimplifier.LODLevel();

			// ----------------
			// LOD TRANSITIONS
			// ----------------

			// Convert MeshKit's LOD Distance Percentage into a 0 - 1 value.
			lod.screenRelativeTransitionHeight = lodDistancePercentage * 0.01f;

			// The width of the cross-fade transition zone (proportion to the current LOD's whole length)
			lod.fadeTransitionWidth = 0f; 	// <- we currently don't have a MeshKit value for this yet!

			// ----------------
			// RENDERERS
			// ----------------

			// The Renderer[] array used in this level
		//	lod.renderers = 	// <- strangely, we don't have this in ours?

			// ----------------
			// QUALITY SETTINGS
			// ----------------

			// Quality of the decimation ( 0 = full decimation, 1 = full quality )
			lod.quality = quality;

			// If all renderers and meshes under this level should be combined into one, where possible.
			lod.combineMeshes = false; 	// this should always be false

			// If all sub-meshes should be combined into one, where possible.
			lod.combineSubMeshes = false; 	// this should always be false

			// The skin quality to use for renderers on this level
			lod.skinQuality = skinQuality;

			// The shadow casting mode for renderers on this level
			lod.shadowCastingMode = shadowCasting;

			// If renderers on this level should receive shadows
			lod.receiveShadows = receiveShadows;

			// The motion vector generation mode for renderers on this level
			lod.motionVectorGenerationMode = motionVectors;

			// If renderers on this level should use skinned motion vectors.
			lod.skinnedMotionVectors = skinnedMotionVectors;

			// The light probe usage for renderers on this level.
			lod.lightProbeUsage = lightProbeUsage;

			// The reflection probe usage for renderers on this level
			lod.reflectionProbeUsage = reflectionProbeUsage;

			// -------------------
			// RETURN LODSettings
			// -------------------

			return lod;
		}

	}

}
