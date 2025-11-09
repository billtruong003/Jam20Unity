////////////////////////////////////////////////////////////////////////////////////////////////
//
//  EditorMeshRendererUtility.cs
//
//	EDITOR Methods for combining Mesh Renderers with atlases
//
//	© 2022 Melli Georgiou.
//	Hell Tap Entertainment LTD
//
////////////////////////////////////////////////////////////////////////////////////////////////

// Compilation Helpers
#define SHOW_PROGRESS_BARS

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Use HellTap Namespace
namespace HellTap.MeshKit {

	public class EditorMeshRendererUtility : MonoBehaviour {

/// -> [EDITOR ONLY] DEBUG MENU ITEMS

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] DEBUG MENU ITEMS
		//	Helps to quickly test experimental functionality
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/*
		// --------------------
		// QUICK DEBUG HELPERS
		// --------------------

		// DEBUG QUICK: Helper strings for default texture properties to extract ("_MainTex" should ALWAYS be included!)
		static readonly string[] defaultPropertyNamesSimpleBump = new string[] { "_MainTex","_BumpMap" };
		static readonly MeshKitCombineMeshSetup.MissingTextureFallback[] defaultTextureFallbacksSimpleBump = new MeshKitCombineMeshSetup.MissingTextureFallback[]{ 
			MeshKitCombineMeshSetup.MissingTextureFallback.White, MeshKitCombineMeshSetup.MissingTextureFallback.Normal
		};

		// Helper strings for default texture properties to extract ("_MainTex" should ALWAYS be included!)
		static readonly string[] defaultPropertyNames = new string[] { "_MainTex","_BumpMap","_MetallicGlossMap","_OcclusionMap", "_EmissionMap" };
		static readonly MeshKitCombineMeshSetup.MissingTextureFallback[] defaultTextureFallbacks = new MeshKitCombineMeshSetup.MissingTextureFallback[]{ 
			MeshKitCombineMeshSetup.MissingTextureFallback.White, MeshKitCombineMeshSetup.MissingTextureFallback.Normal, MeshKitCombineMeshSetup.MissingTextureFallback.Grey, MeshKitCombineMeshSetup.MissingTextureFallback.White, MeshKitCombineMeshSetup.MissingTextureFallback.TransparentBlack
		};

		// --------------------
		// QUICK DEBUG METHODS
		// --------------------

		// DEBUG Method: Shortcut From Menu - REMOVE LATER!
		[MenuItem ("Assets/Combine MeshRenderers (With Atlassing - SIMPLE BUMP - 1 Atlas per shader )")]
		static void CombineMeshRenderersSimpleBump(){ 
			CombineMeshRenderers( Selection.activeGameObject, MeshKitCombineMeshSetup.MaxAtlasSize._2048, defaultPropertyNamesSimpleBump, defaultTextureFallbacksSimpleBump, true, true, "Assets/ZZZ TEST", MeshKitCombineMeshSetup.AtlasMode.OneAtlasForEachShader, 16, null );
		}

		// DEBUG Method: Shortcut From Menu - REMOVE LATER!
		[MenuItem ("Assets/Combine MeshRenderers (With Atlassing - SIMPLE BUMP - Force Standard Shader)")]
		static void CombineMeshRenderersSimpleBumpForceStandard(){ 
			CombineMeshRenderers( Selection.activeGameObject, MeshKitCombineMeshSetup.MaxAtlasSize._2048, defaultPropertyNamesSimpleBump, defaultTextureFallbacksSimpleBump, true, true, "Assets/ZZZ TEST", MeshKitCombineMeshSetup.AtlasMode.ForceStandardShader, 16, null );
		}

		// DEBUG Method: Shortcut From Menu - REMOVE LATER!
		[MenuItem ("Assets/Combine MeshRenderers (With Atlassing - SIMPLE BUMP - 1 Atlas per shader, 2 textures max per atlas )")]
		static void CombineMeshRenderersSimpleBump2MaxTextures(){ 
			CombineMeshRenderers( Selection.activeGameObject, MeshKitCombineMeshSetup.MaxAtlasSize._2048, defaultPropertyNamesSimpleBump, defaultTextureFallbacksSimpleBump, true, true, "Assets/ZZZ TEST", MeshKitCombineMeshSetup.AtlasMode.OneAtlasForEachShader, 2, null );
		}

		// DEBUG Method: Shortcut From Menu - REMOVE LATER!
		[MenuItem ("Assets/Combine MeshRenderers (With Atlassing And All Properties)")]
		static void CombineMeshRenderers(){ 
			CombineMeshRenderers( Selection.activeGameObject, MeshKitCombineMeshSetup.MaxAtlasSize._2048, defaultPropertyNames, defaultTextureFallbacks, true, true, "Assets/ZZZ TEST", MeshKitCombineMeshSetup.AtlasMode.OneAtlasForEachShader, 16, null );
		}
		*/

/// -> [EDITOR ONLY] HELPER TEXTURES & COLORS

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] HELPER TEXTURES & COLORS
		//	Combines multiple child mesh renderers into a new one, complete with atlassing
		//	to reduce draw calls. This is the most efficient way to combine.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Helper Textures
		private static Texture2D texture2DBlack;		// <- Fully Transparent
		private static Texture2D texture2DWhite;		// <- Fully Opaque
		private static Texture2D texture2DGrey;			// <- Half Transparent
		private static Texture2D texture2DNormal;		// <- Neutral Normal Texture

		// Helper Colors
		private static Color transparentBlackColor = new Color(0f,0f,0f,0f);
		private static Color whiteColor = new Color(1f,1f,1f,1f);
		private static Color greyColor = new Color(0.5f,0.5f,0.5f,0.5f);
		private static Color normalColor = new Color(0.5f,0.5f,1f,1f);

		// Converts The Missing Texture Fallback To A Color
		private static Color MissingTextureFallbackToColor( MeshKitCombineMeshSetup.MissingTextureFallback missingTextureFallback ){
			if( missingTextureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.TransparentBlack ){
				return transparentBlackColor;
			} else if( missingTextureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.White ){
				return whiteColor;
			} else if( missingTextureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.Grey ){
				return greyColor;
			} else if( missingTextureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.Normal ){
				return normalColor;
			}

			// Return black if something goes wrong
			return Color.black;
		}

		// Recreate Texture Method
		private static void RecreateHelperTextures(){

			// NOTE: All of these textures must be at least 2x2 otherwise our custom TextureRescale method breaks.

			// Black
			texture2DBlack = new Texture2D (2,2);
			texture2DBlack.SetPixel(0, 0, transparentBlackColor );
			texture2DBlack.SetPixel(0, 1, transparentBlackColor );
			texture2DBlack.SetPixel(1, 0, transparentBlackColor );
			texture2DBlack.SetPixel(1, 1, transparentBlackColor );
			texture2DBlack.Apply();

			// White
			texture2DWhite = new Texture2D (2,2);
			texture2DWhite.SetPixel(0, 0, whiteColor );
			texture2DWhite.SetPixel(0, 1, whiteColor );
			texture2DWhite.SetPixel(1, 0, whiteColor );
			texture2DWhite.SetPixel(1, 1, whiteColor );
			texture2DWhite.Apply();

			// Grey
			texture2DGrey = new Texture2D (2,2);
			texture2DGrey.SetPixel(0, 0, greyColor );
			texture2DGrey.SetPixel(0, 1, greyColor );
			texture2DGrey.SetPixel(1, 0, greyColor );
			texture2DGrey.SetPixel(1, 1, greyColor );
			texture2DGrey.Apply();

			// Normal
			texture2DNormal = new Texture2D (2,2);
			texture2DNormal.SetPixel(0, 0, normalColor );
			texture2DNormal.SetPixel(0, 1, normalColor );
			texture2DNormal.SetPixel(1, 0, normalColor );
			texture2DNormal.SetPixel(1, 1, normalColor );
			texture2DNormal.Apply();
		}	

/// -> [EDITOR ONLY] TEXTURE IMPORTER SETUP

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] TEXTURE IMPORTER SETUP
		//	This class helps us track the textures that need to be modified ( uncompressed, readable, etc ). We can do this in the Editor on demand
		//	and when we're done restore the textures back to their original settings.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Texture Importer Setup
		class TextureImporterSetup {

			// Texture2D Variables
			public Texture2D texture2D = null;
			public TextureFormat originalTextureFormat = TextureFormat.RGBA32;

			// AssetDatabase Variables
			public string assetPath = "";

			// TextureImporter Variables
			public TextureImporter ti = null;
			public bool wasReadable = false;
			public TextureImporterCompression originalCompression = TextureImporterCompression.Uncompressed;

			// TextureImporter Platform Override
			public string platformString;
			public bool usesPlatformOverride = false;
			int originalPlatformMaxTextureSize = 0;
			TextureImporterFormat originalPlatformTextureFmt;
			int originalPlatformCompressionQuality = 0;
			bool originalPlatformAllowsAlphaSplit = false;

			// Custom
			// Normal Map RedFix Required? ( On certain platforms such as "standalone", the red normalmap bugfix is needed )
			public bool requiresNormalMapRedFix = false;
	

			// Constructor
			public TextureImporterSetup ( Texture2D newTexture2D, TextureImporter newTi, string newAssetPath ){

				// Setup the TextureImporterSetup
				texture2D = newTexture2D;
				ti = newTi;
				wasReadable = newTi.isReadable;
				originalCompression = newTi.textureCompression;
				originalTextureFormat = newTexture2D.format;
				assetPath = newAssetPath;

				// Cache the platform string (used to access texture overrides)
				platformString = PlatformToString();

				// If this texture is using a platform override, cache it here as well
				if ( ti.GetPlatformTextureSettings ( 
						platformString, out originalPlatformMaxTextureSize, out originalPlatformTextureFmt, 
						out originalPlatformCompressionQuality, out originalPlatformAllowsAlphaSplit
					)
				){
					usesPlatformOverride = true;

				} else {

					usesPlatformOverride = false;
				}

				// On certain platforms, bugfixes are required. Add tested platforms below:
				//	Platforms that require the fix:	'Standalone' (tested on mac, linux - verify on windows too)
				//	Platforms that work without it: 'iOS'
				if( platformString == "Standalone" ){
					requiresNormalMapRedFix = true;			// <- The normal map has a red tint on this platform and needs to be processed in order to fix it.
				}

			}

			// Prepare Settings so we can read textures properly
			public void PrepareSettings(){

				// Texture Importer
				if( ti != null ){

					// Make sure the texture is readable
					ti.isReadable = true;
					ti.textureCompression = TextureImporterCompression.Uncompressed;

					// If we're using texture overrides, we need to explicilty tell it what format to use
					if( usesPlatformOverride == true ){

						// Grab the current platform's override and set the format to RGBA32 ...
						var platformOverrides = ti.GetPlatformTextureSettings( platformString );
						platformOverrides.format = TextureImporterFormat.RGBA32;
						
						// Set the new version
						ti.SetPlatformTextureSettings( platformOverrides );
					}
					
					// Update the asset
					//AssetDatabase.Refresh();
					//AssetDatabase.ImportAsset( assetPath );

					AssetDatabase.Refresh( ImportAssetOptions.ForceSynchronousImport );
					AssetDatabase.ImportAsset( assetPath, ImportAssetOptions.ForceSynchronousImport );


				}
			}

			// Validate Current Texture Format Settings
			public bool ValidateSettings(){

				// These are apparantly the only formats that allow us
				// to modify color data, etc.
				if( texture2D.format == TextureFormat.ARGB32 ||
					texture2D.format == TextureFormat.RGBA32 ||
					texture2D.format == TextureFormat.BGRA32 ||
					texture2D.format == TextureFormat.RGB24 ||
					texture2D.format == TextureFormat.Alpha8 ||
					texture2D.format == TextureFormat.RGBAFloat ||
					texture2D.format == TextureFormat.RGBAHalf
				){
					return true;
				}

				return false;
			}

			// Reset Settings
			public void ResetOriginalSettings(){

				// Texture Importer
				if( ti != null ){

					// Restore settings
					ti.isReadable = wasReadable;
					ti.textureCompression = originalCompression;

					// If we're using texture overrides, we need to explicilty tell it what format to use
					if( usesPlatformOverride == true ){

						// Grab the current platform's override and set the format back to it's original
						var platformOverrides = ti.GetPlatformTextureSettings( platformString );
						platformOverrides.format = originalPlatformTextureFmt;
						
						// Set the new version
						ti.SetPlatformTextureSettings( platformOverrides );
					}
					
					// Update the asset
					//AssetDatabase.Refresh();
					//AssetDatabase.ImportAsset( assetPath );

					AssetDatabase.Refresh( ImportAssetOptions.ForceSynchronousImport );
					AssetDatabase.ImportAsset( assetPath, ImportAssetOptions.ForceSynchronousImport );

				}
			}

			// Returns the platform string depending on the current platform
			string PlatformToString(){

				#if UNITY_STANDALONE
					return "Standalone";

				#elif UNITY_IOS
					return "iPhone";
					
				#elif UNITY_ANDROID
					return "Android";
					
				#elif UNITY_WEBGL
					return "WebGL";
					
				#elif UNITY_WSA
					return "Windows Store Apps";
					
				#elif UNITY_PS4
					return "PS4";
					
				#elif UNITY_XBOXONE
					return "XboxOne";
					
				#elif UNITY_SWITCH
					return "Nintendo Switch";
					
				#elif UNITY_TVOS
					return "tvOS";

				#else 
					return "Standalone";

				#endif
			}
		}

		// Helper For Locating Specific Textures that could cause issues with normal maps
		static bool DoesTextureRequireRedNormalMapFix( List<TextureImporterSetup> setups, Texture2D tex ){
			for( int i = 0; i < setups.Count; i++ ){

				// If we find the matching texture
				if( setups[i].texture2D == tex ){

					// Return true if its original texture format was DXT5
					if( setups[i].requiresNormalMapRedFix == true ){
						return true;
					} else {
						return false;
					}
				}
			}

			// If we didn't find this texture, assume false.
			return false;
		}

		// Check if our Texture Importer Setup Already Contains A Texture
		static bool DoesTextureImporterSetupContainTexture( ref List<TextureImporterSetup> setups, Texture2D tex ){
			for( int i = 0; i < setups.Count; i++ ){

				// Return true if we find it...
				if( setups[i].texture2D == tex ){ 
					return true;
				}
			}

			// If we didn't find this texture, return false.
			return false;
		}

