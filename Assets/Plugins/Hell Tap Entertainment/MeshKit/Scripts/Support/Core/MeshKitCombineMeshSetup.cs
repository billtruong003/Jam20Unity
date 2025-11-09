////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MeshKitCombineMeshSetup.cs
//
//  This component allows us to setup and create combined non-skinned Meshes with atlases.
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
	[AddComponentMenu("MeshKit/Combine MeshRenderer Setup")]
	public sealed class MeshKitCombineMeshSetup : MonoBehaviour {

	/// -> VARIABLES

		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public AtlasMode atlasMode = AtlasMode.OneAtlasForEachShader;
		public enum AtlasMode {
			OneAtlasForEachShader,																		// <- We'll create one atlas for every shader found
			ForceStandardShader																			// <- All Shaders will be combined to use the standard shader
		}
		
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#else
			[Header("Combine Shader Properties")]
		#endif
		// Property List - used to set which texture properties will be processed during atlassing
		public List<CombineMeshRendererSetup> propertyList = new List<CombineMeshRendererSetup>(){ 
			new CombineMeshRendererSetup( "_MainTex", MissingTextureFallback.White )
		};

		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#else
			[Header("Options")]
		#endif
		public bool bakeColorIntoMainTex = true;
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public bool rebuildLightmapUVs = true;

		// Maximum size of Atlas
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public MaxAtlasSize maximumAtlasSize = MaxAtlasSize._2048;
			public enum MaxAtlasSize { _1024 = 0, _2048 = 1, _4096 = 2, _8192 = 3 }

		// Max number of textures per atlas
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#else
			[Range(4,64)]
		#endif
		public int maxTexturesPerAtlas = 16;

		// Helpers -> Combine method breaks up the property list above to have seperate arrays.
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public string[] propertyNames = new string[0];

		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public MissingTextureFallback[] textureFallbacks = new MissingTextureFallback[0];

		// -> START OF NEW STUFF
		// NOTE:	This is not yet implemented in the GUI, even though it works! :)
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#else
			[Header("Advanced Pixel Processing")]
		#endif
		public bool useAdvancedPixelProcessing = false;
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public BakeFloatIntoTextureProperty[] bakeFloatsIntoTextures = new BakeFloatIntoTextureProperty[0];
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public BakeColorIntoTextureProperty[] bakeColorIntoTextures = new BakeColorIntoTextureProperty[0];
		// <- END OF NEW STUFF

			// -------------------------------------------
			// CLASS: COMBINE MESH RENDERER SETUP
			// -------------------------------------------
			
			[System.Serializable]
			public class CombineMeshRendererSetup {

				// Variables
				public string propertyName = "_MainTex";
				public MissingTextureFallback missingTextureFallback = MissingTextureFallback.White;

				// Default Constructor
				public CombineMeshRendererSetup(){
					propertyName = "_MainTex";
					missingTextureFallback = MissingTextureFallback.White;
				}

				// Custom Constructor
				public CombineMeshRendererSetup( string propertyName, MissingTextureFallback missingTextureFallback ){
					this.propertyName = propertyName;
					this.missingTextureFallback = missingTextureFallback;
				}
			}

				// If there are missing textures, we need to know how to replace them. This helps us know what to do on each texture type
				public enum MissingTextureFallback { White = 0, Grey = 1, TransparentBlack = 2, Normal = 3 }

			
			// -------------------------------
			// EDITOR: DEFAULT PROPERTY LISTS
			// -------------------------------

			// Drop-down Helper Lists
			#if HIDE_VALUES_IN_EDITOR
				[HideInInspector]
			#endif
			public DropDownHelperList dropDownHelperList = DropDownHelperList.Select;
				public enum DropDownHelperList {
					Select = 0,
					UnlitShader = 1,
					BumpedShader = 2,
					StandardShader = 3
				}

			// Default For Unlit Shader
			#if HIDE_VALUES_IN_EDITOR
				[HideInInspector]
			#endif
			public readonly List<CombineMeshRendererSetup> _defaultUnlitPropertyList =new List<CombineMeshRendererSetup>(){ 
				new CombineMeshRendererSetup( "_MainTex", MissingTextureFallback.White )
			};

			// Default For Bumped Shader
			#if HIDE_VALUES_IN_EDITOR
				[HideInInspector]
			#endif
			public readonly List<CombineMeshRendererSetup> _defaultBumpedPropertyList =new List<CombineMeshRendererSetup>(){ 
				new CombineMeshRendererSetup( "_MainTex", MissingTextureFallback.White ),
				new CombineMeshRendererSetup( "_BumpMap", MissingTextureFallback.Normal )
			};

			// Default For Standard Shader
			#if HIDE_VALUES_IN_EDITOR
				[HideInInspector]
			#endif
			public readonly List<CombineMeshRendererSetup> _defaultStandardPropertyList =new List<CombineMeshRendererSetup>(){ 
				new CombineMeshRendererSetup( "_MainTex", MissingTextureFallback.White ),
				new CombineMeshRendererSetup( "_BumpMap", MissingTextureFallback.Normal ),
				new CombineMeshRendererSetup( "_MetallicGlossMap", MissingTextureFallback.Grey ),
				new CombineMeshRendererSetup( "_OcclusionMap", MissingTextureFallback.White ),
				new CombineMeshRendererSetup( "_EmissionMap", MissingTextureFallback.TransparentBlack )
			};
			
		// ------------------------
		//	CACHED COMBINE DATA
		// ------------------------

		// Store results of the combine so we can revert it later if needed.
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#else
			[Header("Results")]
		#endif
		public MeshFilter[] originalMFs = new MeshFilter[0];					// Original MeshFilterrs
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public MeshRenderer[] originalMRs = new MeshRenderer[0];				// Original MeshRenderers
		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public GameObject[] newCombinedGameObjects = new GameObject[0];		// <- we'll cache all the new GameObjects that were created

		#if HIDE_VALUES_IN_EDITOR
			[HideInInspector]
		#endif
		public AtlasMode generatedAtlasMode = AtlasMode.OneAtlasForEachShader;
		
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
		//	UNDO COMBINE
		//	Restore this GameObject to how it was before the Combine process
		////////////////////////////////////////////////////////////////////////////////////////////////

		[ContextMenu("Undo Combine Operation")]	// <- Gives us a dropdown menu in the inspector!

		// MAIN PUBLIC METHOD (This will call one of the variant methods below)
		public void UndoCombine() {

			// Make sure we've generated a mesh ...
			if( generated == true ){

				#if UNITY_EDITOR

					// If the editor isn't playing or will change play mode, allow us to make this operation 'undoable'
					if( UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false ){

						UndoCombine_InEditorWithUndo();

					// Otherwise do it the standard way
					} else {

						UndoCombine_AtRuntime();

					}

				// Always do this without undo if we're not in the Editor.
				#else

					UndoCombine_AtRuntime();

				#endif
			}
		}

		// METHOD VARIANT 1 -> Editor Version with undo functionality
		#if UNITY_EDITOR

			void UndoCombine_InEditorWithUndo(){

				// Destroy the new combined GameObjects
				for( int i = 0; i < newCombinedGameObjects.Length; i++ ){
					if( newCombinedGameObjects[i] != null ){
						UnityEditor.Undo.DestroyObjectImmediate ( newCombinedGameObjects[i] );
					}
				}

				// Enable the original Mesh Renderers
				for( int i = 0; i < originalMRs.Length; i++ ){
					if( originalMRs[i] != null ){
						UnityEditor.Undo.RecordObject ( originalMRs[i], "Undo Combine Operation");
						originalMRs[i].enabled = true;
					}
				}

				// Record the state of this component
				UnityEditor.Undo.RecordObject ( this, "Undo Combine Operation");

				// Set generated to false
				generated = false;

			}

		#endif

		// METHOD VARIANT 2 -> Runtime version without Undo functionality
		void UndoCombine_AtRuntime(){

			// Destroy the new combined GameObjects
			for( int i = 0; i < newCombinedGameObjects.Length; i++ ){
				if( newCombinedGameObjects[i] != null ){
					DestroyImmediate ( newCombinedGameObjects[i] );
				}
			}

			// Enable the original Mesh Renderers
			for( int i = 0; i < originalMRs.Length; i++ ){
				if( originalMRs[i] != null ){
					originalMRs[i].enabled = true;
				}
			}
		
			// Set generated to false
			generated = false;

		}

	}
}