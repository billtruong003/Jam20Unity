////////////////////////////////////////////////////////////////////////////////////////////////
//
//  EditorUntileUtility.cs
//
//  EDITOR Methods for 'untiling' a mesh so that all UVs are in the 0 - 1 range
//
//  © 2022 Melli Georgiou.
//  Hell Tap Entertainment LTD
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

	public static class EditorUntileUtility {

/// ->	[EDITOR ONLY] DEBUG MENU OPTIONS

		/*
		// DEBUG Method: Shortcut From Menu - REMOVE LATER!
		[MenuItem ("Assets/MESHKIT EXPERIMENTAL - Untile MeshRenderer")]
		static void UntileMeshRendererA(){ UntileGameObject( Selection.activeGameObject, MeshKitUntileMeshSetup.UntileMode.RescaleToFit ); }

		[MenuItem ("Assets/MESHKIT EXPERIMENTAL - Untile MeshRenderer With Atlasing 2048")]
		static void UntileMeshRendererB(){ UntileGameObject( Selection.activeGameObject, MeshKitUntileMeshSetup.UntileMode.RescaleWithAtlasing, MeshKitUntileMeshSetup.MaxAtlasSize._2048, "Assets/ZZZ TEST" ); }

		[MenuItem ("Assets/MESHKIT EXPERIMENTAL - Untile MeshRenderer With Atlasing 4096 *")]
		static void UntileMeshRendererC(){ UntileGameObject( Selection.activeGameObject, MeshKitUntileMeshSetup.UntileMode.RescaleWithAtlasing, MeshKitUntileMeshSetup.MaxAtlasSize._4096, "Assets/ZZZ TEST" ); }

		[MenuItem ("Assets/MESHKIT EXPERIMENTAL - Untile MeshRenderer With Atlasing 8192")]
		static void UntileMeshRendererD(){ UntileGameObject( Selection.activeGameObject, MeshKitUntileMeshSetup.UntileMode.RescaleWithAtlasing, MeshKitUntileMeshSetup.MaxAtlasSize._8192, "Assets/ZZZ TEST" ); }
		*/

/// ->	[EDITOR ONLY] TEXTURE IMPORTER SETUP

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//  [EDITOR ONLY] TEXTURE IMPORTER SETUP
		//  This class helps us track the textures that need to be modified ( uncompressed, readable, etc ). We can do this in the Editor on demand
		//  and when we're done restore the textures back to their original settings.
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
				//  Platforms that require the fix: 'Standalone' (tested on mac, linux - verify on windows too)
				//  Platforms that work without it: 'iOS'
				if( platformString == "Standalone" ){
					requiresNormalMapRedFix = true;         // <- The normal map has a red tint on this platform and needs to be processed in order to fix it.
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
					AssetDatabase.Refresh();
					AssetDatabase.ImportAsset( assetPath );


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
					AssetDatabase.Refresh();
					AssetDatabase.ImportAsset( assetPath );

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

/// ->	[DEBUG EXPERIMENTAL] BAKE MATERIAL OFFSET AND SCALE TO UV
	/*	
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//  [DEBUG ONLY] BAKE MATERIAL OFFSET AND SCALE TO UV
		//  Allows us to bake the material's offset and scale to the UV.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Main Method
		[MenuItem ("Assets/MESHKIT EXPERIMENTAL - BAKE MATERIAL OFFSET TO MESH UV")]
		public static bool BakeMaterialOffsetAndScaleToUV(){

			GameObject selectedGameObject = Selection.activeGameObject;

			if( selectedGameObject != null &&
				selectedGameObject.GetComponent<MeshFilter>() != null &&
				selectedGameObject.GetComponent<MeshRenderer>() != null &&
				selectedGameObject.GetComponent<MeshRenderer>().sharedMaterial != null &&
				selectedGameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture != null
			){

				// Cache the needed references
				MeshFilter mf = selectedGameObject.GetComponent<MeshFilter>();
				MeshRenderer mr = selectedGameObject.GetComponent<MeshRenderer>();
				Material originalMat = mr.sharedMaterial;
				Mesh originalMesh = mf.sharedMesh;

				// Make a copy of the mesh's UVs
				Vector2[] cachedUVs = originalMesh.uv;
				int uvLength = cachedUVs.Length;

				Vector2 matOffset = originalMat.mainTextureOffset;
				Vector2 matScale = originalMat.mainTextureScale;

				// Loop through the UVs again ...
				for( int i = 0; i < uvLength; i++){

					//cachedUVs[i].x = cachedUVs[i].x + matOffset.x;
					//cachedUVs[i].y = cachedUVs[i].y + matOffset.y;

					// We should also "bake-in" the material offset and scale here
					cachedUVs[i].x = (cachedUVs[i].x * matScale.x) + matOffset.x;
					cachedUVs[i].y = (cachedUVs[i].y * matScale.y) + matOffset.y;
				}

				// Create new Material
				Material newMat = new Material( originalMat );
				newMat.mainTextureOffset = Vector2.zero;    // <- Remove offset
				newMat.mainTextureScale = Vector2.one;      // <- Reset Scale
				newMat.name = "Experimental MeshKit Material (Baked UV Offset)";

				Undo.RecordObject ( mr, "Untile");
				mr.sharedMaterial = newMat;

				// -------------------------------------------------------------------
				// PREVIEW THE MESH
				// -------------------------------------------------------------------

				Mesh previewMesh = new Mesh();
				previewMesh.name = "Experimental MeshKit Mesh (Baked UV Offset)";

				previewMesh.indexFormat = originalMesh.indexFormat;
				previewMesh.vertices = originalMesh.vertices;
				previewMesh.triangles = originalMesh.triangles;
				previewMesh.normals = originalMesh.normals;
				previewMesh.tangents = originalMesh.tangents;
				previewMesh.colors = originalMesh.colors;
				previewMesh.uv = cachedUVs;

				Undo.RecordObject ( mf, "Untile");
				mf.sharedMesh = previewMesh;

				return true;

			// Otherwise show error message and return false
			} else {
				Debug.LogError( "No.");
			}

			// Something went wrong
			return false;
		}
	*/

/// ->	[EDITOR ONLY] SCAN UVs

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//  [EDITOR ONLY] SCAN UVs
		//  Helper method to tell users the state of the UVs on a specific GameObject. Works on both SkinnedMeshRenderers and MeshRenderer/MeshFilter setups.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool ScanUVs( GameObject gameObjectToScan ){

			// If there is no GameObject to scan, return false
			if(gameObjectToScan == null ){
				EditorUtility.DisplayDialog( "MeshKit Untiler Scan", "No GameObject selected to scan!", "OK");
				return false;
			}

			// -------------------------------------------------------------------
			// CACHE NEEDED REFERENCES
			// -------------------------------------------------------------------

			// Cache needed Components
			MeshFilter mf = gameObjectToScan.GetComponent<MeshFilter>();
			MeshRenderer mr = gameObjectToScan.GetComponent<MeshRenderer>();
			SkinnedMeshRenderer smr = gameObjectToScan.GetComponent<SkinnedMeshRenderer>();
			Mesh originalMesh = null;
			Material originalMaterial = null;

			// Find the mesh and material from either the SkinnedMeshRenderer or MeshFilter/MeshRenderer
			if( smr != null ){

				originalMesh = smr.sharedMesh;
				originalMaterial = smr.sharedMaterial;
			
			} else if( mf != null && mr != null ){

				originalMesh = mf.sharedMesh;
				originalMaterial = mr.sharedMaterial;

			}

			// Make sure the needed references exist at this point
			if( originalMesh == null || originalMaterial == null ){
				EditorUtility.DisplayDialog( "MeshKit Untiler Scan", "Could not find either a mesh or material on this GameObject.", "OK");
				return false;
			}

			// Make sure the mesh only has 1 submesh
			if( originalMesh.subMeshCount > 1 ){
				EditorUtility.DisplayDialog( "MeshKit Untiler Scan", "The selected object has more than one submesh. Try seperating this object first.", "OK");
				return false;
			}

			// Make sure the material has a main texture
			if( originalMaterial.mainTexture == null ){
				EditorUtility.DisplayDialog( "MeshKit Untiler Scan", "The selected object does not have a main texture in its material.", "OK");
				return false;
			}

			// Cache the material's scale and offset
			Vector2 matOffset = originalMaterial.mainTextureOffset;
			Vector2 matScale = originalMaterial.mainTextureScale;


			// -------------------------------------------------------------------
			// FIRST SCAN
			// -------------------------------------------------------------------

			// Show progress bar
			#if SHOW_PROGRESS_BARS
				EditorUtility.DisplayProgressBar(
					"MeshKit Untiler Scan", 
					"Scanning...", 
					1f
				);
			#endif

			// Make a copy of the mesh's UVs
			Vector2[] cachedUVs = originalMesh.uv;
			
			// Setup some values to find the boundaries of the UV space being used
			float xMinUV = float.MaxValue;
			float xMaxUV = float.MinValue;
			float yMinUV = float.MaxValue;
			float yMaxUV = float.MinValue;

			// Loop through the UVs ...
			int uvLength = cachedUVs.Length;
			for( int i = 0; i < uvLength; i++){

				// First, we should "bake-in" the material's offset and scale
				cachedUVs[i].x = (cachedUVs[i].x * matScale.x) + matOffset.x;
				cachedUVs[i].y = (cachedUVs[i].y * matScale.y) + matOffset.y;

				// Discover the real coordinate space being used
				if( cachedUVs[i].x < xMinUV ){ xMinUV = cachedUVs[i].x; }
				if( cachedUVs[i].x > xMaxUV ){ xMaxUV = cachedUVs[i].x; }
				if( cachedUVs[i].y < yMinUV ){ yMinUV = cachedUVs[i].y; }
				if( cachedUVs[i].y > yMaxUV ){ yMaxUV = cachedUVs[i].y; }

			}

			// -------------------------------------------------------------------
			// SHIFT THE UV VALUES TO MAKE SURE EVERYTHING IS IN A POSITIVE RANGE
			// -------------------------------------------------------------------

			// We need to make sure that all of the UV values are in positive space in order to build an atlas for it.
			// Therefore, shift the UVs so that the minimum X and Y are in the 0-1 range.

			// Prepare X and Y shift values
			float xShift = 0f;
			float yShift = 0f;

			// Shift the UV ranges so it starts within the 0-1 range ( we also update how we've shifted them )
			while( xMinUV > 1f ){ xMinUV -= 1f; xMaxUV -= 1f; xShift -= 1f; }   // <- while we're more than 1 in the UV's X space, shift down 1.
			while( yMinUV > 1f ){ yMinUV -= 1f; yMaxUV -= 1f; yShift -= 1f; }   // <- while we're more than 1 in the UV's X space, shift down 1.

			while( xMinUV < 0f ){ xMinUV += 1f; xMaxUV += 1f; xShift += 1f; }   // <- while we're less than 0 in the UV's X space, up down 1.
			while( yMinUV < 0f ){ yMinUV += 1f; yMaxUV += 1f; yShift += 1f; }   // <- while we're less than 0 in the UV's Y space, up down 1.

			// -------------------------------------------------------------------
			// UPDATE THE CACHED UVS WITH THE SHIFTED VALUES
			// -------------------------------------------------------------------

			// Reset the boundary values ( we are keeping the xShift and yShift ones though)
			xMinUV = float.MaxValue;
			xMaxUV = float.MinValue;
			yMinUV = float.MaxValue;
			yMaxUV = float.MinValue;

			// Loop through the UVs again ...
			for( int i = 0; i < uvLength; i++){

				// Apply the shift to make sure we're using absolute numbers and
				// to ensure we start at the bottom left part of an atlas.
				cachedUVs[i].x += xShift;
				cachedUVs[i].y += yShift;

				// While we do that, let's update the UV space being used after the shift.
				if( cachedUVs[i].x < xMinUV ){ xMinUV = cachedUVs[i].x; }
				if( cachedUVs[i].x > xMaxUV ){ xMaxUV = cachedUVs[i].x; }
				if( cachedUVs[i].y < yMinUV ){ yMinUV = cachedUVs[i].y; }
				if( cachedUVs[i].y > yMaxUV ){ yMaxUV = cachedUVs[i].y; }
			}

			// -------------------------------------------------------------------
			// IF WE DON'T NEED SCALING, TELL THE USER
			// -------------------------------------------------------------------

			// If no UV shifting is needed and everything is in the 0-1 range, exit early!
			if( xShift == 0 && yShift == 0 &&
				xMinUV >= 0 && xMaxUV <= 1 &&
				yMinUV >= 0 && yMaxUV <= 1
			){

				Debug.Log("MESHKIT: No UV Scaling Is Needed For This Object!");
				
				// Remove progress bar
				EditorUtility.ClearProgressBar();

				// Show Message
				EditorUtility.DisplayDialog(	
					"MeshKit Untiler Scan", "No UV Scaling Is Needed For This Object!", "OK"
				);
				
				// DEBUG - Uncomment this section below if we want to also see debug info on objects that don't need untiling.

				/*
				// Calculate the number of tiling textures we need on the atlas (ceil ensure we always have enough)
				float numberOfHorizontalTextures = Mathf.Ceil( (float)xMaxUV );
				float numberOfVerticalTextures = Mathf.Ceil( (float)yMaxUV );

				// Create a string containing the results
				string scanResults = 
					"UV SCAN RESULTS:\n" + 
					originalMesh.name + ":\n\n" +

					"UV BOUNDARIES:\n"+
					"Lowest X UV: " + xMinUV + "\nHighest X UV: " + xMaxUV + "\n" +
					"Lowest Y UV: " + yMinUV + "\nHighest Y UV: " + yMaxUV + "\n\n" +

					"ATLAS REQUIREMENTS:\n"+
					"X Tiling Textures Required: " + numberOfHorizontalTextures + "\n" +
					"Y Tiling Textures Required: " + numberOfVerticalTextures + "\n\n" +

					"No UV Scaling Is Needed For This Object!"
				;

				// Also show it in the debugger
				Debug.Log("MESHKIT: No UV Scaling Is Needed For This Object!");

				// Remove progress bar
				EditorUtility.ClearProgressBar();

				// Show Message
				EditorUtility.DisplayDialog( "MeshKit Untiler Scan", scanResults, "OK" );
				*/

			} else {

				// Calculate the number of tiling textures we need on the atlas (ceil ensure we always have enough)
				float numberOfHorizontalTextures = Mathf.Ceil( (float)xMaxUV );
				float numberOfVerticalTextures = Mathf.Ceil( (float)yMaxUV );

				// Create a string containing the results
				string scanResults = 
					"UV SCAN RESULTS:\n" + 
					"Problematic UVs on Mesh: " + originalMesh.name + ":\n\n" +

					"UV BOUNDARIES:\n"+
					"Lowest X UV: " + xMinUV + "\nHighest X UV: " + xMaxUV + "\n" +
					"Lowest Y UV: " + yMinUV + "\nHighest Y UV: " + yMaxUV + "\n\n" +

					"ATLAS REQUIREMENTS:\n"+
					"X Tiling Textures Required: " + numberOfHorizontalTextures + "\n" +
					"Y Tiling Textures Required: " + numberOfVerticalTextures + "\n"
				;

				// Also show it in the debugger
				Debug.Log( scanResults );

				// Remove progress bar
				EditorUtility.ClearProgressBar();

				// Show Message
				EditorUtility.DisplayDialog( "MeshKit Untiler Scan", scanResults, "OK" );
			}

			// Done!
			return true;
		}

/// ->	[EDITOR ONLY] UNTILE GAMEOBJECT

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//  [EDITOR ONLY] UNTILE GAMEOBJECT
		//  This class helps with various modes to make UVs use the 0-1 coordinate space.
		//  This is needed for atlassing to work more efficiently and works with MeshFilters and Skinned Mesh Renderers.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Class for tracking textures
		public class PropertyToUntile {
			public string propertyName = string.Empty;
			public Texture2D oldTexture2D = null;
			public Texture2D newTexture2D = null;
			public bool isNormalMap = false;

			// Constructor
			public PropertyToUntile( string propertyName, Texture2D oldTexture2D ){
				this.propertyName = propertyName;
				this.oldTexture2D = oldTexture2D;
			}
		}

		// Main Method
		public static bool UntileGameObject( 
			GameObject selectedGameObject, 																		// GameObject to untile
			MeshKitUntileMeshSetup.UntileMode untileMode,														// What type of Untile Mode should we use?
			MeshKitUntileMeshSetup.MaxAtlasSize maxAtlasSizeEnum = MeshKitUntileMeshSetup.MaxAtlasSize._2048,	// If using atlassing, what is the max atlas size (as enum)
			string assetRelativePathToSaveFolder = ""															// If using atlassing, where should we save the new assets?
		){

			// ------------------------------------------------------------
			//	GAMEOBJECT CHECK
			// ------------------------------------------------------------

			// Make sure the selected GameObject is valid
			if( selectedGameObject == null ){
				EditorUtility.DisplayDialog( "MeshKit Untiler", "No GameObject was supplied.", "OK" );
				return false;
			}

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
					"MeshKit Untiler", 
					"The path to save assets does not appear to be valid:\n\n" + saveAssetDirectoryForFileAPI, "OK"
				);
				return false;
			}

			// -------------------------------------------------------------------
			// CACHE NEEDED REFERENCES
			// -------------------------------------------------------------------

			// Cache needed Components
			MeshFilter mf = selectedGameObject.GetComponent<MeshFilter>();
			MeshRenderer mr = selectedGameObject.GetComponent<MeshRenderer>();
			SkinnedMeshRenderer smr = selectedGameObject.GetComponent<SkinnedMeshRenderer>();
			Mesh originalMesh = null;
			Material originalMat = null;

			// Find the mesh and material from either the SkinnedMeshRenderer or MeshFilter/MeshRenderer
			if( smr != null ){

				originalMesh = smr.sharedMesh;
				originalMat = smr.sharedMaterial;
			
			} else if( mf != null && mr != null ){

				originalMesh = mf.sharedMesh;
				originalMat = mr.sharedMaterial;

			}

			// Make sure the needed references exist at this point
			if( originalMesh == null || originalMat == null ){
				EditorUtility.DisplayDialog( "MeshKit Untiler", "Could not find either a mesh or material on this GameObject.", "OK");
				return false;
			}

			// Make sure the mesh only has 1 submesh
			if( originalMesh.subMeshCount > 1 ){
				EditorUtility.DisplayDialog( "MeshKit Untiler", "The selected object has more than one submesh. Try seperating this object first.", "OK");
				return false;
			}

			// Make sure the material has a main texture
			if( originalMat.mainTexture == null ){
				EditorUtility.DisplayDialog( "MeshKit Untiler", "The selected object does not have a main texture in its material.", "OK");
				return false;
			}

			// Cache the material's scale and offset
			Vector2 matOffset = originalMat.mainTextureOffset;
			Vector2 matScale = originalMat.mainTextureScale;
			Texture2D originalMainTexture = (Texture2D)originalMat.mainTexture;

			// ------------------------------------------------------------
			//	MAIN REFERENCE CHECKS TO GET STARTED
			// ------------------------------------------------------------

			if( selectedGameObject != null &&
				originalMesh != null &&
				originalMat != null &&
				originalMainTexture != null
			){

				// Texture Importer Setup list
				List<TextureImporterSetup> textureImporterSetups = new List<TextureImporterSetup>();

				// -------------------------------------------------------------------
				// CONVERT ATLAS SIZE ENUM TO FLOAT
				// -------------------------------------------------------------------

				// NOTE: make sure atlas is either 2048, 4096 or 8192.
				float maxAtlasSize = 2048f;	// <- default
				if( maxAtlasSizeEnum == MeshKitUntileMeshSetup.MaxAtlasSize._2048 ){
					maxAtlasSize = 2048f;
				} else if( maxAtlasSizeEnum == MeshKitUntileMeshSetup.MaxAtlasSize._4096 ){
					maxAtlasSize = 4096f;
				} else if( maxAtlasSizeEnum == MeshKitUntileMeshSetup.MaxAtlasSize._8192 ){
					maxAtlasSize = 8192f;
				}

				// -------------------------------------------------------------------
				// PREPARE AND ANALYZE THE MESH'S UV COORDINATES
				// -------------------------------------------------------------------

				// Show progress bar
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar(
						"MeshKit Untiler", 
						"Preparing...", 
						1f
					);
				#endif

				// Make a copy of the mesh's UVs
				Vector2[] cachedUVs = originalMesh.uv;
				
				// Setup some values to find the boundaries of the UV space being used
				float xMinUV = float.MaxValue;
				float xMaxUV = float.MinValue;
				float yMinUV = float.MaxValue;
				float yMaxUV = float.MinValue;

				// Loop through the UVs ...
				int uvLength = cachedUVs.Length;
				for( int i = 0; i < uvLength; i++){

					// First, we should "bake-in" the material's offset and scale
					cachedUVs[i].x = (cachedUVs[i].x * matScale.x) + matOffset.x;
					cachedUVs[i].y = (cachedUVs[i].y * matScale.y) + matOffset.y;

					// Discover the real coordinate space being used
					if( cachedUVs[i].x < xMinUV ){ xMinUV = cachedUVs[i].x; }
					if( cachedUVs[i].x > xMaxUV ){ xMaxUV = cachedUVs[i].x; }
					if( cachedUVs[i].y < yMinUV ){ yMinUV = cachedUVs[i].y; }
					if( cachedUVs[i].y > yMaxUV ){ yMaxUV = cachedUVs[i].y; }

				}

				// Debug the min and max UV ranges
				if( MeshKitGUI.verbose ){
					Debug.Log(
						" ORIGINAL VALUES:"+
						"\nUV Min X: " + xMinUV + "  - UV Max X: " + xMaxUV +
						"\nUV Min Y: " + yMinUV + "  - UV Max Y: " + yMaxUV
					);
				}


				// -------------------------------------------------------------------
				// SHIFT THE UV VALUES TO MAKE SURE EVERYTHING IS IN A POSITIVE RANGE
				// -------------------------------------------------------------------

				// We need to make sure that all of the UV values are in positive space in order to build an atlas for it.
				// Therefore, shift the UVs so that the minimum X and Y are in the 0-1 range.

				// Prepare X and Y shift values
				float xShift = 0f;
				float yShift = 0f;

				// Shift the UV ranges so it starts within the 0-1 range ( we also update how we've shifted them )
				while( xMinUV > 1f ){ xMinUV -= 1f; xMaxUV -= 1f; xShift -= 1f; }   // <- while we're more than 1 in the UV's X space, shift down 1.
				while( yMinUV > 1f ){ yMinUV -= 1f; yMaxUV -= 1f; yShift -= 1f; }   // <- while we're more than 1 in the UV's X space, shift down 1.

				while( xMinUV < 0f ){ xMinUV += 1f; xMaxUV += 1f; xShift += 1f; }   // <- while we're less than 0 in the UV's X space, up down 1.
				while( yMinUV < 0f ){ yMinUV += 1f; yMaxUV += 1f; yShift += 1f; }   // <- while we're less than 0 in the UV's Y space, up down 1.

				// -------------------------------------------------------------------
				// UPDATE THE CACHED UVS WITH THE SHIFTED VALUES
				// -------------------------------------------------------------------

				// Reset the boundary values ( we are keeping the xShift and yShift ones though)
				xMinUV = float.MaxValue;
				xMaxUV = float.MinValue;
				yMinUV = float.MaxValue;
				yMaxUV = float.MinValue;

				// Loop through the UVs again ...
				for( int i = 0; i < uvLength; i++){

					// Apply the shift to make sure we're using absolute numbers and
					// to ensure we start at the bottom left part of an atlas.
					cachedUVs[i].x += xShift;
					cachedUVs[i].y += yShift;

					// While we do that, let's update the UV space being used after the shift.
					if( cachedUVs[i].x < xMinUV ){ xMinUV = cachedUVs[i].x; }
					if( cachedUVs[i].x > xMaxUV ){ xMaxUV = cachedUVs[i].x; }
					if( cachedUVs[i].y < yMinUV ){ yMinUV = cachedUVs[i].y; }
					if( cachedUVs[i].y > yMaxUV ){ yMaxUV = cachedUVs[i].y; }
				}

				// Debug the min and max UV ranges
				if( MeshKitGUI.verbose ){
					Debug.Log(
						" AFTER SHIFTING THE VALUES: ( " + xShift + " , " + yShift + " )"+
						"\nUV Min X: " + xMinUV + "  - UV Max X: " + xMaxUV +
						"\nUV Min Y: " + yMinUV + "  - UV Max Y: " + yMaxUV
					);
				}

				// -------------------------------------------------------------------
				// IF WE DON'T NEED SCALING, EXIT NOW!
				// -------------------------------------------------------------------

				// If no UV shifting is needed and everything is in the 0-1 range, exit early!
				if( xShift == 0 && yShift == 0 &&
					xMinUV >= 0 && xMaxUV <= 1 &&
					yMinUV >= 0 && yMaxUV <= 1
				){

					Debug.Log("MESHKIT: No UV Scaling Is Needed For This Object!");

					// Remove progress bar
					EditorUtility.ClearProgressBar();

					// Show Message
					EditorUtility.DisplayDialog(	
						"MeshKit Untiler", "No UV Scaling Is Needed For This Object!", "OK"
					);
					return false;
				}


				// -------------------------------------------------------------------
				// SCALE UVs TO FIT IN THE 0 - 1 RANGE
				// -------------------------------------------------------------------

				if( untileMode == MeshKitUntileMeshSetup.UntileMode.RescaleToFit ){

					// ----------------
					// FIX UVs
					// ----------------

					// Show progress bar
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"MeshKit Untiler", 
							"Rescaling UVs to fit...", 
							1f
						);
					#endif

					// Loop through the UVs again ...
					for( int i = 0; i < uvLength; i++){

						cachedUVs[i].x = (cachedUVs[i].x / xMaxUV);
						cachedUVs[i].y = (cachedUVs[i].y / yMaxUV);
					}

					// Quick UV Debug
					if( MeshKitGUI.verbose ){
						DebugUVSpace( ref cachedUVs, "AFTER STRETCING UVs" );
					}

					// ----------------
					// CREATE MATERIAL
					// ----------------

					// Show progress bar
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"MeshKit Untiler", 
							"Building Material...", 
							1f
						);
					#endif

					// Create new Material
					Material newMat = new Material( originalMat );
					newMat.name = selectedGameObject.name + " (Untiled) Material";
					newMat.mainTextureOffset = Vector2.zero;    // <- Remove offset
					newMat.mainTextureScale = Vector2.one;      // <- Reset Scale
					
					// Save the material
					AssetDatabase.CreateAsset(newMat, saveAssetDirectory + selectedGameObject.name.MakeFileSystemSafe() + " (Untiled) Material.asset");

					// Apply the material to the right place
					if( mr != null ){

						Undo.RecordObject ( mr, "Untile");
						mr.sharedMaterial = newMat;

					} else if( smr != null ){

						Undo.RecordObject ( smr, "Untile");
						smr.sharedMaterial = newMat;
					}
				}

				// -------------------------------------------------------------------
				// SCALE UVs AND CREATE NEW TEXTURE ATLASES
				// -------------------------------------------------------------------

				if( untileMode == MeshKitUntileMeshSetup.UntileMode.RescaleWithAtlasing ){

					// ----------------------------
					//  HANDLE TEXTURE PROPERTIES
					// ----------------------------

					// Show progress bar
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"MeshKit Untiler", 
							"Scanning Material Properties...", 
							1f
						);
					#endif

					// Get the texture property names on this material
					string[] tempPropertyNames = originalMat.GetTexturePropertyNames();
					List<string> propertyNames = new List<string>(); // <- this will be the accurate one

					// Create a list and only setup the texture properties that have a texture assigned in the material
					List<PropertyToUntile> propertiesToUntile = new List<PropertyToUntile>();
					foreach( string pn in tempPropertyNames ){ 

						// Make sure we can extract a Texture2D from this slot
						// NOTE: GetTexturePropertyNames returns all properties saved in the material, but it seems HasProperty
						// only returns true if it is currently being used in the current shader so we need a specific check here.
						if( originalMat.HasProperty(pn) == true &&	
							originalMat.GetTexture(pn) as Texture2D != null 
						){
							
							// Add the name to the property names list
							propertyNames.Add(pn);

							// Debug
							if( MeshKitGUI.verbose ){ Debug.Log("Added to propertiesToUntile: " + pn); }

							// Setup a new PropertyToUntile item
							propertiesToUntile.Add( new PropertyToUntile( pn, originalMat.GetTexture(pn) as Texture2D )  );
							
						}
					}

					// ----------------------------------------------------------------
					//	DO OPTIMIZED TEXTURE IMPORTER SETUPS BASED ON UNIQUE MATERIALS
					// ----------------------------------------------------------------

					// Show progress bar
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"MeshKit Untiler", 
							"Preparing Material Properties...", 
							1f
						);
					#endif

					// NOTE: This version isn't like the combine version, there's only 1 material!
					// Loop through the texture property names supplied by the user ...
					int propertyIndex = 0;
					foreach( string propertyName in propertyNames ){

						// If this Mesh Renderer has a material which contains one of the property names we want to extract (eg "_MainTex")...
						if( originalMat.HasProperty( propertyName) ){

							// This can happen if the main texture being used is baked into an asset
							if( (Texture2D)originalMat.GetTexture( propertyName ) != null ){

								// Cache the texture's property tex
								Texture2D propertyTex = (Texture2D)originalMat.GetTexture( propertyName );

								// If the property texture doesn't already exist in the textureImporterSetups, add it now ...
								if( DoesTextureImporterSetupContainTexture( ref textureImporterSetups, propertyTex ) == false ){

									// Cache the path of the mainTexture in the AssetDatabase
									string path = AssetDatabase.GetAssetPath ( propertyTex );
								
									// Cache the TextureImporter of the Main Texture
									TextureImporter imp = (TextureImporter) AssetImporter.GetAtPath (path);

									// Attempt to make it readable if it isn't, or if this is a normal map, make sure it is setup correctly
									if ( imp != null && ( 	imp.isReadable == false || 
															imp.textureCompression != TextureImporterCompression.Uncompressed ||
															/*
															textureFallbacks[propertyIndex] == MeshKitCombineMeshSetup.MissingTextureFallback.Normal
															*/
															imp.textureType == TextureImporterType.NormalMap
														)
									){
										// Add the original TextureImporter 
										textureImporterSetups.Add( 
											new TextureImporterSetup( propertyTex, imp, path )
										);

										// DEBUG
										//Debug.Log( "Added Property: " + propertyName + " - texture name: " + propertyTex.name );
									}	

									// Setup Normal map here too
									if( imp.textureType == TextureImporterType.NormalMap ){
										propertiesToUntile[propertyIndex].isNormalMap = true;
									}
								}
							}
						}

						// Increment Property Index
						propertyIndex++;
					}

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
									"MeshKit Untiler", 
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
								Debug.LogWarning( "MESHKIT: Untiler cancelled. Texture Format was invalid on the Texture2D named: " + textureImporterSetups[i].texture2D, textureImporterSetups[i].texture2D );
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
										"MeshKit Untiler",
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
								"MeshKit Untiler", "The operation was cancelled because a Texture format was found to be invalid. To fix this, try making sure the texture is using an uncompressed format such as 'RGBA32' and try again.", "OK"
							);
							return false;
						}
					}

					// ----------------------------
					//  SETUP VALUES FOR ATLAS
					// ----------------------------

					// Show progress bar
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"MeshKit Untiler", 
							"Configuring Atlas Settings...", 
							1f
						);
					#endif

					// Calculate the number of tiling textures we need on the atlas (ceil ensure we always have enough)
					float numberOfHorizontalTextures = Mathf.Ceil( (float)xMaxUV );
					float numberOfVerticalTextures = Mathf.Ceil( (float)yMaxUV );

					if( MeshKitGUI.verbose ){
						Debug.Log("TEXTURES NEEDED - horizontal: " + numberOfHorizontalTextures + " - vertical: " + numberOfVerticalTextures );
					}

					// Calculate a square atlas using the largest row / column size
					float squaredNumberOfTextures = Mathf.Max( numberOfHorizontalTextures, numberOfVerticalTextures ); 

					// Calculate the size of each tile ( always use 8192 to get max tex quality when scaling, etc )
					float sizeOfEachTiledTexture = Mathf.Ceil( maxAtlasSize / squaredNumberOfTextures );    // <- double check this.

					// Detect tiled textures that are too small!
					if( sizeOfEachTiledTexture < 1 ){
						
						Debug.LogError( "Atlas cannot be created with tiling textures that are less than a pixel! Try increasing the max atlas size ( or lessen the tiling amount in the material )" );
						
						// Restore the orginal texture settings
						for( int i = 0; i < textureImporterSetups.Count; i++ ){

							// Show progress bar for each submesh
							#if SHOW_PROGRESS_BARS
								EditorUtility.DisplayProgressBar(
									"MeshKit Untiler",
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
							"MeshKit Untiler", "Atlas cannot be created with tiling textures that are less than a pixel! Try increasing the max atlas size ( or lessen the tiling amount in the material )", "OK"
						);
						return false;
					}


					// Calculate the size of a slightly larger atlas so we can make sure the texture is seamless
					int slightlyLargerAtlasSize = (int)sizeOfEachTiledTexture * (int)squaredNumberOfTextures;

					// Debug Info
					if( MeshKitGUI.verbose ){
						Debug.Log( "ATLAS WILL BE: " + squaredNumberOfTextures + " x " + squaredNumberOfTextures + " ( @" + sizeOfEachTiledTexture + "px on atlas: " + slightlyLargerAtlasSize + " - resized to " +  maxAtlasSize + ")"  );
					}

					// Build the Atlas Rects
					Rect[] packedRects = new Rect[ (int)squaredNumberOfTextures * (int)squaredNumberOfTextures ];
					for( int y = 0; y < squaredNumberOfTextures; y++ ){
						for( int x = 0; x < squaredNumberOfTextures; x++ ){

							// Cache the index
							int i = (y * (int)squaredNumberOfTextures) + x;

							// Build the rects
							packedRects[i].x = Mathf.Floor( x * sizeOfEachTiledTexture );
							packedRects[i].y = Mathf.Floor( y * sizeOfEachTiledTexture );
							packedRects[i].width = sizeOfEachTiledTexture;
							packedRects[i].height = sizeOfEachTiledTexture;

						}
					}

					/// ----------------------------
					//  REBUILD MESH UVs
					// ----------------------------

					// Show progress bar
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"MeshKit Untiler", 
							"Rebuilding Mesh UVs...", 
							1f
						);
					#endif

					// Cache the rescaling multiplier for the UVs
					float rescaleMultiplier = (1f / squaredNumberOfTextures);

					// 0.0002f offset seemed to work well when using 4096 textures ( in material offset, doesnt work here)
					var scalingOffset = new Vector2( 0.0002f, 0.0002f );

					// Override for specific sizes
					if( maxAtlasSize == 8192 ){
						scalingOffset = new Vector2( 0.0001f, 0.0001f );

					} else if( maxAtlasSize == 4096 ){
						scalingOffset = new Vector2( 0.0002f, 0.0002f );

					} else if( maxAtlasSize == 2048 ){
						scalingOffset = new Vector2( 0.0004f, 0.0004f );

					}

					// Loop through the UVs again ...
					for( int i = 0; i < uvLength; i++){

						//  This mostly works. There appears to be a slight offset though
						//	The scaling offset is an attempt to lessen this, not sure what else we can do.
						cachedUVs[i].x = (float)( cachedUVs[i].x * rescaleMultiplier ) + scalingOffset.x;
						cachedUVs[i].y = (float)( cachedUVs[i].y * rescaleMultiplier ) + scalingOffset.y;
					}

					// Quick UV Debug
					//DebugUVSpace( ref cachedUVs, "AFTER SHIFT DEBUG UVs" );


					// ----------------------------
					//  PREPARE CORE TEXTURES
					// ----------------------------

					int currentPropertyIndex = 0;
					foreach( PropertyToUntile propertyToUntile in propertiesToUntile ){

						// Show progress bar
						#if SHOW_PROGRESS_BARS
							EditorUtility.DisplayProgressBar(
								"MeshKit Untiler", 
								"Creating Source Texture For Property '" + propertyToUntile.propertyName + "'... [ " + (currentPropertyIndex+1).ToString() + " / " + propertiesToUntile.Count + " ]", 
								(float)propertiesToUntile.Count / (float)currentPropertyIndex
							);
						#endif

						// Setup an empty atlas (it will be resized later)
						//Texture2D atlas = new Texture2D( (int)maxAtlasSize, (int)maxAtlasSize );

						Texture2D atlas = new Texture2D( (int)slightlyLargerAtlasSize, (int)slightlyLargerAtlasSize );


						// Copy the main texture and resize it so its ready for the atlas
					//	Texture2D resizedTextureToTile = new Texture2D( originalMainTexture.width, originalMainTexture.height );
					//	resizedTextureToTile.SetPixels( originalMainTexture.GetPixels() );
						
						// Copy the current old property texture
						Texture2D resizedTextureToTile = new Texture2D( propertyToUntile.oldTexture2D.width, propertyToUntile.oldTexture2D.height );
						resizedTextureToTile.SetPixels( propertyToUntile.oldTexture2D.GetPixels() );
						resizedTextureToTile.Apply( false );

						// Resize the old property texture to match the new tiled texture size
						TextureScale.Bilinear ( resizedTextureToTile, (int)sizeOfEachTiledTexture, (int)sizeOfEachTiledTexture );

						if( MeshKitGUI.verbose ){
							Debug.Log( "Size of tiling tex '" + propertyToUntile.propertyName + "' (" + resizedTextureToTile.width + " x " + resizedTextureToTile.height + ") - should be: " + sizeOfEachTiledTexture );
						}

						// ----------------------------
						//  FIX RED NORMAL MAPS
						// ----------------------------

						// If this is a normal map, check if it requires the Red Normal Map Fix
						if( propertyToUntile.isNormalMap && 
							DoesTextureRequireRedNormalMapFix( textureImporterSetups, propertyToUntile.oldTexture2D ) == true
						 ){

							// Show progress bar based on number of textures to fix
							#if SHOW_PROGRESS_BARS
								
								EditorUtility.DisplayProgressBar(
									"MeshKit Untiler", 
									"Fixing Normal Map Pixels For Property '" + propertyToUntile.propertyName + "'... [ " + (currentPropertyIndex+1).ToString() + " / " + propertiesToUntile.Count + " ]", 
									(float)propertiesToUntile.Count / (float)currentPropertyIndex
								);

							#endif

							// Prepare the original texture pixels
							Color[] originalTexturePixels = resizedTextureToTile.GetPixels();

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
							resizedTextureToTile.SetPixels( originalTexturePixels );

							// Apply the changes
							resizedTextureToTile.Apply( false );

						}

						// Show progress bar
						#if SHOW_PROGRESS_BARS
							EditorUtility.DisplayProgressBar(
								"MeshKit Untiler", 
								"Creating New Atlas For Property '" + propertyToUntile.propertyName + "'... [ " + (currentPropertyIndex+1).ToString() + " / " + propertiesToUntile.Count + " ]", 
								(float)propertiesToUntile.Count / (float)currentPropertyIndex
							);
						#endif

						// ----------------------------
						//  BUILD ATLAS
						// ----------------------------

						// Try to build the atlas
						if( CreateTilingAtlas( ref packedRects, ref atlas, resizedTextureToTile, slightlyLargerAtlasSize, Color.black   ) == true ){

							//Debug.Log( "Current Atlas Size '" + propertyToUntile.propertyName + "' (" + atlas.width + " x " + atlas.height + " - resizing ..." );

							// We need to make sure our atlas is the proper size
							TextureScale.Bilinear ( atlas, (int)maxAtlasSize, (int)maxAtlasSize );

							if( MeshKitGUI.verbose ){
								Debug.Log( "Size of Final Atlas tex '" + propertyToUntile.propertyName + "' (" + atlas.width + " x " + atlas.height + ") - should be: " + maxAtlasSize );
							}

							// Set the new atlas to the current property index
							propertyToUntile.newTexture2D = atlas;

							// ---------------------------------------------------------------------
							//	SAVE THE TEXTURE ATLASES
							// ---------------------------------------------------------------------

							// Setup the current texture atlas' file name
							string currentTextureAtlasFileNameToSave = selectedGameObject.name + " (Untiled) " + propertyToUntile.propertyName + ".png";
							currentTextureAtlasFileNameToSave.MakeFileSystemSafe(); // Fix dodgy mesh names.

							// Save the Texture using the File API, and then load it back in using the Asset Database's API.
							File.WriteAllBytes ( saveAssetDirectoryForFileAPI + currentTextureAtlasFileNameToSave, propertyToUntile.newTexture2D.EncodeToPNG());
							AssetDatabase.Refresh ();
							propertyToUntile.newTexture2D = (Texture2D) AssetDatabase.LoadAssetAtPath ( saveAssetDirectory + currentTextureAtlasFileNameToSave, typeof(Texture2D));

							// ---------------------------------------------------------------------
							//	SETUP THE ASSET'S TEXTURE IMPORTER
							// ---------------------------------------------------------------------

							// Help the AssetDatabase setup the texture properly
							TextureImporter latestAtlasTI = (TextureImporter) AssetImporter.GetAtPath ( saveAssetDirectory + currentTextureAtlasFileNameToSave );
							if( latestAtlasTI != null ){

								// FIX NORMAL MAPS: If the texture we just saved is a normal map, we should set that up in the AssetDatabase
								if ( propertyToUntile.isNormalMap ){
									latestAtlasTI.textureType = TextureImporterType.NormalMap;
								}
								
								// Setup the Max Atlas Size
								if( maxAtlasSize == 8192f ){
									latestAtlasTI.maxTextureSize = 8192;

								} else if( maxAtlasSize == 4096f ){
									latestAtlasTI.maxTextureSize = 4096;

								} else {

									latestAtlasTI.maxTextureSize = 2048;	// <- Default to a 2048 atlas if something went wrong here
								}

								// Set dirty and re-import it
								EditorUtility.SetDirty( latestAtlasTI );
								latestAtlasTI.SaveAndReimport();
							}


						// Fail if the atlas messes up ...
						} else {

							// Restore the orginal texture settings
							for( int i = 0; i < textureImporterSetups.Count; i++ ){

								// Show progress bar for each submesh
								#if SHOW_PROGRESS_BARS
									EditorUtility.DisplayProgressBar(
										"MeshKit Untiler",
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
								"MeshKit Untiler", "The operation was cancelled because a Texture atlas was not created successfully.", "OK"
							);
							return false;
						}

						// Increment Property Index
						currentPropertyIndex++;

					}

					// Show progress bar
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"MeshKit Untiler", 
							"Creating New Material...", 
							1f
						);
					#endif

					// Create new Material
					Material newMat = new Material( originalMat );
					newMat.name = selectedGameObject.name + " (Untiled) Material";
					newMat.mainTextureOffset = Vector2.zero;    // <- Remove offset
					newMat.mainTextureScale = Vector2.one;      // <- Reset Scale
					
					// Replace all textures in a loop
					//newMat.mainTexture = atlas;
					foreach( PropertyToUntile propertyToUntile in propertiesToUntile ){
						newMat.SetTexture( propertyToUntile.propertyName, propertyToUntile.newTexture2D );
					}

					// Save the material
					AssetDatabase.CreateAsset(newMat, saveAssetDirectory + selectedGameObject.name.MakeFileSystemSafe() + " (Untiled) Material.asset");

					// Apply the material to the right place
					if( mr != null ){

						Undo.RecordObject ( mr, "Untile");
						mr.sharedMaterial = newMat;

					} else if( smr != null ){

						Undo.RecordObject ( smr, "Untile");
						smr.sharedMaterial = newMat;
					}

				}

				// -------------------------------------------------------------------
				// PREVIEW THE MESH
				// -------------------------------------------------------------------

				// Show progress bar
				#if SHOW_PROGRESS_BARS
					EditorUtility.DisplayProgressBar(
						"MeshKit Untiler", 
						"Building the new Mesh...", 
						1f
					);
				#endif

				Mesh newMesh = new Mesh();
				newMesh.name = selectedGameObject.name.MakeFileSystemSafe() + " (Untiled)";

				// Copy core Mesh Data
				newMesh.indexFormat = originalMesh.indexFormat;
				newMesh.vertices = originalMesh.vertices;
				newMesh.triangles = originalMesh.triangles;
				newMesh.normals = originalMesh.normals;
				newMesh.tangents = originalMesh.tangents;
				newMesh.colors = originalMesh.colors;

				// Use the new UVs
				newMesh.uv = cachedUVs;

				// Copy the animation data also
				newMesh.bindposes = originalMesh.bindposes;
				newMesh.boneWeights = originalMesh.boneWeights;

				// Then, save the mesh here too!
				// Create the mesh in the AssetDatabase
				AssetDatabase.CreateAsset(newMesh, saveAssetDirectory + selectedGameObject.name.MakeFileSystemSafe() + " (Untiled) Mesh.asset");

				// Assign the mesh to the right place
				if( mf != null ){
				
					Undo.RecordObject ( mf, "Untile");
					mf.sharedMesh = newMesh;
				
				} else if( smr != null ){
				
					Undo.RecordObject ( smr, "Untile");
					smr.sharedMesh = newMesh;
				}
				

				// -------------------------------------------------------------------
				// SETUP THE MESHKIT UNTILE MESH SETUP
				// -------------------------------------------------------------------

				// Cache the MeshKit Untile Mesh Setup
				MeshKitUntileMeshSetup mkum = selectedGameObject.GetComponent<MeshKitUntileMeshSetup>();
				if( mkum != null ){

					// Set it up so we can restore the original settings
					mkum.originalMesh = originalMesh;
					mkum.originalMaterial = originalMat;
					mkum.generatedUntileMode = untileMode;
					mkum.generated = true;
				}

				// -------------------------------------------------------------------
				// RESTORE ORIGINAL TEXTURE SETTINGS
				// -------------------------------------------------------------------

				// Restore the orginal texture settings
				for( int i = 0; i < textureImporterSetups.Count; i++ ){

					// Show progress bar for each submesh
					#if SHOW_PROGRESS_BARS
						EditorUtility.DisplayProgressBar(
							"MeshKit Untiler",
							"Restoring textures to original settings ( " + i.ToString() + " / " + textureImporterSetups.Count.ToString() + " )", 
							(float)i / (float)textureImporterSetups.Count
						);
					#endif

					// Restore settings for each entry
					textureImporterSetups[i].ResetOriginalSettings();

				}

				// Remove progress bar
				EditorUtility.ClearProgressBar();
				return true;
			
			} else {
				Debug.LogWarning( "Requirements not met!" );
			}

			// Remove progress bar
			EditorUtility.ClearProgressBar();
			return false;
		}