/// -> [EDITOR ONLY] HELPER CLASSES

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] HELPER CLASSES
		//	These classes help us store important data to facilitate complex combine tasks.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// TEXTURE PROPERTY SETUP CLASS
		// Class for setting up texture properties
		class TexturePropertySetup {

			// Setup
			public string propertyName = "";															// eg. "_MainTex"
			public MeshKitCombineMeshSetup.MissingTextureFallback textureFallback = MeshKitCombineMeshSetup.MissingTextureFallback.TransparentBlack;	// eg. use Transparent pixels if _MainTex is missing.
			
			// These values are synced (all indices refer to the same texture / material)
			public Material[] materials = new Material[0];												// The materials found in all renderers
			public Texture2D[] textures = new Texture2D[0];												// The textures found in all renderers
			public bool[] texturesOriginallyMissing = new bool[0];										// Track which of textures in the array were originally null
			public Color[] colorsToBakeIntoMainTex = new Color[0];										// What color should we bake into the main atlas ( per texture )

			// Results
			public Texture2D atlas = new Texture2D (1,1);												// The atlas for each texture property

			// Memory Cleanup
			public List<Texture2D> tempResizedTexturesToDelete = new List<Texture2D>();					// A List of temporary resized textures we created on demand.
		
			// Optimizations: Processed Textures
			public List<ProcessedTexture> redNormalProcessedTextures = new List<ProcessedTexture>();	// A List of textures we've already scaled and applied the red normal fix on.

		}

			// PROCESSED TEXTURE CLASS
			// Class to help us keep track of and dispose processed textures
			class ProcessedTexture {

				// Textures
				Texture2D original = null;
				Texture2D processed = null;

				// Quick Constructor
				public ProcessedTexture( Texture2D original, Texture2D processed ){
					this.original = original;
					this.processed = processed;
				}

				// If we're done with this, we can delete the texture to free memory
				public void Dispose(){
					if(processed != null ){ Object.DestroyImmediate( processed ); }
				}

				// Helper Method to find a previously fixed
				public static Texture2D ListContains( ref List<ProcessedTexture> list, Texture2D original, int processedWidth, int processedHeight ){
					
					// Loop through the Processed Textures...
					foreach( ProcessedTexture pt in list ){

						// If the original texture has been found and has a valid processed texture, return it.
						if( pt.original == original && pt.processed != null ){ 
						
							// Allow this to return true if the texture matches the size of the processed width and height
							if( pt.processed.width == processedWidth && pt.processed.height == processedHeight ){
							
								return pt.processed; 
							
							} else if( MeshKitGUI.verbose ){
								Debug.LogWarning( "TEX FOUND BUT SIZE MISMATCH FOR PROCESSED W/H ( " + processedWidth + " : " + processedHeight + " )\n- Original w: " +  pt.original.width + " h: " + pt.original.height +"\nProcessed w: " +  pt.processed.width + " h: " + pt.processed.height  );
							}
						}
					}

					// Otherwise, return null
					return null;
				}

				// Helper Method to to destroy all processed Texture2Ds in a list of ProcessedTextures
				public static void DisposeList( ref List<ProcessedTexture> list ){
					foreach( ProcessedTexture pt in list ){ pt.Dispose(); }
				}
			}


		// SHADER PASS DATA CLASS
		// Class that helps us organize atlas passes by shader
		[System.Serializable]
		class ShaderPassData {
			
			public Shader shader = null;																// Which shader to use in this pass
			public List<MeshRenderer> passMRs = new List<MeshRenderer>();								// List of Mesh Renderers to use in this pass
			public List<MeshFilter> passMFs = new List<MeshFilter>();									// List of Mesh Filters to use in this pass
			public List<Texture> mainTextures = new List<Texture>();									// Stores the mainTextures from each added object so far

			// Constructor
			public ShaderPassData( Shader shader ){
				this.shader = shader;
				passMRs = new List<MeshRenderer>();
				passMFs = new List<MeshFilter>();
				mainTextures = new List<Texture>();
			}
		}

