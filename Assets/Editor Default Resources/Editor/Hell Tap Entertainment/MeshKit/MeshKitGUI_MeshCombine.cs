////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MeshKitGUI_MeshCombine.cs
//
//  Helper script for the MeshKit GUI.
//
//  © 2022 Melli Georgiou.
//  Hell Tap Entertainment LTD
//
////////////////////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using HellTap.MeshKit;

// Use HellTap Namespace
namespace HellTap.MeshKit {

	public static class MeshKitGUI_MeshCombine {

		// Combine Mode Descriptions
	
		static readonly string k_combineMeshesWithAtlassingDescriptionA = "\n<size=10><b>NOTE:</b> This combination mode creates new combined meshes and utilizes atlasing for high levels of optimization. Use can set Atlas options to combine down to a single object or one object per shader. You may also limit the number of textures that can be added to each atlas to preserve higher levels of quality. This mode disables the previous MeshRenderers and is totally self-contained in regard to materials and textures. However, this does have a few limitations which are detailed in the documentation.</size>";
		
		static readonly string k_combineMeshesWithAtlassingDescriptionB = "<size=10>Setup all the shader texture properties you want to combine below. First enter it's property name (eg '_MainTex') and then it's default texture color to use if it cannot be found in a material ( eg 'White' )</size>.";

	/// -> GET CURRENT COMBINE BUTTON NAME

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	GET CURRENT COMBINE BUTTON NAME
		//	Updates the 'primary' MeshKit button label depending on what the operation will be
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Helpers
		static readonly string k_Undo_Combine = "Undo Combine";
		static readonly string k_Setup_Combine = "Setup Combine";
		static readonly string k_Combine = "Combine";

		// Method
		public static string GetCurrentCombineButtonName( MeshKitGUI mkGUI ){
			if( mkGUI.combineSelGridInt == 0 && mkGUI.meshFilterCombineMode == MeshKitGUI.MeshFilterCombineMode.CombineMeshesWithAtlasing && mkGUI.shouldRevertMeshInsteadOfCombine == true ){
				return k_Undo_Combine;
			
			} else if( mkGUI.combineSelGridInt == 0 && mkGUI.meshFilterCombineMode == MeshKitGUI.MeshFilterCombineMode.CombineMeshesWithAtlasing && mkGUI.shouldCreateNewMeshKitCombineMeshWithAtlas == true ){
				return k_Setup_Combine;
			
			} else {
				return k_Combine;
			} 
		}

		// Method
		public static Texture2D GetCurrentCombineButtonIcon( MeshKitGUI mkGUI ){
			if( mkGUI.shouldRevertMeshInsteadOfCombine == true ){
				return MeshKitGUI.goIcon;
			
			} else if( mkGUI.shouldCreateNewMeshKitCombineMeshWithAtlas == true ){
				return MeshKitGUI.combineIcon;		// <- maybe later change this to an "+" icon or something
			
			} else {
				return MeshKitGUI.combineIcon;
			} 
		}