/// ->		[EDITOR ONLY] CREATE TILING ATLAS

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//  [EDITOR ONLY] CREATE TILING ATLAS
		//  This method creates an atlas at the correct size and places previously resized textures into it using the packedRects array.
		//  Returns true if successful.
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static bool CreateTilingAtlas( ref Rect[] rects, ref Texture2D resizedAtlas, Texture2D resizedTextureToTile, float atlasSize, Color backgroundColor ){
			
			// Make sure the texture and packedRects arrays are the same length
			if( rects.Length > 0 && resizedAtlas != null && resizedTextureToTile != null ){

				// ----------------------------
				// HANDLE THE BACKGROUND
				// ----------------------------

				// Create the fill colours for the background
				Color32[] fillPixels = new Color32[ resizedAtlas.width * resizedAtlas.height ];
				int fillPixelsLength = fillPixels.Length;
				for( int p = 0; p < fillPixelsLength; p++ ){ fillPixels[p] = backgroundColor; }

				// Set the background color on the atlas
				resizedAtlas.SetPixels32( 0, 0, resizedAtlas.width, resizedAtlas.height, fillPixels );

				
				// ----------------------------
				// HANDLE EACH OF THE TEXTURES
				// ----------------------------

				for( int i = 0; i < rects.Length; i++ ){

					// Draw each of the textures into the atlas
					resizedAtlas.SetPixels32(   //Mathf.FloorToInt(rects[i].x),                         // X
												//Mathf.FloorToInt(rects[i].y),                         // Y
												(int)rects[i].x,                           				// X
												(int)rects[i].y,                            			// Y
												resizedTextureToTile.width,                             // Width of texture (this is already resized)
												resizedTextureToTile.height,                            // Height of texture (this is already resized)
												resizedTextureToTile.GetPixels32(),                     // Pixels from texture
												0                                                       // Mipmap level
					);
				}

				// Apply the changes
				resizedAtlas.Apply();
				return true;

			// Otherwise show error message and return false
			} else {
				Debug.LogError( "MESHKIT: CreateTilingAtlas couldn't create atlas because some of the arguments were not valid.");
			}

			// Something went wrong
			return false;
		}