/// -> [EDITOR ONLY] COMBINE MESH RENDERERS (WITH ATLASSING)

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] COMBINE MESH RENDERERS (WITH ATLASSING)
		//	Combines multiple child mesh renderers into a new one, complete with atlassing
		//	to reduce draw calls. This is the most efficient way to combine.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Helpers
		static readonly int MAX_VERTS_16BIT = 65534; 													// <- Max 16bit mesh value (65,535-1).
		static public bool attemptToFixOutOfRangeUVs = true;											// <- this should probably be left on permenantly

		// ----------------------------------------------------
		//	HELPER METHOD: ( from MeshKitCombineMeshSetup.cs )
		// ----------------------------------------------------

		public static bool CombineMeshRenderers( GameObject selectedGameObject, MeshKitCombineMeshSetup.MaxAtlasSize maximumAtlasSize, List<MeshKitCombineMeshSetup.CombineMeshRendererSetup> propertyList, bool bakeColorIntoMainTex, bool rebuildLightmapUVs, string assetRelativePathToSaveFolder, MeshKitCombineMeshSetup.AtlasMode atlasMode = MeshKitCombineMeshSetup.AtlasMode.OneAtlasForEachShader, int maxTexturesPerAtlas = 16, List<MeshRenderer> customMeshRendererList = null
			// Experimental
			, BakeFloatIntoTextureProperty[] bakeFloatsIntoTextures = null,
			BakeColorIntoTextureProperty[] bakeColorIntoTextures = null
		){

			// Create new propertyNames and textureFallbacks arrays based on the combineMeshRendererSetup
			int arrayLength = propertyList.Count;
			string[] propertyNames = new string[ arrayLength ];
			MeshKitCombineMeshSetup.MissingTextureFallback[] textureFallbacks = new MeshKitCombineMeshSetup.MissingTextureFallback[ arrayLength ];

			// Copy the data from the combineMeshRendererSetup to the new arrays
			for( int i = 0; i < arrayLength; i++ ){
				propertyNames[i] = propertyList[i].propertyName;
				textureFallbacks[i] = propertyList[i].missingTextureFallback;
			}

			// Run the method again the normal way
			return CombineMeshRenderers( selectedGameObject, maximumAtlasSize, propertyNames, textureFallbacks, bakeColorIntoMainTex, rebuildLightmapUVs, assetRelativePathToSaveFolder, atlasMode, maxTexturesPerAtlas, customMeshRendererList, bakeFloatsIntoTextures, bakeColorIntoTextures );
		}

		// -------------
		//	MAIN METHOD
		// -------------

		public static bool CombineMeshRenderers( 

			GameObject selectedGameObject, 														// Which GameObject are we combining from.
			MeshKitCombineMeshSetup.MaxAtlasSize maximumAtlasSize, 								// The Maximum Atlas Size to use
			string[] propertyNames, 															// The shader property names to use
			MeshKitCombineMeshSetup.MissingTextureFallback[] textureFallbacks, 					// The matching texture fallbacks to use (for shader property names)
			bool bakeColorIntoMainTex, 															// Should we bake the _Color property into the _MainTex?
			bool rebuildLightmapUVs, 															// Should we create a new UV2 channel for the meshes after they are combined?
			string assetRelativePathToSaveFolder, 												// The asset-relative filepath to save these combined assets
			MeshKitCombineMeshSetup.AtlasMode atlasMode = MeshKitCombineMeshSetup.AtlasMode.OneAtlasForEachShader, 		// How should we combine these objects?
			int maxTexturesPerAtlas = 16,														// The maximum number of textures allowed per atlas (helps to preserve quality)
			List<MeshRenderer> customMeshRendererList = null 									// <OPTIONAL> Allow a user to specify the exact MeshRenderers. Otherwise it is automatic.

			// Experimental Pixel Processing
			, BakeFloatIntoTextureProperty[] bakeFloatsIntoTextures = null,
			BakeColorIntoTextureProperty[] bakeColorIntoTextures = null
		){

			// ------------------------------------------------------------
			//	FIX CUSTOM PIXEL PROCESSING ARRAYS
			// ------------------------------------------------------------

			// Make sure our pixel processing arrays are not null
			if( bakeFloatsIntoTextures == null ){ bakeFloatsIntoTextures = BakeFloatIntoTextureProperty.EmptyArray(); }
			if( bakeColorIntoTextures == null ){ bakeColorIntoTextures = BakeColorIntoTextureProperty.EmptyArray(); }

			// ------------------------------------------------------------
			//	HANDLE SAVE FILEPATHS
			// ------------------------------------------------------------

			// Setup a path to save new assets ( add a forward slash to the supplied paths ) - we use this with the AssetDatabase API
			string saveAssetDirectory = assetRelativePathToSaveFolder+"/";

			// Dynamically setup the full file path using our handy converter method - we use this with the File API		
			string saveAssetDirectoryForFileAPI = MeshAssets.EditorConvertAssetRelativeFolderPathToFullFolderPath( assetRelativePathToSaveFolder ) + "/";

			// If there is a problem with the saveAssetDirectoryForFileAPI path, end early.
			if( saveAssetDirectoryForFileAPI == string.Empty ){
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", 
					"The path to save assets does not appear to be valid:\n\n" + saveAssetDirectoryForFileAPI, "OK"
				);
				return false;
			}

			// ------------------------------------------------------------
			//	INITIAL GAMEOBJECT CHECKS
			// ------------------------------------------------------------

			// End early if we didnt select a GameObject
			if ( selectedGameObject == null){ 
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", 
					"No GameObject was selected to combine.", "OK"
				);
				return false;
			}

			// The Selected GameObject already has a MeshRenderer on it!
			if ( selectedGameObject.GetComponent<MeshRenderer>() != null ){ 
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", 
					"The selected GameObject already has a MeshRenderer component.\n\nIn order to properly combine objects, make sure the parent object does not have a MeshRenderer already attached.", "OK"
				);
				return false;
			}

			// ------------------------------------------------------------
			//	CHECK OUR SUPPLIED PROPERTY NAMES AND TYPES
			// ------------------------------------------------------------

			// Make sure the first property name exists and is "_MainTex"
			if( propertyNames.Length > 0 && propertyNames[0] != "_MainTex" ){
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", 
					"The first propertyNames entry must be '_MainTex' in order to successfully combine textures.", "OK"
				);
				return false;
			}

			// Make sure the property names and types array lengths match
			if( propertyNames.Length != textureFallbacks.Length ){
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", 
					"The propertyNames and textureFallbacks array lengths must match.", "OK"
				);
				return false;
			}

			// ------------------------------------------------------------
			//	PREPARE THE CORE ARRAYS
			// ------------------------------------------------------------

			// Cache the transform of the selected GameObject
			Transform selectedTransform = selectedGameObject.transform;

			// Setup an array of all the MeshRenderers we're going to scan.
			MeshRenderer[] allMRs;

			// If the user has supplied a custom MeshRenderer list with at least 1 item, use that ...
			if( customMeshRendererList != null && customMeshRendererList.Count > 0 ){

				allMRs = customMeshRendererList.ToArray();

			} else {

				allMRs = selectedGameObject.GetComponentsInChildren<MeshRenderer>( false );	// <- false = includeInactive
			}

			// Filter out invalid renderers by creating a list and only adding ones that have an active renderer
			List<MeshRenderer> onlyActiveRenderersList = new List<MeshRenderer>();
			foreach( MeshRenderer mr in allMRs ){
				if( mr.enabled == true ){ onlyActiveRenderersList.Add( mr ); }
			}
			// Then, replace the original array
			allMRs = onlyActiveRenderersList.ToArray();

			// Make another matching array and to store the MeshFilters of the same GameObjects. This should be always be synced with the MeshRenderers.
			MeshFilter[] allMFs = new MeshFilter[allMRs.Length];

			// Track the unique meshes we are combining (this is primarily used for initial UV checks)
			List<Mesh> uniqueMeshes = new List<Mesh>();

			// Track the unique Materials being used
			List<Material> uniqueMaterials = new List<Material>();

			// Track the unique Shaders being used
			List<Shader> uniqueShaders = new List<Shader>();


			// ------------------------------------------------------------
			//	SCAN MESH RENDERERS FOR SUBMESHES AND OTHER ISSUES
			// ------------------------------------------------------------

			// Create a helper variable to detect if any submeshes were found in the script
			bool missingMeshfilterFound = false;				// <- Check if no meshfilter is found on object
			bool subMeshesFound = false;						// <- Check if meshes have submeshes
			bool noSharedMaterialsFound = false;				// <- Check if any SMR doesn't have a shared material
			bool weirdTextureFormatsFound = false;				// <- Check if any of the textures on the materials have formats that are not Texture2D.
			bool customTilingOrScaleFound = false;				// <- Check to see if any of the materials are using tiling or custom scales
			bool overrideCustomTilingOrScaleIssues = false;		// <- If the user tells us to override these issues, we'll do it.

			// Texture Importer Setup list
			List<TextureImporterSetup> textureImporterSetups = new List<TextureImporterSetup>();

			// Loop through the MeshRenderers and run some initial checks
			for( int i = 0; i < allMRs.Length; i++ ){

				// Cache the current MeshRenderer
				MeshRenderer mr = allMRs[i];

				// Check if there was an issue found on the current MeshRenderer
				bool issueFoundWithMeshRenderer = false;

				// Cache the MeshFilter of the same GameObject
				allMFs[i] = mr.gameObject.GetComponent<MeshFilter>();

				// Make sure this MeshRenderer also has a MeshFilter attached on the same GameObject
				MeshFilter mf = mr.gameObject.GetComponent<MeshFilter>();
				if( mf == null ){
					missingMeshfilterFound = true;
					Debug.LogWarning("The GameObject named: " + mr.gameObject.name + "' has a MeshRenderer but is missing a Meshfilter component.", mr.gameObject );
				}

				// If the MF has a subMesh, that will cause issues and we make notes of which ones have issues.
				if( mf != null && mf.sharedMesh != null && mf.sharedMesh.subMeshCount > 1 ){
					subMeshesFound = true;
					Debug.LogWarning("The MeshFilter named: " + mf.name + "' uses a mesh with submeshes which cannot be combined.", mf );
				}
					
				// If we passed the sharedMesh checks, and we haven't already added this mesh to our uniqueMeshes list, do it now
				if( mf != null && mf.sharedMesh != null && mf.sharedMesh.subMeshCount == 1 &&
					uniqueMeshes.Contains( mf.sharedMesh ) == false
				){	
					uniqueMeshes.Add( mf.sharedMesh );
				}

				// If the MR doesn't have a shared material, we won't be able to combine the textures
				if( mr.sharedMaterial == null ){
					noSharedMaterialsFound = true;
					issueFoundWithMeshRenderer = true;
					Debug.LogWarning("The MeshRenderer named: " + mr.name + "' has no sharedMaterial.", mr );
				}

				// This can happen if the main texture being used is baked into an asset
				if( mr.sharedMaterial.mainTexture != null && mr.sharedMaterial.mainTexture.GetType() != typeof(Texture2D) ){
					weirdTextureFormatsFound = true;
					issueFoundWithMeshRenderer = true;
					Debug.LogWarning("The MeshRenderer named: " + mr.name + "' is using main texture that is NOT a Texture2D. It is a: " + mr.sharedMaterial.mainTexture.GetType(), mr );
				}

				// WE CURRENTLY DONT SUPPORT TEXTURE TILING / OFFSETS - ASK USER TO CONFIRM
				// In the future we can look into making this work
				if( mr.sharedMaterial.GetTextureScale("_MainTex") != Vector2.one || mr.sharedMaterial.GetTextureOffset("_MainTex") != Vector2.zero ){
					customTilingOrScaleFound = true;
					issueFoundWithMeshRenderer = true;
					
					// Always show console warnings so the user can track down the problematic objects later
					Debug.LogWarning("MeshKit detected a material named '" + mr.sharedMaterial.name + "' on the gameObject named '" + mr.gameObject.name + " that is using a material with custom texture tiling or offset. MeshKit does not currently support material tiling. Ensure your materials have tiling set to (1,1) and offset to (0,0) if you want to combine them effectively.", mr.gameObject );
				
					// If the user hasn't told us to ignore these issues ...
					if( overrideCustomTilingOrScaleIssues == false ){

						// Ask the user if we should continue?
						if( EditorUtility.DisplayDialog(	
								"Combine MeshRenderer", 
								"MeshKit detected a material named '" + mr.sharedMaterial.name + "' on the gameObject named '" + mr.gameObject.name + "' that is set to use tiling ( UVs that are outside the 0-1 range ).\n\nFor best results, materials should have tiling set to (1,1) and offset to (0,0) in the inspector.\n\nWould you like to continue trying to combine all objects with this issue anyway?", "Continue", "Cancel"
							) == true
						){
							
							// User told us to combine it anyway. Ignore ...
							customTilingOrScaleFound = false;
							issueFoundWithMeshRenderer = false;

							// Always add these types of objects ...
							overrideCustomTilingOrScaleIssues = true;

						} else {

							// Remove progress bar
							EditorUtility.ClearProgressBar();

							// Cancel the combine
							return false;
						}

					} else {

						// User told us to combine it anyway. Ignore ...
						customTilingOrScaleFound = false;
						issueFoundWithMeshRenderer = false;

					}
				}


				// TRACK UNIQUE SHADERS AND MATERIALS
				// If no issues were found with this MeshRenderer, add the shader to our unique list
				if( issueFoundWithMeshRenderer == false ){

					// Track Unique Shaders
					if( uniqueShaders.Contains( mr.sharedMaterial.shader ) == false ){
						uniqueShaders.Add( mr.sharedMaterial.shader );
					}

					// Track unique materials
					if( uniqueMaterials.Contains( mr.sharedMaterial ) == false ){
						uniqueMaterials.Add( mr.sharedMaterial );
					}
				}
			}

			// ----------------------------------------------------------------
			//	DO OPTIMIZED TEXTURE IMPORTER SETUPS BASED ON UNIQUE MATERIALS
			// ----------------------------------------------------------------

			// Loop through the texture property names supplied by the user ...
			int propertyIndex = 0;
			foreach( string propertyName in propertyNames ){

				// Loop through each of the unique materials
				foreach( Material mat in uniqueMaterials ){

					// If this Mesh Renderer has a material which contains one of the property names we want to extract (eg "_MainTex")...
					if( mat.HasProperty( propertyName) ){

						// This can happen if the main texture being used is baked into an asset
						if( (Texture2D)mat.GetTexture( propertyName ) != null ){

							// Cache the 
							Texture2D propertyTex = (Texture2D)mat.GetTexture( propertyName );

							// If the property texture doesn't already exist in the textureImporterSetups, add it now ...
							if( DoesTextureImporterSetupContainTexture( ref textureImporterSetups, propertyTex ) == false ){

								// Cache the path of the mainTexture in the AssetDatabase
								string path = AssetDatabase.GetAssetPath ( propertyTex );
							
								// Cache the TextureImporter of the Main Texture
								TextureImporter imp = (TextureImporter) AssetImporter.GetAtPath (path);

								// Attempt to make it readable if it isn't, or if this is a normal map, make sure it is setup correctly
								if ( imp != null && ( 	imp.isReadable == false || 
														imp.textureCompression != TextureImporterCompression.Uncompressed || 
														textureFallbacks[propertyIndex] == MeshKitCombineMeshSetup.MissingTextureFallback.Normal
													)
								){
									// Add the original TextureImporter 
									textureImporterSetups.Add( 
										new TextureImporterSetup( propertyTex, imp, path )
									);

									// DEBUG
									//Debug.Log( "Added Property: " + propertyName + " - texture name: " + propertyTex.name );
								}	
							}
						}
					}
				}

				// Increment Property Index
				propertyIndex++;
			}


			// -----------------------
			//	SHOW ANY BASIC ERRORS
			// -----------------------

			// If we detected some MeshRenderers using meshes with submeshes...
			if( missingMeshfilterFound == true ){
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", 
					"Some MeshRenderers were found to have missing Meshfilter components on their GameObjects.\n\nMake sure all GameObjects have both a MeshFilter and a MeshRenderer.", "Abort"
				);
				return false;
			}	

			// If we detected some MeshFilters using meshes with submeshes...
			if( subMeshesFound == true ){
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", 
					"Some meshes found on the selected MeshFilters are using submeshes which will cause issues when combining.\n\nIn order to combine them, you can use the 'Seperate' tool to seperate them into individual meshes.", "Abort"
				);
				return false;
			}

			// If we found some MeshRenderers without materials...
			if( noSharedMaterialsFound == true ){
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", "Some of the selected MeshRenderers do not have a material assigned.", "Abort"
				);
				return false;
			}

			// If we found some main textures on materials that are in weird formats ( not Texture2D )...
			if( weirdTextureFormatsFound == true ){
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", "Some of the selected MeshRenderers are using a material that appear to be using Textures with strange formats.\n\nTo fix this, try making sure that all textures used in your materials are actual assets in your project (PNG, JPG, etc).", "Abort"
				);
				return false;
			}

			// If we found some materials that are using custom tiling or scale
			if( customTilingOrScaleFound == true ){
				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", "Some of the selected MeshRenderers are using a material with custom texture tiling or offset. MeshKit does not support combining materials with texture tiling at this time.\n\nYou could make this work by setting the tiling to (1,1) and offset to (0,0) in the material.", "Abort"
				);
				return false;
			}

			// -----------------------
			//	CHECK ARRAY INTEGRITY
			// -----------------------

			if( allMRs.Length == allMFs.Length ){

				// Loop through the MeshRenderers and make sure there are no misssing items
				for( int i = 0; i < allMRs.Length; i++ ){
					if( allMRs[i] == null  ){
						EditorUtility.DisplayDialog(	
							"Combine MeshRenderer", 
							"A missing MeshRenderer component was detected in the array. Make sure that all of your chosen objects have both a MeshFilter and a MeshRenderer component.", "Abort"
						);
						return false;
					}
				}

				// Loop through the MeshFilters and make sure there are no missing items
				for( int i = 0; i < allMFs.Length; i++ ){
					if( allMFs[i] == null  ){
						EditorUtility.DisplayDialog(	
							"Combine MeshRenderer", 
							"A missing MeshFilter component was detected in the array. Make sure that all of your chosen objects have both a MeshFilter and a MeshRenderer component.", "Abort"
						);
						return false;
					}
				}
			
			// Unequal number of MeshFilters and MeshRenderers
			} else {

				EditorUtility.DisplayDialog(	
					"Combine MeshRenderer", 
					"The number of MeshRenderers does not match the number of MeshFilters.\n\nMake sure that all of your chosen objects have both a MeshFilter and a MeshRenderer component.", "Abort"
				);
				return false;
			}


			// -----------------------
			//	CHECK UNIQUE MESH UVs
			// -----------------------

			// Always add these types of objects ...
			bool overrideMeshUVTilingIssues = false;

			// Loop through each unique mesh in the hash set ...
			foreach( Mesh m in uniqueMeshes ){

				// Show progress bar for each submesh
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar(
						"Combining Mesh Renderers", 
						"Checking UVs on mesh: '" + m.name + "'", 0
					);
				#endif

				// Loop through the UVs ...
				int numberOfUVs = m.uv.Length;
				for( int i = 0; i < numberOfUVs; i++ ){
					
					// If we detected a UV that is out of bounds ...
					if( m.uv[i].x < 0 || m.uv[i].x > 1 || m.uv[i].y < 0 || m.uv[i].y > 1 ){

						// Always show console warnings so the user can track down the problematic objects later
						Debug.LogWarning("MeshKit detected a mesh named '" + m.name + "' that is using UVs that are outside of the 0-1 range. This causes problems when trying to combine meshes with atlassing.", m );

						// If the user hasn't told us to ignore these issues, ask...
						if( overrideMeshUVTilingIssues == false ){

							// Ask the user if we should continue?
							if( EditorUtility.DisplayDialog(	
									"Combine MeshRenderer", 
									"MeshKit detected a mesh named '" + m.name + "' that is using UVs that are outside the 0-1 range.\n\nThis is usually a trick modellers use to achieve a tiling effect. However, this will cause texture problems when combining this object on an atlas.\n\nWould you like to continue trying to combine all objects with this issue anyway?", "Continue", "Cancel"
								) == true
							){
								// Break out of checking this mesh and move to the next one.
								overrideMeshUVTilingIssues = true;
								break;

							} else {

								// Remove progress bar
								EditorUtility.ClearProgressBar();

								// Cancel the combine
								return false;
							}

						// Automatically skip	
						} else {

							// Break out of checking this mesh and move to the next one.
							overrideMeshUVTilingIssues = true;
							break;
						}
					}
				}
			}

			// Remove progress bar
			EditorUtility.ClearProgressBar();

			// ----------------------------------------
			//	MAKE SURE TEXTURES ARE SETUP CORRECTLY
			// ----------------------------------------

			// Cache the number of texture importer setups we have
			int textureImporterSetupsCount = textureImporterSetups.Count;

			// Loop the texture importer setups and prepare each one
			if( textureImporterSetupsCount > 0 ){

				// Helper - check if any settings were invalid
				bool wereAnyTextureImporterSettingsInvalid = false;

				// Prepare Each of the textures ...
				for( int i = 0; i < textureImporterSetupsCount; i++ ){

					// Show progress bar for each submesh
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"Combining Mesh Renderers", 
							"Preparing textures for combining ( " + i.ToString() + " / " + textureImporterSetups.Count.ToString() + " )", 
							(float)i / (float)textureImporterSetups.Count
						);
					#endif

					// Prepare the texture importer on each one
					textureImporterSetups[i].PrepareSettings();

					// Check if any of the texture settings were invalid ...
					if( textureImporterSetups[i].ValidateSettings() == false ){
						
						// Record we found a problem and break the loop
						wereAnyTextureImporterSettingsInvalid = true;
						Debug.LogWarning( "MESHKIT: Combine MeshRenderer cancelled. Texture Format was invalid on the Texture2D named: " + textureImporterSetups[i].texture2D, textureImporterSetups[i].texture2D );
						break;
					}
				}

				// Handle any problems by reverting the textures back to their original settings and showing the user a message...
				if( wereAnyTextureImporterSettingsInvalid == true ){
					
					// Restore the orginal texture settings
					for( int i = 0; i < textureImporterSetups.Count; i++ ){

						// Show progress bar for each submesh
						#if SHOW_PROGRESS_BARS
							EditorUtility.DisplayProgressBar(
								"Combining Mesh Renderers",
								"Cancelling. Restoring textures to original settings ( " + i.ToString() + " / " + textureImporterSetups.Count.ToString() + " )", 
								(float)i / (float)textureImporterSetups.Count
							);
						#endif

						// Restore settings for each entry
						textureImporterSetups[i].ResetOriginalSettings();

					}

					// Remove progress bar
					EditorUtility.ClearProgressBar();

					// Show Message
					EditorUtility.DisplayDialog(	
						"Combine MeshRenderer", "The operation was cancelled because a Texture format was found to be invalid. To fix this, try making sure the texture is using an uncompressed format such as 'RGBA32' and try again.", "OK"
					);
					return false;
				}
			}

			// ------------------------------------------------------------
			//	HANDLE MAX ATLAS SIZE
			// ------------------------------------------------------------

			// Setup a integer to help us create the packed rects later
			int maxAtlasSize = 1024;

			// Override the value based on the enum
			if( maximumAtlasSize == MeshKitCombineMeshSetup.MaxAtlasSize._1024 ){
				maxAtlasSize = 1024;

			} else if( maximumAtlasSize == MeshKitCombineMeshSetup.MaxAtlasSize._2048 ){
				maxAtlasSize = 2048;

			} else if( maximumAtlasSize == MeshKitCombineMeshSetup.MaxAtlasSize._4096 ){
				maxAtlasSize = 4096;

			} else if( maximumAtlasSize == MeshKitCombineMeshSetup.MaxAtlasSize._8192 ){
				maxAtlasSize = 8192;

			}

			// ---------------------------------------------------------------------
			//	SETUP LOCAL MESHRENDERER AND MESHFILTER ARRAYS FOR EACH SHADER PASS
			// ---------------------------------------------------------------------

			// Create a new Shader Pass Data list to store our shader pass setups
			List<ShaderPassData> shaderPassDataList = new List<ShaderPassData>(); 
			int shaderPasses = 0;

			// ----------------------------------------------------------------------------------------------------------
			//	COMBINE MODE: ONE ATLAS FOR EACH SHADER [Default mode]
			//	This mode ensures that every shader is using at least one unique atlas.
			//	It will also split extra atlases based on the maxTexturesPerAtlas option.
			// ----------------------------------------------------------------------------------------------------------

			if( atlasMode == MeshKitCombineMeshSetup.AtlasMode.OneAtlasForEachShader ){

				// Helpers
				int numberOfMainTexturesAddedToThisPass = 0;

				// Loop through all of the unique shaders we've detected ...
				for( int sp = 0; sp < uniqueShaders.Count; sp++ ){

					// ----------------
					//	SETUP NEW PASS
					// ----------------

					// Every time we start at a new unique shader, Create a ShaderPassData object using current shader from the unique shaders array
					ShaderPassData shaderPassData = new ShaderPassData( uniqueShaders[sp] );

					// As we do this, we're going to reset and track the number of textures added to this pass
					numberOfMainTexturesAddedToThisPass = 0;

					// ---------------------------------------------------------
					//	ADD MESH RENDERERS THAT MATCH THE CURRENT UNIQUE SHADER
					// ---------------------------------------------------------
						
					// Loop through ALL of the MeshRenderers
					int allMRsLength = allMRs.Length;
					for( int i = 0; i < allMRsLength; i++ ){

						// If the current MeshRenderer is using a shader that matches our current unique shader, add it!
						if( allMRs[i].sharedMaterial.shader == uniqueShaders[sp] ){

							// Add the MeshRenderer and MeshFilter from this index
							shaderPassData.passMRs.Add( allMRs[i] );
							shaderPassData.passMFs.Add( allMFs[i] );
							
							// If the mainTextures list doesn't already contain the main texture from the current MeshRenderer's material ...
							if( allMRs[i].sharedMaterial.mainTexture != null &&
								shaderPassData.mainTextures.Contains( allMRs[i].sharedMaterial.mainTexture ) == false  ){
								
								// Add the main texture reference to the list
								shaderPassData.mainTextures.Add( allMRs[i].sharedMaterial.mainTexture );

								// Increment the number of textures added to this pass
								numberOfMainTexturesAddedToThisPass++;
							}
							
							// ----------------------------------------------------------------------
							//	CREATE NEW PASS IF WE'VE EXCEEDED THE MAX NUMBER OF TEXTURES ALLOWED
							// ----------------------------------------------------------------------

							// If we've reached the max number of textures to add to this pass,
							// make a new shader pass!
							if( numberOfMainTexturesAddedToThisPass >= maxTexturesPerAtlas ){

								// Add the current shader pass data to the list
								shaderPassDataList.Add( shaderPassData );

								// Replace the current shader pass data so we can continue adding new items
								shaderPassData = new ShaderPassData( uniqueShaders[sp] );

								// Reset the number of textures add to the pass and continue
								numberOfMainTexturesAddedToThisPass = 0;

							}
						}
					}

					// ------------------------------------------------------
					//	FINISH THIS PASS BEFORE MOVING TO NEXT UNIQUE SHADER
					// ------------------------------------------------------

					// Add this shader pass if it has objects included
					if( shaderPassData.passMRs.Count > 0 ){
						shaderPassDataList.Add( shaderPassData );
					}

				}

				// Setup Shader Passes count
				shaderPasses = shaderPassDataList.Count;

			}

			// ----------------------------------------------------------------------------------------------------------
			//	COMBINE MODE: FORCE STANDARD SHADER
			//	This mode ignores what shaders the objects were using and forces everything to use the Standard shader.
			//	It will only split into multiple atlases based on the maxTexturesPerAtlas option.
			// ----------------------------------------------------------------------------------------------------------
			
			else if( atlasMode == MeshKitCombineMeshSetup.AtlasMode.ForceStandardShader ){

				// ------------------
				//	SETUP FIRST PASS
				// ------------------

				// To begin, we start the first shader pass data assuming we're using the Standard shader for everything.
				ShaderPassData shaderPassData = new ShaderPassData( Shader.Find( "Standard" ) );

				// As we do this, we're going to reset and track the number of textures added to this pass
				int numberOfMainTexturesAddedToThisPass = 0;

				// ---------------------------------------------------------
				//	ADD MESH RENDERERS THAT MATCH THE CURRENT UNIQUE SHADER
				// ---------------------------------------------------------
					
				// Loop through ALL of the MeshRenderers
				int allMRsLength = allMRs.Length;
				for( int i = 0; i < allMRsLength; i++ ){

					// Add the MeshRenderer and MeshFilter from this index
					shaderPassData.passMRs.Add( allMRs[i] );
					shaderPassData.passMFs.Add( allMFs[i] );
					
					// If the mainTextures list doesn't already contain the main texture from the current MeshRenderer's material ...
					if( allMRs[i].sharedMaterial.mainTexture != null &&
						shaderPassData.mainTextures.Contains( allMRs[i].sharedMaterial.mainTexture ) == false  ){
						
						// Add the main texture reference to the list
						shaderPassData.mainTextures.Add( allMRs[i].sharedMaterial.mainTexture );

						// Increment the number of textures added to this pass
						numberOfMainTexturesAddedToThisPass++;
					}
					
					// ----------------------------------------------------------------------
					//	CREATE NEW PASS IF WE'VE EXCEEDED THE MAX NUMBER OF TEXTURES ALLOWED
					// ----------------------------------------------------------------------

					// If we've reached the max number of textures to add to this pass,
					// make a new shader pass!
					if( numberOfMainTexturesAddedToThisPass >= maxTexturesPerAtlas ){

						// Add the current shader pass data to the list
						shaderPassDataList.Add( shaderPassData );

						// Replace the current shader pass data so we can continue adding new items
						shaderPassData = new ShaderPassData( Shader.Find( "Standard" ) );

						// Reset the number of textures add to the pass and continue
						numberOfMainTexturesAddedToThisPass = 0;

					}
				}

				// ------------------------------------------------------
				//	ADD THIS PASS IF THERE ARE ANY OBJECTS LEFT ...
				// ------------------------------------------------------

				// Add this shader pass if it has objects included
				if( shaderPassData.passMRs.Count > 0 ){
					shaderPassDataList.Add( shaderPassData );
				}

				// Setup Shader Passes count
				shaderPasses = shaderPassDataList.Count;

			}

			// ---------------------------------------------------------------
			//	PREPARE THE MESHKIT COMBINE MESH SETUP COMPONENT FOR NEW DATA
			// ---------------------------------------------------------------

			// Make an array of GameObjects that match our shader passes.
			// We'll add all the new combined GameObjects we create in after each pass
			GameObject[] newCombinedGameObjects = new GameObject[shaderPasses];


			// -----------------------------------
			//	HELPER VARIABLES TO USE EACH PASS
			// -----------------------------------

			// Setup local arrays for each shader pass
			MeshRenderer[] MRs = new MeshRenderer[0];
			MeshFilter[] MFs = new MeshFilter[0];
		 
			// Prepare the rest of the helper values
			int vertCount = 0;
			int normCount = 0;
			int tanCount = 0;
			int triCount = 0;
			int uvCount = 0;
			int color32Count = 0;
	 
			Vector3[] verts;
			Vector3[] norms;
			Vector4[] tans;
			int[] tris;
			Vector2[] uvs;
			Color32[] colors32s;

			int vertOffset = 0;
			int normOffset = 0;
			int tanOffset = 0;
			int triOffset = 0;
			int uvOffset = 0;
			int color32Offset = 0;

			int meshOffset = 0;
			int mfCount = 0;

			bool normalsDontMatchVertexCount = false;
			bool tangentsDontMatchVertexCount = false;
			Color32 whiteColor32 = new Color32( 255, 255, 255, 255 );
			Mesh tempMeshFix = null;

			string progressBarTitle = string.Empty;
			string currentTextureAtlasFileNameToSave = string.Empty;


			// ------------------
			//	SHADER PASSES
			// ------------------

			// Loop through the shader passes
			for( int shaderPass = 0; shaderPass < shaderPasses; shaderPass++ ){

				// ------------------
				//	RESET VARIABLES
				// ------------------
			 
				// Prepare the rest of the helper values
				vertCount = 0;
				normCount = 0;
				tanCount = 0;
				triCount = 0;
				uvCount = 0;
				color32Count = 0;
		 
				verts = new Vector3[0];
				norms = new Vector3[0];
				tans = new Vector4[0];
				tris = new int[0];
				uvs = new Vector2[0];
				colors32s = new Color32[0];	// <- new

				vertOffset = 0;
				normOffset = 0;
				tanOffset = 0;
				triOffset = 0;
				uvOffset = 0;
				color32Offset = 0;

				meshOffset = 0;
				mfCount = 0;

				normalsDontMatchVertexCount = false;
				tangentsDontMatchVertexCount = false;
				tempMeshFix = null;

				// Update the progress bar title so we only do it once per pass 
				progressBarTitle =  "Combine Mesh Renderers - Atlas " + (shaderPass+1).ToString() + " of " + shaderPasses.ToString();

				// Reset texture atlas name to save
				currentTextureAtlasFileNameToSave = string.Empty;

				// -------------------------
				//	UPDATE ARRAYS
				// -------------------------

				// Cache the MeshFilters and MeshRenderers from the ShaderPassData list matching this pass
				MFs = shaderPassDataList[ shaderPass ].passMFs.ToArray();
				MRs = shaderPassDataList[ shaderPass ].passMRs.ToArray();

				// ---------------------------------------
				//	MAKE SURE HELPER TEXTURES ARE WORKING
				// ---------------------------------------

				// Make sure we have created out helper textures (this gives us backwards-compatibility with unity 2017.4)
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar( progressBarTitle, "Preparing Helper Textures...", 1f );
				#endif
				RecreateHelperTextures();

				
				// -----------------------------
				//	SCAN MESH RENDERERS
				// -----------------------------

				// Show progress bar for each submesh
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar( progressBarTitle, "Processing Meshes...", 1f );
				#endif

				// Loop through the MeshFilters
				foreach (MeshFilter mf in MFs){

					// Tally up total lengths of all vertices, normals, etc.
					vertCount += mf.sharedMesh.vertexCount;
					//normCount += mf.sharedMesh.normals.Length;
					//tanCount += mf.sharedMesh.tangents.Length;

						// NEW: Always make sure the vertex counts match
						// we'll create the normals / tangents on the fly if something is wrong.
						normCount += mf.sharedMesh.vertexCount;
						tanCount += mf.sharedMesh.vertexCount;
						color32Count += mf.sharedMesh.vertexCount;

					triCount += mf.sharedMesh.triangles.Length;
					uvCount += mf.sharedMesh.uv.Length;
				

					// Increment the number of meshfilters we've processed
					mfCount++;
				}

				// -------------------------------------------
				//	PREPARE THE TEXTURE PROPERTY SETUP ARRAY
				// -------------------------------------------

				// Setup a TexturePropertySetup array based on shader texture property names passed to the method
				TexturePropertySetup[] texturePropertySetup = new TexturePropertySetup[ propertyNames.Length ];
				
				// Copy the property name and fallback to each of the texturePropertySetup entries. Then, create a textures array the same length as the number of MRs.
				for( int i = 0; i < texturePropertySetup.Length; i++ ){ 
					texturePropertySetup[i] = new TexturePropertySetup();
					texturePropertySetup[i].propertyName = propertyNames[i];						// <- Cache the property name
					texturePropertySetup[i].textureFallback = textureFallbacks[i];					// <- Cache the default texture fallback
					texturePropertySetup[i].materials = new Material[mfCount];						// <- Create materials array of the same length in each property setup
					texturePropertySetup[i].textures = new Texture2D[mfCount];						// <- Create textures array of the same length in each property setup
					texturePropertySetup[i].texturesOriginallyMissing = new bool[mfCount];			// <- Create a bool array to track which textures were originally missing
					texturePropertySetup[i].colorsToBakeIntoMainTex = new Color[mfCount];			// <- What color should we bake into the main texture atlas?
					texturePropertySetup[i].tempResizedTexturesToDelete = new List<Texture2D>();	// <- Create the temp textures List
					
				}

				// -----------------------------
				//	SCAN MESHES
				// -----------------------------

				verts = new Vector3[vertCount];
				norms = new Vector3[normCount];				// <- This should be the same as vertex count?
				tans = new Vector4[tanCount];				// <- This should be the same as vertex count?
				tris = new int[triCount];
				uvs = new Vector2[uvCount];

				colors32s = new Color32[color32Count];		// <- TEST LATER: I think there needs to be the same number of colors as verts?

				// Vertex Offset Helpers
				Vector3 newVertex = Vector3.zero;
				Transform mfTransform = null;
				Transform originalParent = null;
				Vector3 originalLocalPosition = Vector3.zero;
				Quaternion originalLocalRotation = Quaternion.identity;
				Vector3 originalLocalScale = Vector3.one;

				// Loop through each of the Mesh Filters
				foreach (MeshFilter mf in MFs){

					// Cache the MeshFilter's transform
					mfTransform = mf.transform;

					// ------------------
					//	HANDLE MESH DATA
					// ------------------

					// Loop through each of the mesh triangles and copy them
					foreach (int i in mf.sharedMesh.triangles){
						tris[triOffset++] = i + vertOffset;
					}

					// Loop through each of the mesh vertices and copy them
					foreach (Vector3 v in mf.sharedMesh.vertices){
						
						// Old way used for SkinnedMeshRenderers
						//verts[vertOffset++] = v;
					
						// NOTE: SPECIAL VERTEX HANDLING FOR MESH RENDERERS
						// When we're combining the vertices, we need to account for the difference in position, rotation and scale.
						// We temporarily move the MeshFilters under the main parent to do this more accurately.

						// Cache this MeshFilter's original local position, rotation, scale and parent
						originalParent = mfTransform.parent;
						originalLocalPosition = mfTransform.localPosition;
						originalLocalRotation = mfTransform.localRotation;
						originalLocalScale = mfTransform.localScale;

						// Move it so its directly underneith the selected GameObject
						mfTransform.parent = selectedGameObject.transform;

						// Rotate the vertex to bake it's rotation
						newVertex = RotatePointAroundPivot( v, mfTransform.localRotation, Vector3.zero );

						// Apply CURRENT scaling to the vertex (as this can change once its been moved)
						newVertex.x *= mfTransform.localScale.x;
						newVertex.y *= mfTransform.localScale.y;
						newVertex.z *= mfTransform.localScale.z;

						// Set the vertex and also apply it's CURRENT position offset
						verts[vertOffset++] = newVertex + mfTransform.localPosition;

						// Move the MeshFilter's transform back to its original position and restore its settings
						mfTransform.parent = originalParent;
						mfTransform.localPosition = originalLocalPosition;
						mfTransform.localRotation = originalLocalRotation;
						mfTransform.localScale = originalLocalScale;

					}

					// --------------------------------------------------------
					//	IF NORMALS AND / OR TANGENTS ARE MISSING, REBUILD THEM
					// --------------------------------------------------------

					// Check if the vertices / tangents are missing not matching the vertex count (indicates a problem)
					normalsDontMatchVertexCount = ( mf.sharedMesh.normals.Length != mf.sharedMesh.vertexCount );
					tangentsDontMatchVertexCount = ( mf.sharedMesh.tangents.Length != mf.sharedMesh.vertexCount );

					// If we are missing tangents or normals, rebuild a temporary mesh to help us
					if( normalsDontMatchVertexCount || tangentsDontMatchVertexCount ){

						// Debug
						if( MeshKitGUI.verbose == true ){
							Debug.Log( " Rebuilding normals [ " + normalsDontMatchVertexCount + " ] / tangents [ " + tangentsDontMatchVertexCount + " ] on temp mesh in place of: " + mf.sharedMesh.name, mf.gameObject );
						}

						// Create a temporary mesh to fix the missing normals / tangents
						tempMeshFix = MeshKit.RebuildMesh( mf.sharedMesh, false, false, false, false, false, false, false, false, false, false, normalsDontMatchVertexCount, tangentsDontMatchVertexCount, -1 );

						// Add the normals from the temp mesh
						foreach (Vector3 n in tempMeshFix.normals){
							norms[normOffset++] = n;
						}

						// Add the tangents from the temp mesh
						foreach (Vector4 t in tempMeshFix.tangents){
							tans[tanOffset++] = t;
						}

					// Otherwise, copy the normals / tangents normally
					} else {

						// Loop through each of the mesh normals and copy them
						foreach (Vector3 n in mf.sharedMesh.normals){
							norms[normOffset++] = n;
						}

						// Loop through each of the mesh tangents and copy them
						foreach (Vector4 t in mf.sharedMesh.tangents){
							tans[tanOffset++] = t;
						}

					}

					
					// --------------------------------------------------------
					//	COPY COLORS IF THEY EXIST, OTHERWISE USE WHTIE
					// --------------------------------------------------------

					// If the colors dont match the vertex count, copy white directly
					if( mf.sharedMesh.colors32.Length != mf.sharedMesh.vertexCount ){

						for( int c = 0; c < mf.sharedMesh.vertexCount; c++ ){
							colors32s[color32Offset++] = whiteColor32;
						}

					// If the colors match, copy them directly	
					} else {

						// Loop through each of the mesh colors and copy them
						foreach( Color32 c32 in mf.sharedMesh.colors32 ){
							colors32s[color32Offset++] = c32;
						}

					}

					// ->

					// Loop through each of the mesh UVs and copy them
					foreach (Vector2 uv in mf.sharedMesh.uv){
						uvs[uvOffset++] = uv;
					}

					// We need to cache the MeshRenderer from this MeshFilter to help the next section
					MeshRenderer mr = mf.gameObject.GetComponent<MeshRenderer>();

					// -------------------
					//	HANDLE PROPERTIES
					// -------------------

					// Copy all the textures into the entries we've setup via the texturePropertySetup array
					for( int i = 0; i < texturePropertySetup.Length; i++ ){

						// Cache the Shared Materials on each property so we have easy access to them
						texturePropertySetup[i].materials[meshOffset] = mr.sharedMaterial;

						// _MainTex Entry - check for colors
						if( i == 0 ){

							// Cache the color to bake into the MainTex if enabled
							if( bakeColorIntoMainTex == true && mr.sharedMaterial.HasProperty( "_Color" ) ){
						
								texturePropertySetup[0].colorsToBakeIntoMainTex[meshOffset] = (Color) mr.sharedMaterial.GetColor( "_Color" );
								//Debug.Log("Custom _Color detected on MR: " + texturePropertySetup[0].colorsToBakeIntoMainTex[meshOffset] );

							// Otherwise, default is Color.white
							} else {

								texturePropertySetup[0].colorsToBakeIntoMainTex[meshOffset] = Color.white;
							}
						}

						// If the shared material has a property that matches our texture property setup ...
						if( mr.sharedMaterial.HasProperty( texturePropertySetup[i].propertyName ) ){

							// Setup the textures array
							texturePropertySetup[i].textures[meshOffset] = (Texture2D) mr.sharedMaterial.GetTexture( texturePropertySetup[i].propertyName );

						}

						// If our texture it still null ( it could be nothing was set in the shader as well ), use the fallbacks to set a new texture. 
						if( texturePropertySetup[i].textures[meshOffset] == null ){

							// Fallback to transparent black
							if( texturePropertySetup[i].textureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.TransparentBlack ){
								texturePropertySetup[i].textures[meshOffset] = texture2DBlack;

							// Fallback to white
							} else if( texturePropertySetup[i].textureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.White ){
								texturePropertySetup[i].textures[meshOffset] = texture2DWhite;

							// Fallback to grey
							} else if( texturePropertySetup[i].textureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.Grey ){
								texturePropertySetup[i].textures[meshOffset] = texture2DGrey;

							// Fallback to a neutral normal
							} else if( texturePropertySetup[i].textureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.Normal ){
								texturePropertySetup[i].textures[meshOffset] = texture2DNormal;

							}

							// Track which textures were originally missing so we can fix / resize them later
							texturePropertySetup[i].texturesOriginallyMissing[meshOffset] = true;

						}
					}

					// Increment the number of mesh offsets and disable the original MeshRenderer
					meshOffset++;
					mr.enabled = false;

				}

				// ----------------------------
				//	MISSING MAIN TEXTURE FIXES
				// ----------------------------

				// Create a new list to track all the resized Texture2Ds we're going to make
				List<Texture2D>texture2DsCreatedOnDemandList = new List<Texture2D>();

				// NOTE: This section aims to improve missing MainTex entries. If a MainTex is missing but it has another property that isn't missing,
				// ( eg, having no main texture but a bump map ), then the fallback main texture will be resized using the same size as the bump map
				// in order to try and maintain its detail...

				// Loop through the _MainTex textures (entry 0) to see if any were originally missing ...
				for( int i = 0; i < texturePropertySetup[0].texturesOriginallyMissing.Length; i++ ){

					// Check if this entry in MainTexture was missing ...
					if( texturePropertySetup[0].texturesOriginallyMissing[i] == true ){

						// Debug
						//Debug.LogWarning( "Missing texture found in " + texturePropertySetup[0].propertyName + " at entry: " + i );

						// If there are more properties, we can get better results by matching the size of the missing maintexture to another property
						// that requires it. If we can find one, do it here ...
						for( int j = 0; j < texturePropertySetup.Length; j++ ){
							// Always skip the MainTexture
							if( j > 0 && texturePropertySetup[j].textures[i] != null ){

								// Show progress bar
								#if SHOW_PROGRESS_BARS
									EditorUtility.DisplayProgressBar( progressBarTitle, "Optimizing Missing Main Textures...", 1f );
								#endif

								// Debug
								//Debug.LogWarning( "Replacement texture found in " + texturePropertySetup[j].propertyName + " at entry: " + i );
								
								// -------------------------------------------------------------
								// CREATE NEW MAIN TEXTURE BASED ON THE SIZE OF THE REPLACEMENT
								// -------------------------------------------------------------

								// Create a new Texture2D that has the same size as the problem texture ( we need to do that to clone it )
								Texture2D newTexture2D = new Texture2D( texturePropertySetup[j].textures[i].width, texturePropertySetup[j].textures[i].height );

								// Cache the color we're going to use to fill the texture ( sampled from the original texture )
								Color ColorToFill = texturePropertySetup[0].textures[i].GetPixel(0,0);
								
								// Cache the total pixel array size
								int pixelArraysize = newTexture2D.width * newTexture2D.height;

								// Setup pixel array
								Color[] pixels = new Color[ pixelArraysize ];

								// Loop through the pixels and set it to the color to fill
								for( int c = 0; c < pixelArraysize; c++ ){ pixels[c] = ColorToFill; }

								// Then, apply it to all the pixels on the new texture
								newTexture2D.SetPixels( pixels );

								// Apply the changes
								newTexture2D.Apply();

								// -----------
								// REPLACE IT
								// -----------

								// Replace the texture
								texturePropertySetup[0].textures[i] = newTexture2D;

								// keep track of any temporary textures we created for each property so we can free it up later
								texturePropertySetup[0].tempResizedTexturesToDelete.Add( newTexture2D );

								// Also add it to the list so we can destroy it when we're done
								texture2DsCreatedOnDemandList.Add( newTexture2D );

								// break the loop once we find a replacement
								break;
							}
						}
					}
				}


				// -----------------
				//	TEXTURE PACKING
				// -----------------

				// Setup the texture atlas and packedRects
				Rect[] packedRects = new Rect[0];

				// Loop through the texturePropertySetup array
				for( int i = 0; i < texturePropertySetup.Length; i++ ){

					// Show progress bar for each texturePropertySetup property
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar( progressBarTitle, "Creating Texture2D assets for " + texturePropertySetup[i].propertyName + "... ( " + (i + 1).ToString() + " / " + texturePropertySetup.Length.ToString() + " )" ,  (float)(i+1) / (float)texturePropertySetup.Length );
					#endif

					// Create a new Texture2D and use the PackTextures method to both create the atlas and setup it's rects.
					texturePropertySetup[i].atlas = new Texture2D (1,1);

					// -------------------
					//	_MAINTEX PROPERTY
					// -------------------

					// If this is the first property entry (usually "_MainTex")...
					if( i == 0 ){ 

						// ---------------------------------------------------------------------
						//	CREATE THE FIRST ATLAS AND STORE THE PACKED RECTS FOR LATER
						// ---------------------------------------------------------------------

						// Setup a placeholder bool to catch any errors in packing textures
						bool packTexturesError = false;
					
						// Create the _MainTex atlas and save the packedRects into the variable we prepared earlier
						//packedRects = texturePropertySetup[i].atlas.PackTextures ( texturePropertySetup[i].textures, 0, maxAtlasSize );	// <- OLD WAY

						// NEW FIX for Unity 2022 - Custom AtlasPacker! It returns the atlas Texture and Rects array
						(texturePropertySetup[i].atlas, packedRects, packTexturesError) = SquareAtlasPacker.PackTextures( texturePropertySetup[i].textures, 0, maxAtlasSize );


						// If packing finished successfully, do other checks
						if( packTexturesError == false ){

							// Debug Packed Rects and Atlas Size
							// Debug.LogWarning( "maxAtlasSize: " + maxAtlasSize );
							// Debug.LogWarning("texturePropertySetup[i].atlas width: " + texturePropertySetup[i].atlas.width );
							// Debug.LogWarning("texturePropertySetup[i].atlas height: " + texturePropertySetup[i].atlas.height );


							// BUGFIX: If we select a larger max atlas size (eg 4096), it is possible for Unity to return the packedRects to be a
							// smaller size if it wasn't needed (eg 2048). We need to detect this change and update the maxAtlas Size here.
							if( maxAtlasSize > texturePropertySetup[i].atlas.width && texturePropertySetup[i].atlas.width > 16 ){
								maxAtlasSize = texturePropertySetup[i].atlas.width;
							//	Debug.LogWarning( "BUGFIX - updated maxAtlasSize: " + maxAtlasSize );
							}

							// Check for non-square atlases ( this could mess things up )
							// DEV NOTE: I saw this happen once in testing and the model still worked ok. Keep an eye on it in development.
							if( texturePropertySetup[i].atlas.width != texturePropertySetup[i].atlas.height ){
								if( MeshKitGUI.verbose == true ){
									Debug.LogWarning( "MeshKit detected that Unity returned a non-square atlas! This may cause issues...");
								}
							}


							// ---------------------------------------------------------------------
							//	APPLY COLOR BAKES
							// ---------------------------------------------------------------------

							// Apply Color Bakes
							if( bakeColorIntoMainTex == true ){
								BakeColorIntoMainTextureAtlas( ref texturePropertySetup[i].atlas, ref packedRects, ref texturePropertySetup );
							}


							// ---------------------------------------------------------------------
							//	APPLY CUSTOM PIXEL PROCESSING HERE
							// ---------------------------------------------------------------------

							if( bakeFloatsIntoTextures.Length > 0 ){
								CustomPixelProcessingIntoMainTextureAtlas(
									ref texturePropertySetup[i].atlas, ref packedRects, ref texturePropertySetup, ref bakeFloatsIntoTextures,
									ref bakeColorIntoTextures
								);
							}

						}

						// ---------------------------------------------------------------------
						//	IF PACKED RECTS COULDN'T BE CREATED, CANCEL THE PROCESS
						// ---------------------------------------------------------------------

						// NOTE: If we gave too many textures to pack, Unity sends back null according to the docs
						// (although null doesnt work here, so lets check for length == 0)
						// UPDATE: As we're using out custom packer utility, we directly set the packTexturesError bool, so check that first.
						if( packTexturesError == true || packedRects.Length == 0 ){

							// Restore the orginal texture settings
							if( textureImporterSetupsCount > 0 ){
								for( int j = 0; j < textureImporterSetups.Count; j++ ){

									// Show progress bar for each submesh
									#if SHOW_PROGRESS_BARS
										EditorUtility.DisplayProgressBar(
											"Combining Mesh Renderers",
											"Cancelling - Restoring textures to original settings ( " + i.ToString() + " / " + textureImporterSetups.Count.ToString() + " )", 
											(float)j / (float)textureImporterSetups.Count
										);
									#endif

									// Restore settings for each entry
									textureImporterSetups[j].ResetOriginalSettings();
								}
							}

							// Remove Progress Bar
							EditorUtility.ClearProgressBar();

							// Show Message
							EditorUtility.DisplayDialog(	
								"Combine MeshRenderer", "A PackedRects array couldn't be returned by Unity's 'PackTextures' method. You may be trying to fit too many textures into too small a space.\n\nYou can try to resolve this issue by making the max atlas size bigger or using less textures.", "OK"
							);
							Debug.LogWarning( "MESHKIT: Combine MeshRenderer operation cancelled because PackedRects array was not created by Unity (this is usually an indicator of the Max Atlas size being too small).");
							return false;
						}
					
					// -------------------
					//	OTHER PROPERTIES
					// -------------------

					// If this is not the first entry, we need to make sure all new texture sizes match the original ones in _MainTex
					} else {

						// Cache the number of _MainTex textures in entry 0 of the texturePropertySetup
						int numberOfMainTexInTexturePropertySetup = texturePropertySetup[0].textures.Length;

						// Loop through all of the _MainTex textures (entry 0)
						for( int j = 0; j < numberOfMainTexInTexturePropertySetup; j++ ){

							// If any texture's size in this property do not match the same MainTexture ...
							if( texturePropertySetup[i].textures[j].width != texturePropertySetup[0].textures[j].width ||
								texturePropertySetup[i].textures[j].height != texturePropertySetup[0].textures[j].height ||

								// Also do this if this is a normal map...
								texturePropertySetup[i].textureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.Normal
							){

								// ---------------------------------------------------------------------
								//	STEP 1) SETUP CORE HELPERS AND DUPLICATE ORIGINAL TEXTURE
								// ---------------------------------------------------------------------

								// Cache the original Texture
								var originalTexture = texturePropertySetup[i].textures[j];

								// Cache the current packed Rect Width and Height
								int currentPackedRectWidth = Mathf.RoundToInt( packedRects[j].width * maxAtlasSize );
								int currentPackedRectHeight = Mathf.RoundToInt( packedRects[j].height * maxAtlasSize );

								// DEBUG
								/*
								Debug.Log( texturePropertySetup[i].textures[j].name );
								Debug.Log("original width: " + currentPackedRectWidth );
								Debug.Log("original height: " + currentPackedRectHeight );
								Debug.Log("original texture name: " + texturePropertySetup[0].textures[j] );
								*/

								// Create a new Texture2D that has the same size as the problem texture ( we need to do that to clone it )
								Texture2D newTexture2D = new Texture2D( originalTexture.width, originalTexture.height );

								// Figure out if we need to apply the red normal fix
								bool redNormalMapFixNeeded = (

									// Current fallback texture is normal map
									texturePropertySetup[i].textureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.Normal && 

									// Does this texture require a normal map fix?
									DoesTextureRequireRedNormalMapFix( textureImporterSetups, originalTexture ) == true
								
								);

								// Show progress bar based on number of textures to fix
								#if SHOW_PROGRESS_BARS
									
									EditorUtility.DisplayProgressBar( 
										progressBarTitle, 
										"Copying Texture Data On " + texturePropertySetup[i].propertyName + " ( " + (j + 1).ToString() + " / " + numberOfMainTexInTexturePropertySetup.ToString() + " ). Please Wait..." ,
										(float)(j+1) / (float)numberOfMainTexInTexturePropertySetup
									);

								#endif

								// ---------------------------------------------------------------------
								//	STEP 2a) RED NORMAL MAP FIX IS REQUIRED
								// ---------------------------------------------------------------------

								//	NOTE: Because this can take a huge amount of time, we have setup these optimizations to drastically speed this up!
								if( redNormalMapFixNeeded == true  ){

									// Prepare the original texture pixels
									Color[] originalTexturePixels = new Color[0];
									
									// OPTIMIZATION: If we need to apply a normal map fix on this texture on this pass, grab it and use the processed pixels
									if( ProcessedTexture.ListContains( 
											ref texturePropertySetup[i].redNormalProcessedTextures,
											originalTexture, 
											currentPackedRectWidth,
											currentPackedRectHeight 
										) != null
									){ 

										// Cache the previously processed and scaled Texture2D
										Texture2D processedTexture = ProcessedTexture.ListContains( ref texturePropertySetup[i].redNormalProcessedTextures, originalTexture, currentPackedRectWidth, currentPackedRectHeight );
									
										// Rebuild the new Texture2D as pre-processed textures should already be scaled correctly
										newTexture2D = new Texture2D( currentPackedRectWidth, currentPackedRectHeight );
										newTexture2D.SetPixels( processedTexture.GetPixels() );

										// Apply the changes
										newTexture2D.Apply( false );

										// We no longer need to apply the normal map
										redNormalMapFixNeeded = false;
									
									// Otherwise, cache the original texture pixels as normal
									} else {

										originalTexturePixels = originalTexture.GetPixels();

									}


									// IF WE STILL NEED TO DO IT, FIX FOR NORMAL MAPS USING DXT5 TEXTURE FORMATS (red normal map fix)
									// This will be false if we've already cached the texture and re-used it
									if ( redNormalMapFixNeeded == true ){

										// Show progress bar based on number of textures to fix
										#if SHOW_PROGRESS_BARS
											
											EditorUtility.DisplayProgressBar( 
												progressBarTitle, 
												"Processing Pixels On " + texturePropertySetup[i].propertyName + " ( " + (j + 1).ToString() + " / " + numberOfMainTexInTexturePropertySetup.ToString() + " ). Please Wait..." ,
												(float)(j+1) / (float)numberOfMainTexInTexturePropertySetup
											);

										#endif


										// Helper variables
										Color pixel;
										Vector2 v2RG;
										
										//Debug.Log( "Attempting Red Normal Map Fix on: " + originalTexture.name );
										int originalTexturePixelsLength = originalTexturePixels.Length;
										for( int p = 0; p < originalTexturePixelsLength; p++ ){

											// Cache the original pixel
											pixel = originalTexturePixels[p];
											
											// Process the pixel by converting the range from ( 0 to 1 ) to ( -1 to +1 )
											pixel.r = pixel.a * 2 - 1;  										// Red becomes the alpha and converted
											pixel.g = pixel.g * 2 - 1; 											// Green is directly converted
											v2RG = new Vector2( pixel.r, pixel.g );								// Helper Vector2: x = red, y = green
											pixel.b = Mathf.Sqrt( 1 - Mathf.Clamp01(Vector2.Dot(v2RG, v2RG)));	// Recreate the blue value

											// Apply the Pixel, converting it back to the ( 0 to 1 ) range
											originalTexturePixels[p] = new Color ( 
												pixel.r * 0.5f + 0.5f, 
												pixel.g * 0.5f + 0.5f, 
												pixel.b * 0.5f + 0.5f
											); 
										}

										// Set the new pixels
										newTexture2D.SetPixels( originalTexturePixels );

										// Apply the changes
										newTexture2D.Apply( false );


										// ------------------------------------------------------------------------
										// MAKE A SCALED COPY OF THIS TEXTURE TO BE RE-USED IN THIS PASS IF NEEDED
										// ------------------------------------------------------------------------

										// Make another copy of the processed texture to store in our texturePropertySetup
										Texture2D copyOfProcessedT2D = new Texture2D( newTexture2D.width, newTexture2D.height );
										copyOfProcessedT2D.SetPixels( originalTexturePixels );
										copyOfProcessedT2D.Apply();

										// Scale it before it add it so that we won't have to rescale it back each time
										if( copyOfProcessedT2D.width != currentPackedRectWidth || copyOfProcessedT2D.height != currentPackedRectHeight ){
											TextureScale.Bilinear ( copyOfProcessedT2D, currentPackedRectWidth, currentPackedRectHeight );
										}

										// Track the texture we just processed so we can re-use it if needed
										texturePropertySetup[i].redNormalProcessedTextures.Add( new ProcessedTexture( originalTexture, copyOfProcessedT2D ) );
									
									}

								// -> START OF NEW	




									/*
									// Try to detect Metallic property fix
									if( texturePropertySetup[i].propertyName == "_MetallicGlossMap" &&			// <- This is the _MetallicGlossMap property
										texturePropertySetup[i].texturesOriginallyMissing[j] == true &&			// <- The original texture was missing
										//texturePropertySetup[i].textures[j]

										// _GlossMapScale
									){

										Debug.LogWarning("We found a shader property '_MetallicGlossMap' with its texture originally missing at index: " + j );

									}
									*/

									




								// -> END OF NEW

								// ---------------------------------------------------------------------
								//	STEP 2b) NO NORMAL MAP FIXES REQUIRED
								// ---------------------------------------------------------------------

								// Otherwise, directly duplicate the texture
								} else {

									// Set the new pixels directly from the original texture
									newTexture2D.SetPixels( originalTexture.GetPixels() );

									// Apply the changes
									newTexture2D.Apply( false );

								}

								// ---------------------------------------------------------------------
								// STEP 3) RESIZE IT TO MATCH THE ORIGINAL _MAINTEX ATLAS VERSION
								// ---------------------------------------------------------------------
								
								// -> Start of Threaded version
								/*
									// Rescale using custom method. Unity's Texture2D.Resize version doesn't work so we need to it like this.
									// BUG NOTE: Sometimes there is a multi-threading timing issue and this messes up. Use the non-threaded version instead.
									ThreadedTextureScale.Bilinear ( newTexture2D, currentPackedRectWidth, currentPackedRectHeight );
									
									// Debug.Log( texturePropertySetup[0].textures[j].name + " => w: " + newTexture2D.width + " h: " + newTexture2D.height  );

									// Apply the changes
									newTexture2D.Apply();
								*/
								// -> End of Threaded version
							

								// Rescale using custom method on the main thread. Unity's Texture2D.Resize version doesn't work here!
								// NOTE: This applies the texture inside the function
								if( newTexture2D.width != currentPackedRectWidth || newTexture2D.height != currentPackedRectHeight ){
									TextureScale.Bilinear ( newTexture2D, currentPackedRectWidth, currentPackedRectHeight );
								}


								// ---------------------------------------------------------------------
								// STEP 4) APPLY IT TO THE ARRAY AND TRACK IT SO WE CAN DELETE IT LATER
								// ---------------------------------------------------------------------

								// Replace it in the array
								texturePropertySetup[i].textures[j] = newTexture2D;

								// TEST THIS!
								// Debug.Log( texturePropertySetup[i].textures[j].name + ": Rescaled Texture is now => w: " + texturePropertySetup[i].textures[j].width + " h: " + texturePropertySetup[i].textures[j].height + " should be the same as original" );

								// keep track of any temporary textures we created for each property so we can free it up later
								texturePropertySetup[i].tempResizedTexturesToDelete.Add( newTexture2D );

								// Also add it to the list so we can destroy it when we're done
								texture2DsCreatedOnDemandList.Add( newTexture2D );


							}
						}
						
						// ---------------------------------------------------------------------
						// CUSTOM ATLAS CREATION
						// ---------------------------------------------------------------------

						// Create a new texture atlas using our resized textures and the original packedRects. We define the atlas size using the one Unity created in the _MainTex atlas.
						// NOTE: This will create the texture atlas but it allows us to handle if something goes wrong in easy if statement.
						if( CreateNewAtlasFromPackedRectsAndResizedTextures ( 
								ref texturePropertySetup[i].atlas, ref texturePropertySetup[i].textures, ref packedRects, 
								ref bakeFloatsIntoTextures, ref bakeColorIntoTextures,										// <- NEW: Custom Pixel Processing Data
								maxAtlasSize, maxAtlasSize,																	// This uses the updated maxAtlasSize for width and height
								MissingTextureFallbackToColor( texturePropertySetup[i].textureFallback ),					// Background color
								texturePropertySetup[i]																		// The current Texture Property Setup
							) == false // -> If there is an error, do the following ...
						){

							// Restore the orginal texture settings
							if( textureImporterSetupsCount > 0 ){
								for( int j = 0; j < textureImporterSetups.Count; j++ ){

									// Show progress bar for each submesh
									#if SHOW_PROGRESS_BARS
										EditorUtility.DisplayProgressBar(
											"Combining Mesh Renderers",
											"Cancelling - Restoring textures to original settings ( " + i.ToString() + " / " + textureImporterSetups.Count.ToString() + " )", 
											(float)j / (float)textureImporterSetups.Count
										);
									#endif

									// Restore settings for each entry
									textureImporterSetups[j].ResetOriginalSettings();
								}
							}

							// Remove Progress Bar
							EditorUtility.ClearProgressBar();

							// Show Message
							EditorUtility.DisplayDialog(	
								"Combine MeshRenderer", "An error occured while trying to pack textures for property: " + texturePropertySetup[i].propertyName, "OK"
							);
							Debug.LogError( "MESHKIT: Combine MeshRenderer operation cancelled because an error occured while trying to pack textures for property: " + texturePropertySetup[i].propertyName);
							
							return false;

						}


						// ---------------------------------------------------------------------
						// FREE UP TEXTURE MEMORY AFTER EACH PROPERTY
						// ---------------------------------------------------------------------

						// Free up memory for each property by deleting the temporary textures we created
						for( int t = 0; t < texturePropertySetup[i].tempResizedTexturesToDelete.Count; t++ ){
							if( texturePropertySetup[i].tempResizedTexturesToDelete[t] != null ){
								DestroyImmediate( texturePropertySetup[i].tempResizedTexturesToDelete[t] );
							}
						}

					}

					// ---------------------------------------------------------------------
					//	SAVE THE TEXTURE ATLASES
					// ---------------------------------------------------------------------

					// Setup the current texture atlas' file name
					currentTextureAtlasFileNameToSave = selectedGameObject.name + " s" + shaderPass.ToString() + " " + texturePropertySetup[i].propertyName + ".png";
					currentTextureAtlasFileNameToSave.MakeFileSystemSafe(); // Fix dodgy mesh names.

					// Save the Texture using the File API, and then load it back in using the Asset Database's API.
					File.WriteAllBytes ( saveAssetDirectoryForFileAPI + currentTextureAtlasFileNameToSave, texturePropertySetup[i].atlas.EncodeToPNG());
					//AssetDatabase.Refresh ();
					AssetDatabase.Refresh ( ImportAssetOptions.ForceSynchronousImport );
					texturePropertySetup[i].atlas = (Texture2D) AssetDatabase.LoadAssetAtPath ( saveAssetDirectory + currentTextureAtlasFileNameToSave, typeof(Texture2D));

					// ---------------------------------------------------------------------
					//	SETUP THE ASSET'S TEXTURE IMPORTER
					// ---------------------------------------------------------------------

					// Help the AssetDatabase setup the texture properly
					TextureImporter latestAtlasTI = (TextureImporter) AssetImporter.GetAtPath ( saveAssetDirectory + currentTextureAtlasFileNameToSave );
					if( latestAtlasTI != null ){

						// FIX NORMAL MAPS: If the texture we just saved is a normal map, we should set that up in the AssetDatabase
						if ( texturePropertySetup[i].textureFallback == MeshKitCombineMeshSetup.MissingTextureFallback.Normal ){
							latestAtlasTI.textureType = TextureImporterType.NormalMap;
						}
						
						// Setup the Max Atlas Size
						if( maximumAtlasSize == MeshKitCombineMeshSetup.MaxAtlasSize._8192 ){
							latestAtlasTI.maxTextureSize = 8192;

						} else if( maximumAtlasSize == MeshKitCombineMeshSetup.MaxAtlasSize._4096 ){
							latestAtlasTI.maxTextureSize = 4096;

						} else if( maximumAtlasSize == MeshKitCombineMeshSetup.MaxAtlasSize._2048 ){
							latestAtlasTI.maxTextureSize = 2048;

						} else {

							latestAtlasTI.maxTextureSize = 1024;	// <- Default to a 1024 atlas if something went wrong here
						}

						// Set dirty and re-import it
						EditorUtility.SetDirty( latestAtlasTI );
						latestAtlasTI.SaveAndReimport();
					}

					// ---------------------------------------------------------------------
					//	CLEAN UP PROCESSED TEXTURES FOR THIS PASS
					// ---------------------------------------------------------------------

					// Clean up the pixel processed lists after each pass to free up ram
					ProcessedTexture.DisposeList( ref texturePropertySetup[i].redNormalProcessedTextures );

				}

				// -------------------
				//	NEW MESH UV SETUP
				// -------------------

				// Show progress bar for each submesh
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar( progressBarTitle, "Processing UVs...", 1f );
				#endif

				// Helpers
				uvOffset = 0;
				meshOffset = 0;
				Vector2 uvClamped = Vector2.zero;
				bool tilingUVsDetected = false;

				// Loop through each of the MeshFilters
				foreach ( MeshFilter mf in MFs ){

					// Reset the tiling UVs on each MeshFilter
					tilingUVsDetected = false;

					// Loop through each of their UVs
					foreach ( Vector2 uv in mf.sharedMesh.uv ){
						
						// OUT OF RANGE UV FIXES
						// If any of the UVs are outside of the 0-1 range, this approach tries to fix it...
						if( attemptToFixOutOfRangeUVs == true ){

							// Cache the original UV			   
							uvClamped = uv;

							// DEBUG: DETECT TILING (out of range) UVS
							if( tilingUVsDetected == false && ( uvClamped.x < 0 || uvClamped.x > 1 || uvClamped.y < 0 || uvClamped.y > 1 ) == true ){
								tilingUVsDetected = true;
								Debug.LogWarning( "MESHKIT: Detected tiling UVs on: " + mf.gameObject.GetComponent<MeshRenderer>().sharedMaterial.name, mf.gameObject );
							}
						   
							// Keep reducing / adding by 1 to get to a 0-1 range.
							while (uvClamped.x > 1){ uvClamped.x = uvClamped.x - 1; }
							while (uvClamped.x < 0){ uvClamped.x = uvClamped.x + 1; }
							while (uvClamped.y > 1){ uvClamped.y = uvClamped.y - 1; }
							while (uvClamped.y < 0){ uvClamped.y = uvClamped.y + 1; }			// 3.0.1 bugfix
						   
							// Setup the new UVs using the info from the packedRects
							uvs[uvOffset].x = Mathf.Lerp (packedRects[meshOffset].xMin, packedRects[meshOffset].xMax, uvClamped.x);            
							uvs[uvOffset].y = Mathf.Lerp (packedRects[meshOffset].yMin, packedRects[meshOffset].yMax, uvClamped.y);            
							uvOffset ++;
						
						} else {

							// ORIGINAL APPROACH
							// Setup the new UVs using the info from the packedRects
							uvs[uvOffset].x = Mathf.Lerp (packedRects[meshOffset].xMin, packedRects[meshOffset].xMax, uv.x);
							uvs[uvOffset].y = Mathf.Lerp (packedRects[meshOffset].yMin, packedRects[meshOffset].yMax, uv.y);
							uvOffset ++;

						}		

					}

					// Increment the meshes
					meshOffset ++;
				}

				// --------------------
				//	CREATE NEW MATERIAL
				// --------------------

				// Show progress bar
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar( progressBarTitle, "Building new Material...", 1f );
				#endif
		 
				// Create New Material using the Standard shader as the default
				Material mat = new Material (Shader.Find("Standard"));

				// If we're using one shader for each atlas, use the shader pass data list
				if( atlasMode == MeshKitCombineMeshSetup.AtlasMode.OneAtlasForEachShader ){
				
					mat.shader = shaderPassDataList[ shaderPass ].shader;
				
				// If we're forcing the standard shader, do that.
				} else if( atlasMode == MeshKitCombineMeshSetup.AtlasMode.ForceStandardShader ){

					mat.shader = Shader.Find("Standard");

				}

				// Apply the properties
				for( int i = 0; i < texturePropertySetup.Length; i++ ){

					// Setup Textures for each property
					mat.SetTexture( texturePropertySetup[i].propertyName, texturePropertySetup[i].atlas );

					// Special Setups - Normal Maps
					if( texturePropertySetup[i].propertyName == "_BumpMap" ){ 
						mat.EnableKeyword("_NORMALMAP");												// <- Turn On Normal Map
					}

					// Special Setups - Gloss Maps
					if( texturePropertySetup[i].propertyName == "_MetallicGlossMap" ){ 
						mat.EnableKeyword("_METALLICGLOSSMAP");											// <- Turn On Metallic Gloss Map
					}

					// Special Setups - Emission
					if( texturePropertySetup[i].propertyName == "_EmissionMap" ){ 

						mat.SetColor( "_EmissionColor", Color.white );									// <- Set Emission Color First
						mat.EnableKeyword("_EMISSION");													// <- Turn On Emission
						mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;	// <- This needs to be set for Emission to work!
					}	
				}

				// Create the material in the AssetDatabase
				AssetDatabase.CreateAsset(mat, saveAssetDirectory + selectedGameObject.name + " s" + shaderPass.ToString() + " Material.mat");


				// --------------------
				//	CREATE NEW MESH
				// --------------------

				// Show progress bar
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar( progressBarTitle, "Building New Mesh In Pass " + shaderPass.ToString() +"...", 1f );
				#endif
		 
		 		// Create a new Combined GameObject and place it in the same hierarchy as the selected GameObject
				GameObject newCombinedGo = new GameObject( selectedGameObject.name + " - Combined s" + shaderPass.ToString() );
				newCombinedGo.transform.parent = selectedTransform.parent;
				newCombinedGo.transform.localPosition = selectedTransform.localPosition;
				newCombinedGo.transform.localRotation = selectedTransform.localRotation;
				newCombinedGo.transform.localScale = selectedTransform.localScale;

				// Create New Mesh
				Mesh newMesh = new Mesh();

				// Determine if we need to use a 16 or 32 bit mesh based on vertex count and triangle count.
				if ( verts.Length <= MAX_VERTS_16BIT && tris.Length <= MAX_VERTS_16BIT ){
					newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
				} else {
					newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				}

				// Setup the new mesh
				newMesh.name = selectedGameObject.name + " s" + shaderPass.ToString();
				newMesh.vertices = verts;
				newMesh.normals = norms;
				newMesh.tangents = tans;
				newMesh.uv = uvs;
				newMesh.triangles = tris;

				// NEW
				newMesh.colors32 = colors32s;


				// Automatically rebuild Normals / Tangents ( it seems to mess up after the combine is done )

				// Show progress bar for each submesh
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar( progressBarTitle, "Creating new normals and tangents for Mesh. Please Wait...", 1f );
				#endif

				// Create a temporary mesh to fix the missing normals / tangents
				newMesh = MeshKit.RebuildMesh( newMesh, false, false, false, false, false, false, false, false, false, false, true, true, -1 );

				// Rebuild Lightmap UVs (UV2)
				if( rebuildLightmapUVs ){

					// Show progress bar for each submesh
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar( progressBarTitle, "Creating UV2 For Mesh. Please Wait...", 1f );
					#endif
					
					// Create UV2 Set
					Unwrapping.GenerateSecondaryUVSet( newMesh ); 

				}
		   
				// Create New MeshFilter and apply the new mesh
				MeshFilter newMF = newCombinedGo.AddComponent<MeshFilter>();
				newMF.sharedMesh = newMesh;

				// Create new MeshRenderer and apply the new material
				MeshRenderer newMR = newCombinedGo.AddComponent<MeshRenderer>();
				newMR.material = mat;

		
				// Show progress bar for each submesh
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar( progressBarTitle, "Creating Assets. Please Wait...", 1f );
				#endif

				// Create the mesh in the AssetDatabase
				AssetDatabase.CreateAsset(newMesh, saveAssetDirectory + selectedGameObject.name + " s" + shaderPass.ToString() + " Mesh.asset");


				// Keep track of this combined GameObject
				newCombinedGameObjects[shaderPass] = newCombinedGo;

				/*
				// ------------------------------------------------------
				//	SETUP MESHKIT COMBINE SKINNEDMESH COMPONENT
				// ------------------------------------------------------
				
				// Cache the MeshKitCombineMeshSetup component if it exists
				var mkcms = selectedGameObject.GetComponent<MeshKitCombineMeshSetup>();
				if( mkcms != null ){

					// NOTE: We need to divide this in shader passes!
					Debug.Log("Fix shader pass data here");

					// Cache the original Skinned Mesh Renderers
					mkcms.originalMRs = MRs;
					mkcms.originalMFs = MFs;

					// Cache the new SMR
					mkcms.newMR = newMR;

					// Set generated to true and update the generated Combine Mode
					mkcms.generated = true;
					mkcms.generatedCombineMode = atlasMode;
				}
				*/

				// ------------------------------------------------------
				//	CLEAN UP TEXTURE MEMORY ON EACH PASS
				// ------------------------------------------------------

				// Show progress bar
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar( progressBarTitle, "Cleaning Memory From Pass...", 1f );
				#endif

				// Destroy all the textures we created previously on demand to clean up memory
				for( int i = 0; i < texture2DsCreatedOnDemandList.Count; i++ ){
					if( texture2DsCreatedOnDemandList[i] != null ){ DestroyImmediate( texture2DsCreatedOnDemandList[i] ); }
				}

				// Clear Progress Bar
				EditorUtility.ClearProgressBar();

			}

			// ------------------------------------------------------
			//	RESTORE ORIGINAL TEXTURE SETTINGS 
			// ------------------------------------------------------

			// Restore the orginal texture settings
			if( textureImporterSetupsCount > 0 ){
				for( int i = 0; i < textureImporterSetups.Count; i++ ){

					// Show progress bar for each submesh
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"Combining Mesh Renderers",
							"Restoring textures to original settings ( " + i.ToString() + " / " + textureImporterSetups.Count.ToString() + " )", 
							(float)i / (float)textureImporterSetups.Count
						);
					#endif

					// Restore settings for each entry
					textureImporterSetups[i].ResetOriginalSettings();
				}
			}




			// ------------------------------------------------------
			//	SETUP MESHKIT COMBINE SKINNEDMESH COMPONENT
			// ------------------------------------------------------

			// Show progress bar for each submesh
			#if SHOW_PROGRESS_BARS
				EditorUtility.DisplayProgressBar( "Combining Mesh Renderers", "Caching data to MeshKit component...", 1f );
			#endif
			
			// Cache the MeshKitCombineMeshSetup component if it exists
			var mkcms = selectedGameObject.GetComponent<MeshKitCombineMeshSetup>();
			if( mkcms != null ){

				// Cache the original Skinned Mesh Renderers we used
				mkcms.originalMRs = allMRs;
				mkcms.originalMFs = allMFs;

				// Save an array of all the new Combined GameObjects we just made
				mkcms.newCombinedGameObjects = newCombinedGameObjects;

				// Set generated to true and update the generated atlas Mode
				mkcms.generated = true;
				mkcms.generatedAtlasMode = atlasMode;
			}

			// -------------------
			//	CLEAN MEMORY
			// -------------------

			// Show progress bar for each submesh
			#if SHOW_PROGRESS_BARS
				EditorUtility.DisplayProgressBar( "Combining Mesh Renderers", "Cleaning Memory...", 1f );
			#endif

			/*
			// Destroy all the textures we created previously on demand to clean up memory
			for( int i = 0; i < texture2DsCreatedOnDemandList.Count; i++ ){
				if( texture2DsCreatedOnDemandList[i] != null ){ DestroyImmediate( texture2DsCreatedOnDemandList[i] ); }
			}
			*/

			// Clear Progress Bar
			EditorUtility.ClearProgressBar();

			// Return true if the operation completed successfully
			return true;

		}