	/// -> DRAW OPTIONS

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	DRAW OPTIONS
		//	This essentially extends the OnInspectorGUI of the core MeshKitGUI script
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void DrawOptions( MeshKitGUI mkGUI ){

			// --------------------------------------
			//	CHECK FOR COMBINE SYSTEMS IN PARENTS
			// --------------------------------------

			// We need to make sure there are no LODGroups / MeshKitAutoLOD components on the child objects.
			if( mkGUI.combineSystemExistsInParent == true ){

				HTGUI.WrappedTextLabel("You cannot use another combine setup component ( 'AtlasMode' ) when one already exists in a parent object. To work on this object you must first finalize your combine operations on the parent and click the 'Remove Setup' button." );

				// Setup Helpers
				mkGUI.disableMainButton = true;
				mkGUI.shouldCreateNewMeshKitCombineMeshWithAtlas = true;

				// End Early
				return;
			}


			// -----------------------
			//	CHECK FOR LODGROUPS
			// -----------------------

			// We need to make sure there are no LODGroups / MeshKitAutoLOD components on the child objects.
			if( mkGUI.GetHowManyMeshKitAutoLODComponentsInChildren() > 0 || 
				mkGUI.GetHowManyLODGroupComponentsInChildren() > 0 || 
				mkGUI.GetLODSystemExistsInParent() == true
			){
				HTGUI.WrappedTextLabel("\n<size=10><b>NOTE:</b> You cannot combine objects that contain or are a part of an LOD system.</size>" );

				// Setup Helpers
				mkGUI.disableMainButton = true;
				mkGUI.shouldCreateNewMeshKitCombineMeshWithAtlas = true;

				// End Early
				return;
			}

			// -----------------------
			//	COMPONENT IS NOT SETUP
			// -----------------------

			// Cache the MeshKitCombineMeshSetup component on this GameObject (if it exists)
			MeshKitCombineMeshSetup mkcm = Selection.activeGameObject.GetComponent<MeshKitCombineMeshSetup>();

			if( mkcm == null ){

				// Setup Helpers
				mkGUI.disableMainButton = false;
				mkGUI.shouldCreateNewMeshKitCombineMeshWithAtlas = true;
				mkGUI.shouldRevertMeshInsteadOfCombine = false;
				mkGUI.showBottomSeparator = true;

				HTGUI.WrappedTextLabel("\n<size=10>Press the Setup Combine button to begin the process...</size>" );


			// ---------------------
			//	COMPONENT IS SETUP
			// ---------------------

			} else {

				// Setup Helpers
				mkGUI.disableMainButton = false;
				mkGUI.shouldCreateNewMeshKitCombineMeshWithAtlas = false;
				mkGUI.showBottomSeparator = false;

				// If we have already generated the mesh, don't allow us to recreate the mesh
				if( mkcm.IsGenerated == true ){
					mkGUI.shouldRevertMeshInsteadOfCombine = true;
				} else {
					mkGUI.shouldRevertMeshInsteadOfCombine = false;
				}

				// Show Info About This Combine Mode
				HTGUI.WrappedTextLabel( k_combineMeshesWithAtlassingDescriptionA );

				// Add Space and another sepLine
				GUILayout.Space(8);
				HTGUI.SepLine();

				// ---------------------------------------
				//	SETUP PROPERTIES WITH QUICK DROPDOWNS
				// ---------------------------------------

				// Header
				GUILayout.Space(8);
				GUILayout.Label( "Setup Properties Using Templates: ", "BoldLabel", GUILayout.MinWidth(100) );
				GUILayout.Space(8);

				// Spacer
				HTGUI.WrappedTextLabel( "<size=10>You can quickly setup properties for common shader setups using the drop down list below. You can also customize the properties after you have chosen a starting point.</size>" );
				GUILayout.Space(8);


				// Dropdown List
				mkcm.dropDownHelperList = (MeshKitCombineMeshSetup.DropDownHelperList)HTGUI_UNDO.EnumField( mkcm, "Quick Setup", MeshKitGUI.gearIcon, "Quick Setup: ", mkcm.dropDownHelperList);

				// Setup Quick Unlit Shader
				if( mkcm.dropDownHelperList == MeshKitCombineMeshSetup.DropDownHelperList.UnlitShader ){
					mkcm.propertyList = mkcm._defaultUnlitPropertyList;
					mkcm.dropDownHelperList = MeshKitCombineMeshSetup.DropDownHelperList.Select;

				// Setup Quick Bumped Shader
				} else if( mkcm.dropDownHelperList == MeshKitCombineMeshSetup.DropDownHelperList.BumpedShader ){
					mkcm.propertyList = mkcm._defaultBumpedPropertyList;
					mkcm.dropDownHelperList = MeshKitCombineMeshSetup.DropDownHelperList.Select;
				
				// Setup Quick Standard Shader
				} else if( mkcm.dropDownHelperList == MeshKitCombineMeshSetup.DropDownHelperList.StandardShader ){
					mkcm.propertyList = mkcm._defaultStandardPropertyList;
					mkcm.dropDownHelperList = MeshKitCombineMeshSetup.DropDownHelperList.Select;
				
				// Setup Quick Standard Specular Shader
				} /*else if( mkcm.dropDownHelperList == MeshKitCombineMeshSetup.DropDownHelperList.StandardSpecularShader ){
					mkcm.propertyList = mkcm._defaultStandardSpecularPropertyList;
					mkcm.dropDownHelperList = MeshKitCombineMeshSetup.DropDownHelperList.Select;
				
				}
				*/

				// Add Space and another sepLine
				GUILayout.Space(8);
				HTGUI.SepLine();


				// ---------------------
				//	DROP-DOWN HELPER
				// ---------------------

				// Label
				GUILayout.Space(8);
				GUILayout.Label("Shader Properties To Combine", "boldLabel");
				GUILayout.Space(8);

				// Spacer
				HTGUI.WrappedTextLabel( k_combineMeshesWithAtlassingDescriptionB );

				// Spacer
				GUILayout.Label( string.Empty, GUILayout.MinHeight(16), GUILayout.MaxHeight(16) );



				// ----------------------
				//	PROPERTY LIST HEADER
				// ----------------------

				// Start Horizontal Group
				GUILayout.BeginHorizontal();

					// Name / Title of object
					GUILayout.Label( string.Empty, GUILayout.MaxWidth(20), GUILayout.MaxHeight(20) );
					GUILayout.Label( string.Empty, GUILayout.MinWidth(32), GUILayout.MaxWidth(32), GUILayout.MaxHeight(20));
				//	GUILayout.Label( "Property Name: ", "BoldLabel", GUILayout.MinWidth(100) );
				//	GUILayout.Label( "Default Texture: ", "BoldLabel", GUILayout.MinWidth(100) );

					// We need this weird spacing to line up the columns - weird.
					GUILayout.Label( "Texture Property Name: ", "BoldLabel", GUILayout.MinWidth(100) );
					GUILayout.Label( "Default Texture:               ", "BoldLabel", GUILayout.MinWidth(100) );

				// End Horizontal Group
				GUILayout.EndHorizontal();

				// ---------------------
				//	PROPERTY LIST SETUP
				// ---------------------

				// Helper and loop
				MeshKitCombineMeshSetup.CombineMeshRendererSetup pl = null;
				MeshKitCombineMeshSetup.CombineMeshRendererSetup tempPL = new MeshKitCombineMeshSetup.CombineMeshRendererSetup();
				bool listContainsEmptyPropertyName = false;

				for( int i = 0; i < mkcm.propertyList.Count; i++ ){

					// Cache the current entry
					pl = mkcm.propertyList[i];

					// Create a new entry if one doesn't already exist
					if( pl == null ){ pl = new MeshKitCombineMeshSetup.CombineMeshRendererSetup(); }

					// Setup the temporary property so we can use the undo check
					tempPL.propertyName = pl.propertyName;
					tempPL.missingTextureFallback = pl.missingTextureFallback;

					// If we find a property name that is empty, track it
					if( pl.propertyName == string.Empty ){ listContainsEmptyPropertyName = true; }

					// Create a new variable to hold the Field
					EditorGUI.BeginChangeCheck();

						// Start Horizontal Group
						GUILayout.BeginHorizontal();

							// Name / Title of object
							GUILayout.Label( MeshKitGUI.goIcon, GUILayout.MaxWidth(20), GUILayout.MaxHeight(20) );
							GUILayout.Label( " " + (i+1).ToString() + ": ", "BoldLabel", GUILayout.MinWidth(32), GUILayout.MaxWidth(32), GUILayout.MaxHeight(20));
							tempPL.propertyName = EditorGUILayout.TextField( string.Empty, tempPL.propertyName, GUILayout.MinWidth(100) );
							tempPL.missingTextureFallback = (MeshKitCombineMeshSetup.MissingTextureFallback)EditorGUILayout.EnumPopup(string.Empty, tempPL.missingTextureFallback, GUILayout.MinWidth(100) );

						// End Horizontal Group
						GUILayout.EndHorizontal();

					// If a GUI Control has been updated while we updated the above value, record the undo!
					if (EditorGUI.EndChangeCheck()){

						// Record the undo object and set the reference to the new value
						Undo.RecordObject ( mkcm, "Texture Property");
						pl.propertyName = tempPL.propertyName;
						pl.missingTextureFallback = tempPL.missingTextureFallback;
					}

				}

				// -----------------------
				//	PROPERTY LIST OPTIONS
				// -----------------------

				// Start Horizontal Group
				GUILayout.BeginHorizontal();

					// Add Space
					GUILayout.FlexibleSpace();

					// Remove Property
					if( mkcm.propertyList.Count > 1 &&
						GUILayout.Button( new GUIContent( System.String.Empty, HTGUI.removeButton, "Remove Property" ), GUILayout.MinWidth(32), GUILayout.MaxWidth(32) ) 
					){
						Undo.RecordObject ( mkcm, "Remove Property" );
						mkcm.propertyList.RemoveAt( mkcm.propertyList.Count-1 );
						GUIUtility.ExitGUI();
					}

					// Add Property
					if( GUILayout.Button( new GUIContent( System.String.Empty, HTGUI.addButton, "Add Property" ), GUILayout.MinWidth(32), GUILayout.MaxWidth(32) ) 
					){
						Undo.RecordObject ( mkcm, "Add Property" );
						mkcm.propertyList.Add( new MeshKitCombineMeshSetup.CombineMeshRendererSetup() );
						GUIUtility.ExitGUI();
					}

				// End Horizontal Group
				GUILayout.EndHorizontal();

				// ----------------------
				//	ERROR CHECKING
				// ----------------------

				if( listContainsEmptyPropertyName == true ){
					
					// Don't Allow Player To use the main button
					mkGUI.disableMainButton = true;

					// Spacer
					GUILayout.Label( string.Empty, GUILayout.MinHeight(16), GUILayout.MaxHeight(16) );

					// Show Error Message In Help Box
					mkGUI.DoHelpBox( "Texture property names cannot be empty!" );
					
				}

				// Make sure the list doesn't contain duplicate names
				if( PropertyListContainsDuplicateNames( ref mkcm.propertyList ) == true ){

					// Don't Allow Player To use the main button
					mkGUI.disableMainButton = true;

					// Spacer
					GUILayout.Label( string.Empty, GUILayout.MinHeight(16), GUILayout.MaxHeight(16) );

					// Show Error Message In Help Box
					mkGUI.DoHelpBox( "Texture property Names must be unique!" );

				}


				// ----------------------------
				//	ADDITIONAL COMBINE OPTIONS
				// ----------------------------

				// Label
				GUILayout.Space(8);
				GUILayout.Label("Additional Options", "boldLabel");
				GUILayout.Space(8);

				// Bake _Color Property Into the _MainTex.
				mkcm.bakeColorIntoMainTex = HTGUI_UNDO.ToggleField( mkcm, "Bake Color Property Into MainTex", MeshKitGUI.gearIcon, "Bake Color Property Into MainTex", mkcm.bakeColorIntoMainTex  ); 

				// Create a new UV2 set for the combined meshes
				mkcm.rebuildLightmapUVs = HTGUI_UNDO.ToggleField( mkcm, "Create New Lightmap UVs", MeshKitGUI.gearIcon, "Create New Lightmap UVs", mkcm.rebuildLightmapUVs  ); 


				// ----------------------------
				//	ADVANCED PIXEL PROCESSING
				// ----------------------------

				// Add Space and another sepLine
				GUILayout.Space(8);
				HTGUI.SepLine();

				// Label
				GUILayout.Space(8);
				GUILayout.Label("Advanced Pixel Processing", "boldLabel");
				GUILayout.Space(8);

				// Info
				HTGUI.WrappedTextLabel( "<size=10>You can enable advanced pixel procesing to help with combining custom shaders and setups. It allows you to bake material data such as floats and colors into other textures in custom ways. You must make sure that each setup uses properties that exist and have the correct names in the material or it will be ignored. Typically, you should leave this feature disabled.</size>");
				GUILayout.Space(8);

				// Enable Advanced Pixel Processing?
				mkcm.useAdvancedPixelProcessing = HTGUI_UNDO.ToggleField( mkcm, "Use Advanced Pixel Processing", MeshKitGUI.gearIcon, "Use Advanced Pixel Processing", mkcm.useAdvancedPixelProcessing  ); 

				// If we've enabled Advanced Pixel Processing, handle it
				if( mkcm.useAdvancedPixelProcessing == true ){

					int result = 0;

					// These classes have the GUI Layout built in! They return 0 if no issues occured.
					result = BakeFloatIntoTextureProperty.DoGUILayout( mkcm, ref mkcm.bakeFloatsIntoTextures, MeshKitGUI.goIcon, HTGUI.removeButton, HTGUI.addButton );
					if( result == 1 ){

						// Don't Allow Player To use the main button
						mkGUI.disableMainButton = true;

						// Show Error Message In Help Box
						mkGUI.DoHelpBox( "Texture / Float property names cannot be empty!" );

						// Spacer
						GUILayout.Label( string.Empty, GUILayout.MinHeight(16), GUILayout.MaxHeight(16) );
						
					}

					// These classes have the GUI Layout built in! They return 0 if no issues occured.
					result = BakeColorIntoTextureProperty.DoGUILayout( mkcm, ref mkcm.bakeColorIntoTextures, MeshKitGUI.goIcon, HTGUI.removeButton, HTGUI.addButton );
					if( result == 1 ){

						// Don't Allow Player To use the main button
						mkGUI.disableMainButton = true;

						// Show Error Message In Help Box
						mkGUI.DoHelpBox( "Texture / Color property names cannot be empty!" );

						// Spacer
						GUILayout.Label( string.Empty, GUILayout.MinHeight(16), GUILayout.MaxHeight(16) );
						
					}
				}

				// ---------------------
				//	ATLAS OPTIONS
				// ---------------------

				// Add Space and another sepLine
				GUILayout.Space(8);
				HTGUI.SepLine();

				// Label
				GUILayout.Space(8);
				GUILayout.Label("Atlas Options", "boldLabel");
				GUILayout.Space(8);

				// Atlas Mode
				mkcm.atlasMode = (MeshKitCombineMeshSetup.AtlasMode) HTGUI_UNDO.EnumField( mkcm, "Atlas Mode", MeshKitGUI.gearIcon, "Atlas Mode: ", mkcm.atlasMode );

				// Maximum Atlas Size
				mkcm.maximumAtlasSize = (MeshKitCombineMeshSetup.MaxAtlasSize)HTGUI_UNDO.EnumField( mkcm, "Maximum Atlas Size", MeshKitGUI.gearIcon, "Maximum Atlas Size: ", mkcm.maximumAtlasSize);

				// Maximum number of textures Per Atlas
				mkcm.maxTexturesPerAtlas = (int)HTGUI_UNDO.SliderField( mkcm, "Max Textures Per Atlas", MeshKitGUI.gearIcon, "Max Textures Per Atlas: ", mkcm.maxTexturesPerAtlas, 4, 64 );

				// ---------------------
				//	OUTPUT OPTIONS
				// ---------------------

				// Add Space and another sepLine
				GUILayout.Space(8);
				HTGUI.SepLine();

				// Label
				GUILayout.Space(8);
				GUILayout.Label("Output Options", "boldLabel");
				GUILayout.Space(8);

				HTGUI.WrappedTextLabel( mkcm.assetRelativePathToFolder != string.Empty ? "<size=10>Your combined meshes, materials and textures for this setup will be saved here:</size>" : "<size=10>As this combine mode creates multiple assets, it is unsuitable to be managed by MeshKit as a local mesh. Instead, the combined assets are created directly into a project folder you specify.\n\nClick the 'Set Saved Asset Folder' button below to choose a folder inside your project's 'Asset' directory to save your combined meshes, materials and textures.</size>");

				// Spacer
				HTGUI.WrappedTextLabel( mkcm.assetRelativePathToFolder );

				// Begin Horizontal Row for the save location button
				GUILayout.BeginHorizontal();

					// Flexible Space
					GUILayout.FlexibleSpace();

					// --------------------------
					// SHOW FOLDER BUTTON
					// --------------------------

					if( mkcm.assetRelativePathToFolder != string.Empty && GUILayout.Button(" Show Saved Asset Folder ", GUILayout.MinWidth(160)) ){
						if( AssetDatabase.IsValidFolder( mkcm.assetRelativePathToFolder ) == true ){

							// We can ping the object in the Project pane to show the user where it is
							UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath( mkcm.assetRelativePathToFolder, typeof(UnityEngine.Object));
							EditorGUIUtility.PingObject(obj);

						} else {
							
							// Tell the user this location cannot be used
							EditorUtility.DisplayDialog("Show Saved Asset Folder", "The location you have previously chosen appears to be invalid.\n\nThis can happen if the original folder was renamed or deleted. Please select a folder using the 'Set Asset Location' button.", "OK"); 
						}
					}

					// --------------------------
					// SET ASSET LOCATION BUTTON
					// --------------------------

					if( GUILayout.Button(" Set Saved Asset Folder ", GUILayout.MinWidth(160)) ){
					
						// Make Undo Point
						Undo.RecordObject ( mkcm, "Set Saved Asset Folder" );

						// Show Open Folder Panel and setup a full and asset-relative file path
						if( MeshAssets.SetupFolderToSaveAssets( "Set Saved Asset Folder", ref mkcm.assetRelativePathToFolder ) == true ){

							// Tell user the selection is fine
							EditorUtility.DisplayDialog("Set Saved Asset Folder", "The following folder will be used to save your combined assets:\n\n" + mkcm.assetRelativePathToFolder + "\n\nNOTE: It is recommended to choose a unique folder for each combine setup you create to avoid any possible conflicts.", "OK"); 

							// Update the Asset Database in case we created a new folder
							AssetDatabase.Refresh();


						} else {

							// Tell the user this location cannot be used
							EditorUtility.DisplayDialog("Set Saved Asset Folder", "The location you have chosen is invalid.\n\nYou must choose a folder inside of your project's Asset folder.", "OK"); 
						}

						// Debug Paths
						// Debug.Log( "fullFilePathToFolder: " + mkcm.fullFilePathToFolder );
						// Debug.Log( "assetRelativePathToFolder: " + mkcm.assetRelativePathToFolder );

					}

				// End Button Row
				GUILayout.EndHorizontal();


				// Add Space and another sepLine
				GUILayout.Space(8);
				HTGUI.SepLine();

				// RUN SELECTED TOOL
				GUILayout.BeginHorizontal();

					// Setup GUI for displaying Red Button
					GUIStyle finalButton = new GUIStyle(GUI.skin.button);
					finalButton.padding = new RectOffset(6, 6, 6, 6);
					finalButton.imagePosition = ImagePosition.ImageLeft;
					finalButton.fontStyle = FontStyle.Bold;

					/*
					// Undo Combined Mesh Setup
					if( mkcm.IsGenerated == true ){
						if( GUILayout.Button( new GUIContent( " Undo Combine ", MeshKitGUI.defaultIcon ), finalButton, GUILayout.MinWidth(160), GUILayout.MinHeight(40), GUILayout.MaxHeight(40) ) &&
							EditorUtility.DisplayDialog("Undo Combine Operation", "Are you sure you want to undo the combine operation on \""+Selection.activeGameObject.name+"\"?", "Yes", "No") 
						){

							// Make an undo point
							//Undo.RecordObject ( mkcm.gameObject, "Undo Combine Operation");
							mkcm.UndoCombine();

							// Update the selection
							mkGUI.OnSelectionChange();
							GUIUtility.ExitGUI();
						}
					}
					*/
								
					// Add Flexible Space
					GUILayout.FlexibleSpace();

					// Completely Remove MeshKitCombineMeshSetup Component 
					if( GUILayout.Button( new GUIContent( " Remove Setup ", MeshKitGUI.deleteIcon ), finalButton, GUILayout.MinWidth(160), GUILayout.MinHeight(40), GUILayout.MaxHeight(40) ) &&
						EditorUtility.DisplayDialog("Remove Combine Setup", "Are you sure you want to remove the MeshKitCombineMeshSetup component on \""+Selection.activeGameObject.name+"\"?", "Yes", "No") 

						&& ( mkcm.IsGenerated == false || mkcm.IsGenerated == true && EditorUtility.DisplayDialog("Remove Combine Setup", "Removing this component will remove the functionality to revert the mesh back to the state it was in before it was combined.\n\nAre you sure you wish to continue?", "Yes", "No")  )
					){

						// Make an undo point and destroy the combine setup component
						Undo.RecordObject ( mkcm.gameObject, "Remove Combine Setup");
						Undo.DestroyObjectImmediate ( mkcm );

						// Update the selection
						mkGUI.OnSelectionChange();
						GUIUtility.ExitGUI();
					}

				GUILayout.EndHorizontal();	

				// Final Space
				GUILayout.Space(8);

			}
			
		}