/// ->		[EDITOR ONLY] DEBUG UV INFO 

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//  [EDITOR ONLY] DEBUG UV INFO 
		//  Dumps UV data to the console for quick debugging
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static void DebugUVSpace( ref Vector2[] cachedUVs, string debugLabel = "DEBUG UV INFO" ){

			// Reset the boundary values ( we are keeping the xShift and yShift ones though)
			float xMinUV = float.MaxValue;
			float xMaxUV = float.MinValue;
			float yMinUV = float.MaxValue;
			float yMaxUV = float.MinValue;

			// Loop through the UVs again ...
			int uvLength = cachedUVs.Length;
			for( int i = 0; i < uvLength; i++){ 

				// While we do that, let's update the UV space being used after the shift.
				if( cachedUVs[i].x < xMinUV ){ xMinUV = cachedUVs[i].x; }
				if( cachedUVs[i].x > xMaxUV ){ xMaxUV = cachedUVs[i].x; }
				if( cachedUVs[i].y < yMinUV ){ yMinUV = cachedUVs[i].y; }
				if( cachedUVs[i].y > yMaxUV ){ yMaxUV = cachedUVs[i].y; }
			}

			// Debug the min and max UV ranges
			Debug.Log(
				debugLabel +
				"\nUV Min X: " + xMinUV + "  - UV Max X: " + xMaxUV +
				"\nUV Min Y: " + yMinUV + "  - UV Max Y: " + yMaxUV
			);
		}

// -> END OF CLASS

	}
}