/// -> 		[HELPER] ROTATE POINT AROUND PIVOT

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	ROTATE POINT AROUND PIVOT
		//	Helper method for rotating a position around a pivot point
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		public static Vector3 RotatePointAroundPivot( Vector3 position, Quaternion rotation, Vector3 pivot = default(Vector3)) {
		    return rotation * (position - pivot) + pivot;
		}

/// -> 		[EDITOR ONLY] PROPERTY REQUIRES PIXEL PROCESSING

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] PROPERTY REQUIRES PIXEL PROCESSING
		//	This method looks at the property name in a material and determines whether we should also be factoring in another property value as well.
		//	For example, if we are using the _MetallicGlossMap property, we should also look at the _Metallic Value as well as _Smoothness and bake it in to the value.
		//	The method simply checks to see if we need to do this and returns true or false.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Check to see if this property requires pixel processing ...
		static bool PropertyRequiresPixelProcessing( string propertyName, Material mat, ref BakeFloatIntoTextureProperty[] bakeFloatsIntoTextures, ref BakeColorIntoTextureProperty[] bakeColorIntoTextures ){
			
			// Check the built-in pixel processing
		//	if( propertyName == "_MetallicGlossMap" && mat.HasProperty( "_Glossiness" ) ){ return true; }	// <- old version.
			if( propertyName == "_MetallicGlossMap" && mat.HasProperty( "_GlossMapScale" ) && mat.HasProperty( "_Glossiness" ) && mat.HasProperty( "_Metallic" ) ){ return true; }
			if( propertyName == "_OcclusionMap" && mat.HasProperty( "_OcclusionStrength" ) ){ return true; }
			if( propertyName == "_EmissionMap" && mat.IsKeywordEnabled("_EMISSION") ){ return true; }

			// Check the custom floats
			foreach( BakeFloatIntoTextureProperty bfittp in bakeFloatsIntoTextures ){
				// Debug.LogWarning("Checking: " + propertyName + " == " + bfittp.texturePropertyName );
				if( bfittp.texturePropertyName == propertyName && mat.HasProperty( bfittp.floatPropertyName ) ){ 
					// Debug.LogWarning("TRUE ON: " + propertyName);
					return true;
				}
			}

			// Check the custom colors
			foreach( BakeColorIntoTextureProperty bcittp in bakeColorIntoTextures ){
				// Debug.LogWarning("Checking: " + propertyName + " == " + bcittp.texturePropertyName );
				if( bcittp.texturePropertyName == propertyName && mat.HasProperty( bcittp.colorPropertyName ) ){ 
					// Debug.LogWarning("TRUE ON: " + propertyName);
					return true;
				}
			}

			// Otherwise return false
			return false;
		}