	/// -> PROPERTY LIST CONTAINS DUPLICATE NAMES

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	PROPERTY LIST CONTAINS DUPLICATE NAMES
		//	Called from the MeshKitGUI script to add the needed component on the GameObject
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		static bool PropertyListContainsDuplicateNames( ref List<MeshKitCombineMeshSetup.CombineMeshRendererSetup> list ){

			// Helpers
			int listCount = list.Count;
			string currentPropertyName = string.Empty;

			// First Loop
			for( int x = 0; x < listCount; x++ ){

				// Cache the current property name and start the second Loop
				currentPropertyName = list[x].propertyName;				
				for( int y = 0; y < listCount; y++ ){

					// Make sure both loop indices are not referencing the same entry and check if a duplicate was found
					if( x != y && list[x].propertyName == list[y].propertyName ){
						return true;
					}
				}
			}

			// Otherwise, return false;
			return false;

		}

	/// -> SETUP COMBINE ON THIS GAMEOBJECT

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	SETUP COMBINE COMPONENT ON THIS GAMEOBJECT
		//	Called from the MeshKitGUI script to add the needed component on the GameObject
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void SetupCombineComponentOnThisGameObject(){

			// There is no conflicting components
			if( Selection.activeGameObject.GetComponent<MeshKitUntileMeshSetup>() == null &&
				Selection.activeGameObject.GetComponent<MeshKitCombineMeshSetup>() == null &&
				Selection.activeGameObject.GetComponent<MeshKitCombineSkinnedMeshSetup>() == null &&
				Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() == null &&
				Selection.activeGameObject.GetComponent<MeshFilter>() == null &&
				Selection.activeGameObject.GetComponent<Renderer>() == null
			){

				Undo.RecordObject (Selection.activeGameObject, "Setup Mesh Combine");
				Undo.AddComponent<MeshKitCombineMeshSetup>(Selection.activeGameObject);
				GUIUtility.ExitGUI();

			} else {

				EditorUtility.DisplayDialog(	
					"MeshKitCombineMeshSetup Setup", 
					"MeshKit cannot combine multiple MeshFilters using this parent GameObject.\n\nIt already has either a 'MeshFilter', 'Renderer' or MeshKit Setup component attached.", "OK"
				);
			}
		}

