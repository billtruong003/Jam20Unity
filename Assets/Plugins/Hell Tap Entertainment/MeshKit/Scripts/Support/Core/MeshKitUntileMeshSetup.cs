////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MeshKitUntileMeshSetup.cs
//
//  This component allows us to setup and create untiled meshes.
//
//  © 2022 Melli Georgiou.
//  Hell Tap Entertainment LTD
//
////////////////////////////////////////////////////////////////////////////////////////////////

// NOTES: Comment this out to see values in inspector
#define HIDE_VALUES_IN_EDITOR

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HellTap.MeshKit {

	[DisallowMultipleComponent]
	[AddComponentMenu("MeshKit/Untile Setup")]
	public sealed class MeshKitUntileMeshSetup : MonoBehaviour {

	/// -> VARIABLES

		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public UntileMode untileMode = UntileMode.RescaleToFit;
		public enum UntileMode {
			RescaleToFit,																		// <- Remaps the UVs and scales them to fit in the 0 - 1 space.
			RescaleWithAtlasing																	// <- Attempts to recreate the tiling by creating new tiling atlases.
		}

		
		// Maximum size of Atlas
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public MaxAtlasSize maximumAtlasSize = MaxAtlasSize._2048;
			public enum MaxAtlasSize { _2048 = 0, _4096 = 1, _8192 = 2 }							// <- 2048 will be the smallest size you can pick for untiling.

			
		// ------------------------
		//	CACHED COMBINE DATA
		// ------------------------

		// Store results of the combine so we can revert it later if needed.
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#else
			[Header("Results")]
		#endif
		public Mesh originalMesh = null;							// The original mesh used on this GameObject
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public Material originalMaterial = null;					// The original Material used on this GameObject

		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public UntileMode generatedUntileMode = UntileMode.RescaleToFit;
		
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public bool generated = false;


		// ------------------------
		//	SAVE ASSETS HERE
		// ------------------------

		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#else
			[Header("Save Assets Here:")]
		#endif
		public string assetRelativePathToFolder = string.Empty;

	/// -> IS GENERATED

		////////////////////////////////////////////////////////////////////////////////////////////////
		//	IS GENERATED
		//	Gets if this object has already been combined.
		////////////////////////////////////////////////////////////////////////////////////////////////

		public bool IsGenerated {

			get { return generated; }
		}

	/// -> UNDO COMBINE

		////////////////////////////////////////////////////////////////////////////////////////////////
		//	UNDO UNTILE
		//	Restore this GameObject to how it was before the Combine process
		////////////////////////////////////////////////////////////////////////////////////////////////

		[ContextMenu("Undo Untile Operation")]	// <- Gives us a dropdown menu in the inspector!

		// MAIN PUBLIC METHOD (This will call one of the variant methods below)
		public void UndoUntile() {

			// Make sure we've generated a mesh ...
			if( generated == true ){

				#if UNITY_EDITOR

					// If the editor isn't playing or will change play mode, allow us to make this operation 'undoable'
					if( UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false ){

						UndoUntile_InEditorWithUndo();

					// Otherwise do it the standard way
					} else {

						UndoUntile_AtRuntime();

					}

				// Always do this without undo if we're not in the Editor.
				#else

					UndoUntile_AtRuntime();

				#endif
			}
		}

		// METHOD VARIANT 1 -> Editor Version with undo functionality
		#if UNITY_EDITOR

			void UndoUntile_InEditorWithUndo(){

				// Cache Relevant Components
				var mf = GetComponent<MeshFilter>();
				var mr = GetComponent<MeshRenderer>();
				var smr = GetComponent<SkinnedMeshRenderer>();

				// Setup Skinned Mesh Renderer if it exists
				if( mf != null ){ 
					UnityEditor.Undo.RecordObject ( mf, "Undo Untile Operation");
					mf.sharedMesh = originalMesh;
				}

				// Setup Skinned Mesh Renderer if it exists
				if( mr != null ){ 
					UnityEditor.Undo.RecordObject ( mr, "Undo Untile Operation");
					mr.sharedMaterial = originalMaterial;
				}

				// Setup Skinned Mesh Renderer if it exists
				if( smr != null ){ 
					UnityEditor.Undo.RecordObject ( smr, "Undo Untile Operation");
					smr.sharedMesh = originalMesh;
					smr.sharedMaterial = originalMaterial;
				}

				// Record the state of this component
				UnityEditor.Undo.RecordObject ( this, "Undo Untile Operation");

				// Set generated to false
				generated = false;

			}

		#endif

		// METHOD VARIANT 2 -> Runtime version without Undo functionality
		void UndoUntile_AtRuntime(){

			// Cache Relevant Components
				var mf = GetComponent<MeshFilter>();
				var mr = GetComponent<MeshRenderer>();
				var smr = GetComponent<SkinnedMeshRenderer>();

				// Setup Skinned Mesh Renderer if it exists
				if( mf != null ){ 
					mf.sharedMesh = originalMesh;
				}

				// Setup Skinned Mesh Renderer if it exists
				if( mr != null ){ 
					mr.sharedMaterial = originalMaterial;
				}

				// Setup Skinned Mesh Renderer if it exists
				if( smr != null ){ 
					smr.sharedMesh = originalMesh;
					smr.sharedMaterial = originalMaterial;
				}

				// Set generated to false
				generated = false;

		}
	}
}