/// -> 		[EDITOR ONLY] PROCESS PIXEL USING PROPERTY NAME AND MATERIAL

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] PROCESS PIXELS USING PROPERTY NAME AND MATERIAL
		//	This method looks at the property name in a material and determines whether we should also be factoring in another property value as well.
		//	For example, if we are using the _MetallicGlossMap property, we should also look at the _Metallic Value as well as _Smoothness and bake it in to the value.
		//	This method actually handles the processing of an array of pixels in a texture. We should call 'PropertyRequiresPixelProcessing() == true ' first.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static void ProcessPixelsUsingPropertyNameAndMaterial( 
			ref Color[] pixels,
			string propertyName,
			Material material,
			bool textureWasOriginallyNull,
			ref BakeFloatIntoTextureProperty[] bakeFloatsIntoTextures,
			ref BakeColorIntoTextureProperty[] bakeColorIntoTextures
		){

			// ---------------------------------------
			//	CACHE WHICH PROPERTY WE'RE PROCESSING
			// ---------------------------------------

			// Cache the property we're seting up first to speed up the loop
			//bool processingMetallicGlossMap = ( propertyName == "_MetallicGlossMap" && material.HasProperty( "_Glossiness" ) );
			bool processingMetallicGlossMap = ( propertyName == "_MetallicGlossMap" && material.HasProperty( "_GlossMapScale" ) && material.HasProperty( "_Glossiness" ) && material.HasProperty( "_Metallic" ) );
			bool processingOcclusionMap = ( propertyName == "_OcclusionMap" && material.HasProperty( "_OcclusionStrength" ) );
			bool processingEmissionMap = ( propertyName == "_EmissionMap" && material.IsKeywordEnabled("_EMISSION") );

			//if(processingEmissionMap){ Debug.Log("Found _Emission keyword on material: " + material.name + " - propertyname: " + propertyName + " - texture was originally null: " + textureWasOriginallyNull ); }

			// ---------------------
			//	CACHE HELPER VALUES
			// ---------------------

			// Setup cached versions of the values to speed things up
			float cachedMetallicValue = processingMetallicGlossMap ? material.GetFloat( "_Metallic" ) : 0.5f;
			float cachedGlossinessValue = processingMetallicGlossMap ? material.GetFloat( "_Glossiness" ) : 0.01f;
			float cachedGlossMapScaleValue = processingMetallicGlossMap ? material.GetFloat( "_GlossMapScale" ) : 0.01f;


			// _SmoothnessTextureChannel

			// Cache the metallic gloss map value
			float cachedOcclusionStrengthValue = processingOcclusionMap ? material.GetFloat( "_OcclusionStrength" ) : 1f;

			// Cache the emission Color
			Color cachedEmissionColor = material.HasProperty( "_EmissionColor" ) ? material.GetColor( "_EmissionColor" ) : Color.black;




			// -----------------
			//	PROCESS PIXELS
			// -----------------

			// Loop through the pixels
			int pixelsLength = pixels.Length;

			// -----------------------------
			//	METALLIC GLOSS MAP PROPERTY
			// -----------------------------

			if( processingMetallicGlossMap == true ){

				// SHADER NOTES:
				// _Metallic =  The float value from the _Metallic slider (used if there isn't a _MetallicGlossMap texture)
				// _Glossiness = the float value from the _Glossiness (Smoothness) Slider (used if there isn't a _MetallicGlossMap texture)
				// _MetallicGlossMap = The texture (if one is being used). 
				// _GlossMapScale = if we're using a _MetallicGlossMap texture,  
				// _SmoothnessTextureChannel = 0 if the alpha is in the _MetallicGlossMap.a / _Glossiness, or 1 = mainTexture.a 

				// LIMITATIONS:
				// At the moment, we are ignoring the _SmoothnessTextureChannel when combining Metallic / Smoothness. we are always assuming
				// that the shader is using the _MetallicGlossMap's alpha as its source rather than the _MainTex's alpha channel.

				// If there is no _MetallicGlossMap, 
				if( textureWasOriginallyNull == true ){
					
					//Debug.Log("[A] _MetallicGlossMap was originally NULL on material: " + material.name + " - propertyname: " + propertyName + " - texture was originally null: " + textureWasOriginallyNull );

					// Loop through the pixels
					for( int i = 0; i < pixelsLength; i++ ){ 

						// This algorithm works - finally! lol
						pixels[i].r = cachedMetallicValue;
						pixels[i].g = 0f;								// <- This is ignored.
						pixels[i].b = 0f;								// <- This is ignored
						pixels[i].a = cachedGlossinessValue;
					}

				// If the Metallic GlossMap has a texture, we'll use that directly.	
				} else {

					// NOTE: This version may need more testing ...

					//Debug.Log("[B] _MetallicGlossMap was originally present on material: " + material.name + " - propertyname: " + propertyName + " - texture was originally null: " + textureWasOriginallyNull );

					// Loop through the pixels
					for( int i = 0; i < pixelsLength; i++ ){ 

						// If we're using a Metallic Gloss Map, all we need to do is bake in the Gloss Map Scale value so it is pre-multiplied 
						// and should work when slider is set to 1 on the finished atlas.
						pixels[i].a = pixels[i].a * cachedGlossMapScaleValue;

					}
				}
			}

			// -----------------------------
			//	OCCLUSION MAP PROPERTY
			// -----------------------------

			else if( processingOcclusionMap == true ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					// Occlusion map using OcclusionStrength to determine the strength of the effect. To mimic its effect on the texture, we lerp from white.
					pixels[i] = Color.Lerp( Color.white, pixels[i], cachedOcclusionStrengthValue );
				}
			}

			// -----------------------------
			//	EMISSION MAP PROPERTY
			// -----------------------------

			else if( processingEmissionMap == true ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					// If the Emission texture originally didn't exist ...
					if( textureWasOriginallyNull ){

						// Override the RGB values with the emission color
						pixels[i].r = cachedEmissionColor.r;
						pixels[i].g = cachedEmissionColor.g;
						pixels[i].b = cachedEmissionColor.b;

					// if it did exist ...
					} else {

						// Multiply the RGB values with the emission color
						pixels[i].r *= cachedEmissionColor.r;
						pixels[i].g *= cachedEmissionColor.g;
						pixels[i].b *= cachedEmissionColor.b;

					}
				}
			}

			// -------------------------
			//	CUSTOM PIXEL PROCESSING
			// -------------------------

			// Let the BakeFloatIntoTextureProperty class handle this
			BakeFloatIntoTextureProperty.ProcessPixels( ref pixels, propertyName, material, textureWasOriginallyNull, ref bakeFloatsIntoTextures );


			// Let the BakeColorIntoTextureProperty class handle this
			BakeColorIntoTextureProperty.ProcessPixels( ref pixels, propertyName, material, textureWasOriginallyNull, ref bakeColorIntoTextures );

		}