	/// -> UNDO COMBINE OPERATION

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	UNDO COMBINE OPERATION
		//	Remove the combine operation using the MeshKitCombineMeshSetup component
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void UndoCombine(){

			// Cache the MeshKitCombineMeshSetup component on this GameObject (if it exists)
			MeshKitCombineMeshSetup mkcm = Selection.activeGameObject.GetComponent<MeshKitCombineMeshSetup>();

			// Make sure we have a MeshKitCombineMeshSetup component and pass the method along
			if( mkcm != null ){
				mkcm.UndoCombine();
			}
		}

	/// -> START COMBINE

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	START COMBINE
		//	Start combining the object now using the setup from the MeshKitCombineMeshSetup component
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool StartCombine(){

			// Cache the MeshKitCombineMeshSetup component on this GameObject (if it exists)
			MeshKitCombineMeshSetup mkcm = Selection.activeGameObject.GetComponent<MeshKitCombineMeshSetup>();

			// Make sure we have a MeshKitCombineMeshSetup component
			if( mkcm != null ){

				// Track if the combine was successful
				bool combineWasSuccessful = false;

				// Make sure our asset-relative folder is valid
				if( AssetDatabase.IsValidFolder( mkcm.assetRelativePathToFolder ) == true ){

					combineWasSuccessful = EditorMeshRendererUtility.CombineMeshRenderers( 
						Selection.activeGameObject, 
						mkcm.maximumAtlasSize, 
						mkcm.propertyList, 
						mkcm.bakeColorIntoMainTex,
						mkcm.rebuildLightmapUVs,
						mkcm.assetRelativePathToFolder,
						mkcm.atlasMode,
						mkcm.maxTexturesPerAtlas,
						null,							// <- Custom Selection List
						mkcm.useAdvancedPixelProcessing ? mkcm.bakeFloatsIntoTextures : null,	// <- Experimental
						mkcm.useAdvancedPixelProcessing ? mkcm.bakeColorIntoTextures : null		// <- Experimental
					);


				// If there is an issue with the asset-relative folder	
				} else {

					// Show a dialog to show 
					EditorUtility.DisplayDialog(	
						"Combine Meshes With Texture Atlassing", 
						"Original save location is no longer valid!\n\nTry pressing the 'Set Saved Asset Folder' button again to update the location.", 
						"OK"
					);

					combineWasSuccessful = false;
				}
				

				// Return if the combine was successful
				return combineWasSuccessful;
			}

			// Return false if something goes wrong
			return false;
		}


	}
}