/// -> 		[EDITOR ONLY] CREATE NEW ATLAS FROM PACKED RECTS AND RESIZED TEXTURES

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] CREATE NEW ATLAS FROM PACKED RECTS AND RESIZED TEXTURES
		//	This method creates an atlas at the correct size and places previously resized textures into it using the packedRects array.
		//	Returns true if successful.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static bool CreateNewAtlasFromPackedRectsAndResizedTextures( ref Texture2D atlas, ref Texture2D[] textures, ref Rect[] rects, ref BakeFloatIntoTextureProperty[] bakeFloatsIntoTextures, ref BakeColorIntoTextureProperty[] bakeColorIntoTextures, float originalAtlasWidth, float originalAtlasHeight, Color backgroundColor, TexturePropertySetup texturePropertySetup ){
			
			// Make sure the texture and packedRects arrays are the same length
			if( textures.Length == rects.Length ){

				// Make this atlas the same size as the first atlas we created with PackTextures.
				atlas = new Texture2D( (int)originalAtlasWidth, (int)originalAtlasHeight );

				// DEBUG
				// Debug.LogWarning( "Original Atlas Width / Height =  w: " + originalAtlasWidth  + " h: " + originalAtlasHeight );

				// ----------------------------
				// HANDLE THE BACKGROUND
				// ----------------------------

				// Create the fill colours for the background
				Color32[] fillPixels = new Color32[ atlas.width * atlas.height ];
				int fillPixelsLength = fillPixels.Length;
				for( int p = 0; p < fillPixelsLength; p++ ){ fillPixels[p] = backgroundColor; }

				// Set the background color on the atlas
				atlas.SetPixels32( 0, 0, atlas.width, atlas.height, fillPixels );

				// DEBUG AFTER SETTING PIXELS
				// Debug.LogWarning( "New Atlas Width / Height (after setting background pixels - should match original ) =  w: " + atlas.width  + " h: " + atlas.height );

				// ----------------------------
				// HANDLE EACH OF THE TEXTURES
				// ----------------------------

				// Helper Values
				int i = 0;																// <- Setup an index to increment through the textures
				//Color propertyColorModifier = Color.white;							// <- Setup a color to multiply with ( based on property )
				Color[] modifiedTexturePixels = new Color[0];							// <- Setup a re-usable color array to modify any textures we need

				// Loop through each of the textures in our array
				foreach( Texture2D t2d in textures ){

					// Debug size of each texture
					// Debug.Log( "NEW TEXTURE " + i.ToString() + " - textures[i].width: " + textures[i].width + " textures[i].height: " + textures[i].height );

					// ------------------------------
					// USING PROPERTY COLOR MODIFIER
					// ------------------------------

					// Cache the property color modifier for this texture by looking at its respective property name and material
					//propertyColorModifier = PropertyNameToColorModifier( texturePropertySetup.propertyName, texturePropertySetup.materials[i] );

					// Before we apply the pixels, we need to modify them with the property color modifier if needed
					//if( propertyColorModifier != Color.white ){

					// Check to see if this property requires extra pixel processing ...
					if( PropertyRequiresPixelProcessing( texturePropertySetup.propertyName, texturePropertySetup.materials[i], ref bakeFloatsIntoTextures, ref bakeColorIntoTextures ) == true ){

						// Cache the pixels
						modifiedTexturePixels = t2d.GetPixels();

						// Process the pixels using data from the property name and material being used
						ProcessPixelsUsingPropertyNameAndMaterial( 
							ref modifiedTexturePixels,
							texturePropertySetup.propertyName,
							texturePropertySetup.materials[i], 
							texturePropertySetup.texturesOriginallyMissing[i],
							ref bakeFloatsIntoTextures,
							ref bakeColorIntoTextures
						);

						// DEBUG
						/*
						Debug.Log( "A) rects[i].x = " + rects[i].x );
						Debug.Log( "A) rects[i].y = " + rects[i].y );
						Debug.Log( "A) rects[i].width = " + rects[i].width );
						Debug.Log( "A) rects[i].height = " + rects[i].height );
						Debug.Log( "A) originalAtlasWidth = " + originalAtlasWidth );
						Debug.Log( "A) originalAtlasHeight = " + originalAtlasHeight );
						Debug.Log( "A) t2d.name = " + t2d.name );
						Debug.Log( "A) t2d.width = " + t2d.width );
						Debug.Log( "A) t2d.height = " + t2d.height );
						Debug.Log( "A) Mathf.FloorToInt(rects[i].x * originalAtlasWidth) = " + Mathf.FloorToInt(rects[i].x * originalAtlasWidth) );
						Debug.Log( "A) Mathf.FloorToInt(rects[i].y * originalAtlasHeight) = " + Mathf.FloorToInt(rects[i].y * originalAtlasHeight) );
						Debug.Log( "=============================================================================== ");
						*/


						// Draw each of the textures into the atlas
						atlas.SetPixels(	Mathf.FloorToInt(rects[i].x * originalAtlasWidth), 		// X
											Mathf.FloorToInt(rects[i].y * originalAtlasHeight), 	// Y
											t2d.width, 												// Width of texture (this is already resized)
											t2d.height, 											// Height of texture (this is already resized)
											modifiedTexturePixels, 									// Use the modified texture pixels
											0 														// Mipmap level
						);

					// ------------------------------
					// WITHOUT COLOR MODIFIER
					// ------------------------------

					// The fast way ( without property color modifications ) ...
					} else {

						// DEBUG
						/*
						Debug.Log( "B) rects[i].x = " + rects[i].x );
						Debug.Log( "B) rects[i].y = " + rects[i].y );
						Debug.Log( "B) rects[i].width = " + rects[i].width );
						Debug.Log( "B) rects[i].height = " + rects[i].height );
						Debug.Log( "B) originalAtlasWidth = " + originalAtlasWidth );
						Debug.Log( "B) originalAtlasHeight = " + originalAtlasHeight );
						Debug.Log( "B) t2d.name = " + t2d.name );
						Debug.Log( "B) t2d.width = " + t2d.width );
						Debug.Log( "B) t2d.height = " + t2d.height );
						Debug.Log( "B) Mathf.FloorToInt(rects[i].x * originalAtlasWidth) = " + Mathf.FloorToInt(rects[i].x * originalAtlasWidth) );
						Debug.Log( "B) Mathf.FloorToInt(rects[i].y * originalAtlasHeight) = " + Mathf.FloorToInt(rects[i].y * originalAtlasHeight) );
						Debug.Log( "=============================================================================== ");
						*/

						
						// Draw each of the textures into the atlas
						atlas.SetPixels32(	Mathf.FloorToInt(rects[i].x * originalAtlasWidth), 		// X
											Mathf.FloorToInt(rects[i].y * originalAtlasHeight), 	// Y
											t2d.width, 												// Width of texture (this is already resized)
											t2d.height, 											// Height of texture (this is already resized)
											t2d.GetPixels32(), 										// Pixels from texture
											0 														// Mipmap level
						);
						

					}

					// Increment rect index
					i++;
				}

				// Done!
				atlas.Apply();
				return true;


			// Otherwise show error message and return false
			} else {
				Debug.LogError( "MESHKIT: MeshRenderer Combine couldn't create atlas because the textures and packed rects array length were not the same.");
			}

			// Something went wrong
			return false;
		}

/// -> 		[EDITOR ONLY] BAKE COLOR INTO MAIN TEXTURE ATLAS

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] BAKE COLOR INTO MAIN TEXTURE ATLAS
		//	This method allows us to mix in the _Color property directly into _MainTex atlas. It doesnt need to return true or false because it shouldn't mess up the
		//	combine process even if it fails.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static void BakeColorIntoMainTextureAtlas( ref Texture2D mainTextureAtlas, ref Rect[] rects, ref TexturePropertySetup[] texturePropertySetup ){
			
			// Make sure the texturePropertySetup at entry 0 (_MainTex) and packedRects arrays are the same length
			if( texturePropertySetup.Length > 0 && texturePropertySetup[0].colorsToBakeIntoMainTex.Length == rects.Length ){

				// Make sure the atlas isn't null for extra security...
				if( mainTextureAtlas != null ){

					// Cache the atlas width and height
					float atlasWidth = (float)mainTextureAtlas.width;
					float atlasHeight = (float)mainTextureAtlas.height;

					// Helpers
					int x = 0;
					int y = 0;
					int width = 0;
					int height = 0;
					int pixelsLength = 0;
					Color colorToBlend = Color.white;

					// Show progress bar for each submesh
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar( "Combining Mesh Renderers", "Baking Colors Into Main Texture Atlas...", 1f );
					#endif

					// Loop through the first texture property's (_MainTex) colors array ...
					for( int i = 0; i < texturePropertySetup[0].colorsToBakeIntoMainTex.Length; i++ ){

						// Cache the current color to blend
						colorToBlend = texturePropertySetup[0].colorsToBakeIntoMainTex[i];

						// If the current color to blend isn't white, then process the pixels ...
						if( colorToBlend != Color.white ){

							// Calculate the pixel area of each part of the main texture atlas ...
							x = Mathf.FloorToInt( rects[i].x * atlasWidth );
							y = Mathf.FloorToInt( rects[i].y * atlasHeight );
							width = Mathf.FloorToInt( rects[i].width * atlasWidth );
							height = Mathf.FloorToInt( rects[i].height * atlasHeight );

							// Get the pixels of each part of the main texture atlas ...
							Color[] pixels = mainTextureAtlas.GetPixels( x, y, width, height );

							// Cache the length of the pixels array
							pixelsLength = pixels.Length;

							// Process the pixels ...
							for( int p = 0; p < pixelsLength; p++ ){

								// Multiply the Atlas texture area with the cached _Color property
								pixels[p] = pixels[p] * colorToBlend;
							}

							// Set the pixels back
							mainTextureAtlas.SetPixels( x, y, width, height, pixels, 0 );

						}
					}

					// Apply the Texture atlas at the end
					mainTextureAtlas.Apply();


				// Otherwise show error message
				} else {
					Debug.LogError( "MESHKIT: MeshRenderer Combine couldn't create bake color into the main texture atlas because the supplied atlas was null.");
				}
				
			// Otherwise show error message and return false
			} else {
				Debug.LogError( "MESHKIT: MeshRenderer Combine couldn't create bake color into the main texture atlas because the colorsToBakeIntoMainTex and rects array length were not the same.");
			}
		}


/// -> 		[EDITOR ONLY] CUSTOM PIXEL PROCESSING INTO MAIN TEXTURE ATLAS

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	[EDITOR ONLY] CUSTOM PIXEL PROCESSING INTO MAIN TEXTURE ATLAS
		//	Allows us to run the custom pixel processing in the main texture atlas
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static void CustomPixelProcessingIntoMainTextureAtlas( ref Texture2D mainTextureAtlas, ref Rect[] rects, ref TexturePropertySetup[] texturePropertySetup, ref BakeFloatIntoTextureProperty[] bakeFloatsIntoTextures, ref BakeColorIntoTextureProperty[] bakeColorIntoTextures ){
			
			// Make sure the we have items in the tps and the custom pixel arrays
			if( texturePropertySetup.Length > 0 && ( bakeFloatsIntoTextures.Length > 0 || bakeColorIntoTextures.Length > 0 ) ){

				// Make sure the atlas isn't null for extra security...
				if( mainTextureAtlas != null ){

					// Cache the atlas width and height
					float atlasWidth = (float)mainTextureAtlas.width;
					float atlasHeight = (float)mainTextureAtlas.height;

					// Helpers
					int x = 0;
					int y = 0;
					int width = 0;
					int height = 0;
					// int pixelsLength = 0;
					float matFloatValue = 0f;
					Color matColorValue = Color.white;
					Color[] pixels;

					// Show progress bar for each submesh
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar( "Combining Mesh Renderers", "Processing Custom Pixels Into Main Texture Atlas...", 1f );
					#endif

					// Track if we actually apply any changes
					bool pixelsWereChanged = false;

					// Cache the material length in the first texture property setup (_MainTex)
					int tpsMaterialsFromIndexZeroLength = texturePropertySetup[0].materials.Length;

					// -----------------------------
					//	DO CUSTOM FLOAT PROCESSING
					// -----------------------------

					// Loop through each of the entries for pixel processing
					foreach( var bakeFloatsIntoTexture in bakeFloatsIntoTextures ){

						// Make sure the property name matches
						if( bakeFloatsIntoTexture.texturePropertyName == texturePropertySetup[0].propertyName ){

							// Loop through each of material entries
							for( int i = 0; i < tpsMaterialsFromIndexZeroLength; i++ ){

								// Make sure this material has the property we're looking for
								if( texturePropertySetup[0].materials[i].HasProperty( bakeFloatsIntoTexture.floatPropertyName ) == true ){

									// Cache the material's float value
									matFloatValue = texturePropertySetup[0].materials[i].GetFloat( bakeFloatsIntoTexture.floatPropertyName );

									// Calculate the pixel area of each part of the main texture atlas ...
									x = Mathf.FloorToInt( rects[i].x * atlasWidth );
									y = Mathf.FloorToInt( rects[i].y * atlasHeight );
									width = Mathf.FloorToInt( rects[i].width * atlasWidth );
									height = Mathf.FloorToInt( rects[i].height * atlasHeight );

									// Get the pixels of each part of the main texture atlas ...
									pixels = mainTextureAtlas.GetPixels( x, y, width, height );

									// Process the pixels
									BakeFloatIntoTextureProperty.ProcessPixels( ref pixels, bakeFloatsIntoTexture.pixelProcess, matFloatValue );

									// Set the pixels back
									mainTextureAtlas.SetPixels( x, y, width, height, pixels, 0 );
									
									// Track the fact we updated the pixels
									pixelsWereChanged = true;

								}
							}
						}
					}

					// -----------------------------
					//	DO CUSTOM COLOR PROCESSING
					// -----------------------------

					// Loop through each of the entries for pixel processing
					foreach( var bakeColorIntoTexture in bakeColorIntoTextures ){

						// Make sure the property name matches
						if( bakeColorIntoTexture.texturePropertyName == texturePropertySetup[0].propertyName ){

							// Loop through each of material entries
							for( int i = 0; i < tpsMaterialsFromIndexZeroLength; i++ ){

								// Make sure this material has the property we're looking for
								if( texturePropertySetup[0].materials[i].HasProperty( bakeColorIntoTexture.colorPropertyName ) == true ){

									// Cache the material's color value
									matColorValue = texturePropertySetup[0].materials[i].GetColor( bakeColorIntoTexture.colorPropertyName );

									// Calculate the pixel area of each part of the main texture atlas ...
									x = Mathf.FloorToInt( rects[i].x * atlasWidth );
									y = Mathf.FloorToInt( rects[i].y * atlasHeight );
									width = Mathf.FloorToInt( rects[i].width * atlasWidth );
									height = Mathf.FloorToInt( rects[i].height * atlasHeight );

									// Get the pixels of each part of the main texture atlas ...
									pixels = mainTextureAtlas.GetPixels( x, y, width, height );

									// Process the pixels
									BakeColorIntoTextureProperty.ProcessPixels( ref pixels, bakeColorIntoTexture.pixelProcess, matColorValue );

									// Set the pixels back
									mainTextureAtlas.SetPixels( x, y, width, height, pixels, 0 );
									
									// Track the fact we updated the pixels
									pixelsWereChanged = true;

								}
							}
						}
					}

					// -----------------------------
					//	APPLY IF NEEDED
					// -----------------------------

					// Apply the Texture atlas at the end
					if( pixelsWereChanged == true ){
						mainTextureAtlas.Apply();
					}

				// Otherwise show error message
				} else {
					Debug.LogWarning( "MESHKIT: MeshRenderer Combine - CustomPixelProcessingIntoMainTextureAtlas skipped because the supplied atlas was null.");
				}
				
			// Otherwise show error message and return false
			} else {
				if(MeshKitGUI.verbose){
					Debug.Log( "MESHKIT: MeshRenderer Combine - CustomPixelProcessingIntoMainTextureAtlas skipped because the array lengths were not populated.");
				}
			}
		}		

/// -> END OF CLASS
	}
}