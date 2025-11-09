////////////////////////////////////////////////////////////////////////////////////////////////
//
//  MeshKitGUI_Untile.cs
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

	public static class MeshKitGUI_Untile {

		// Untile Mode Descriptions
		static readonly string k_untileModeA = "<size=10><b>NOTE:</b> The 'Rescale To Fit' untile mode attempts to scale the UVs of the mesh back into the 0-1 range. It tries to do this uniformly and takes the object's material offset and scale into account. This mode can significantly change the original appearance of the object as the tiling is essentially removed. This operation results in a new mesh and a new material.</size>";
		
		static readonly string k_untileModeB = "<size=10><b>NOTE:</b> The 'Rescale With Atlasing' untile mode attempts to rebuild the UVs of the mesh back into the 0-1 range by creating new textures atlases. The object's material offset and scale are also taken into account. This operation results in a new mesh, material and textures.</size>";

	/// -> GET CURRENT UNTILE BUTTON NAME

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	GET CURRENT UNTILE BUTTON NAME
		//	Updates the 'primary' MeshKit button label depending on what the operation will be
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Helpers
		static readonly string k_Undo_Untile = "Undo Untile";
		static readonly string k_Setup_Untile = "Setup Untile";
		static readonly string k_Untile = "Untile";

		// Method
		public static string GetCurrentUntileButtonName( MeshKitGUI mkGUI ){

			if( mkGUI.shouldCreateNewMeshKitUntileSetupComponent ){
				return k_Setup_Untile;
			} else if( mkGUI.shouldRevertMeshInsteadOfUntile == true ){
				return k_Undo_Untile;
			} else {
				return k_Untile;
			} 
		}

		// Method
		public static Texture2D GetCurrentUntileButtonIcon( MeshKitGUI mkGUI ){
			if( mkGUI.shouldRevertMeshInsteadOfUntile == true ){
				return MeshKitGUI.goIcon;
			
			} else if( mkGUI.shouldCreateNewMeshKitUntileSetupComponent == true ){
				return MeshKitGUI.untileIcon;		// <- maybe later change this to an "+" icon or something
			
			} else {
				return MeshKitGUI.untileIcon;
			} 
		}

	/// -> DRAW OPTIONS

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	DRAW OPTIONS
		//	This essentially extends the OnInspectorGUI of the core MeshKitGUI script
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void DrawOptions( MeshKitGUI mkGUI ){

			// --------------------------------------
			//	CHECK FOR UNTILE SYSTEMS IN PARENTS
			// --------------------------------------

			// NOTE: the following few checks probably isn't necessary because this operation only affects this GameObject, so shouldn't cause an issue to other operations.

			/*
			// We need to make sure there are no LODGroups / MeshKitAutoLOD components on the child objects.
			if( mkGUI.untileSystemExistsInParent == true ){

				HTGUI.WrappedTextLabel("You cannot use another untile setup component ( 'AtlasMode' ) when one already exists in a parent object. To work on this object you must first finalize your untile operations on the parent and click the 'Remove Setup' button." );

				// Setup Helpers
				mkGUI.disableMainButton = true;
				mkGUI.shouldCreateNewMeshKitUntileSetupComponent = true;

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
				HTGUI.WrappedTextLabel("\n<size=10><b>NOTE:</b> You cannot untile objects that contain or are a part of an LOD system.</size>" );

				// Setup Helpers
				mkGUI.disableMainButton = true;
				mkGUI.shouldCreateNewMeshKitUntileSetupComponent = true;

				// End Early
				return;
			}

			*/

			// -----------------------
			//	COMPONENT IS NOT SETUP
			// -----------------------

			// Cache the MeshKitUntileMeshSetup component on this GameObject (if it exists)
			MeshKitUntileMeshSetup mkum = Selection.activeGameObject.GetComponent<MeshKitUntileMeshSetup>();

			if( mkum == null ){

				// Setup Helpers
				mkGUI.disableMainButton = false;
				mkGUI.shouldCreateNewMeshKitUntileSetupComponent = true;
				mkGUI.shouldRevertMeshInsteadOfUntile = false;
				mkGUI.showBottomSeparator = true;

				HTGUI.WrappedTextLabel("<size=10>Press the Setup Untile button to begin the process...</size>" );


			// ---------------------
			//	COMPONENT IS SETUP
			// ---------------------

			} else {

				// Setup Helpers
				mkGUI.disableMainButton = false;
				mkGUI.shouldCreateNewMeshKitUntileSetupComponent = false;
				mkGUI.showBottomSeparator = false;

				// If we have already generated the mesh, don't allow us to recreate the mesh
				if( mkum.IsGenerated == true ){
					mkGUI.shouldRevertMeshInsteadOfUntile = true;
				} else {
					mkGUI.shouldRevertMeshInsteadOfUntile = false;
				}

				// ---------------------
				//	UNTILE OPTIONS
				// ---------------------

				// Untile Mode
				mkum.untileMode = (MeshKitUntileMeshSetup.UntileMode) HTGUI_UNDO.EnumField( mkum, "Untile Mode", MeshKitGUI.gearIcon, "Untile Mode: ", mkum.untileMode );

				// Add Space
				GUILayout.Space(8);


				// RESCALE TO FIT MODE
				if( mkum.untileMode == MeshKitUntileMeshSetup.UntileMode.RescaleToFit ){

					// Show Info About This Untile Mode
					HTGUI.WrappedTextLabel( k_untileModeA );


				// RESCALE UVs WITH ATLASING
				// Show Atlas Options If Relevant
				} else if( mkum.untileMode == MeshKitUntileMeshSetup.UntileMode.RescaleWithAtlasing){


					// Show Info About This Untile Mode
					HTGUI.WrappedTextLabel( k_untileModeB );

					// Add Space and another sepLine
					GUILayout.Space(8);
					HTGUI.SepLine();

					// Label
					GUILayout.Space(8);
					GUILayout.Label("Atlas Options", "boldLabel");
					GUILayout.Space(8);

					// Maximum Atlas Size
					mkum.maximumAtlasSize = (MeshKitUntileMeshSetup.MaxAtlasSize)HTGUI_UNDO.EnumField( mkum, "Maximum Atlas Size", MeshKitGUI.gearIcon, "Maximum Atlas Size: ", mkum.maximumAtlasSize);

					// Maximum number of textures Per Atlas
					//mkum.maxTexturesPerAtlas = (int)HTGUI_UNDO.SliderField( mkum, "Max Textures Per Atlas", MeshKitGUI.gearIcon, "Max Textures Per Atlas: ", mkum.maxTexturesPerAtlas, 4, 64 );

				}

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

				HTGUI.WrappedTextLabel( mkum.assetRelativePathToFolder != string.Empty ? "<size=10>Your untiled meshes, materials and textures for this setup will be saved here:</size>" : "<size=10>As this untile mode creates multiple assets, it is unsuitable to be managed by MeshKit as a local mesh. Instead, the untiled assets are created directly into a project folder you specify.\n\nClick the 'Set Saved Asset Folder' button below to choose a folder inside your project's 'Asset' directory to save your new meshes, materials and textures.</size>");

				// Spacer
				HTGUI.WrappedTextLabel( mkum.assetRelativePathToFolder );

				// Begin Horizontal Row for the save location button
				GUILayout.BeginHorizontal();

					// Flexible Space
					GUILayout.FlexibleSpace();

					// --------------------------
					// SHOW FOLDER BUTTON
					// --------------------------

					if( mkum.assetRelativePathToFolder != string.Empty && GUILayout.Button(" Show Saved Asset Folder ", GUILayout.MinWidth(160)) ){
						if( AssetDatabase.IsValidFolder( mkum.assetRelativePathToFolder ) == true ){

							// We can ping the object in the Project pane to show the user where it is
							UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath( mkum.assetRelativePathToFolder, typeof(UnityEngine.Object));
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
						Undo.RecordObject ( mkum, "Set Saved Asset Folder" );

						// Show Open Folder Panel and setup a full and asset-relative file path
						if( MeshAssets.SetupFolderToSaveAssets( "Set Saved Asset Folder", ref mkum.assetRelativePathToFolder ) == true ){

							// Tell user the selection is fine
							EditorUtility.DisplayDialog("Set Saved Asset Folder", "The following folder will be used to save your untiled assets:\n\n" + mkum.assetRelativePathToFolder + "\n\nNOTE: It is recommended to choose a unique folder for each untile setup you create to avoid any possible conflicts.", "OK"); 

							// Update the Asset Database in case we created a new folder
							AssetDatabase.Refresh();


						} else {

							// Tell the user this location cannot be used
							EditorUtility.DisplayDialog("Set Saved Asset Folder", "The location you have chosen is invalid.\n\nYou must choose a folder inside of your project's Asset folder.", "OK"); 
						}

						// Debug Paths
						// Debug.Log( "fullFilePathToFolder: " + mkum.fullFilePathToFolder );
						// Debug.Log( "assetRelativePathToFolder: " + mkum.assetRelativePathToFolder );

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
					// Undo Untiled Mesh Setup
					if( mkum.IsGenerated == true ){
						if( GUILayout.Button( new GUIContent( " Undo Untile ", MeshKitGUI.defaultIcon ), finalButton, GUILayout.MinWidth(160), GUILayout.MinHeight(40), GUILayout.MaxHeight(40) ) &&
							EditorUtility.DisplayDialog("Undo Untile Operation", "Are you sure you want to undo the untile operation on \""+Selection.activeGameObject.name+"\"?", "Yes", "No") 
						){

							// Make an undo point
							//Undo.RecordObject ( mkum.gameObject, "Undo Untile Operation");
							mkum.UndoUntile();

							// Update the selection
							mkGUI.OnSelectionChange();
							GUIUtility.ExitGUI();
						}
					}
					*/

					// Cool debug tool that lets us scan the UVs first
					if( GUILayout.Button( new GUIContent( " Scan UVs ", MeshKitGUI.defaultIcon ), finalButton, GUILayout.MinWidth(160), GUILayout.MinHeight(40), GUILayout.MaxHeight(40) )  ){

						// Scan UVs
						EditorUntileUtility.ScanUVs( mkum.gameObject );

						// Update the selection
						GUIUtility.ExitGUI();
					}
								
					// Add Flexible Space
					GUILayout.FlexibleSpace();

					// Completely Remove MeshKitUntileMeshSetup Component 
					if( GUILayout.Button( new GUIContent( " Remove Setup ", MeshKitGUI.deleteIcon ), finalButton, GUILayout.MinWidth(160), GUILayout.MinHeight(40), GUILayout.MaxHeight(40) ) &&
						EditorUtility.DisplayDialog("Remove Untile Setup", "Are you sure you want to remove the MeshKitUntileMeshSetup component on \""+Selection.activeGameObject.name+"\"?", "Yes", "No") 

						&& ( mkum.IsGenerated == false || mkum.IsGenerated == true && EditorUtility.DisplayDialog("Remove Untile Setup", "Removing this component will remove the functionality to revert the mesh back to the state it was in before it was untiled.\n\nAre you sure you wish to continue?", "Yes", "No")  )
					){

						// Make an undo point and destroy the untile setup component
						Undo.RecordObject ( mkum.gameObject, "Remove Untile Setup");
						Undo.DestroyObjectImmediate ( mkum );

						// Update the selection
						mkGUI.OnSelectionChange();
						GUIUtility.ExitGUI();
					}

				GUILayout.EndHorizontal();	

				// Final Space
				GUILayout.Space(8);

			}
			
		}

	/// -> SETUP UNTILE ON THIS GAMEOBJECT

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	SETUP UNTILE COMPONENT ON THIS GAMEOBJECT
		//	Called from the MeshKitGUI script to add the needed component on the GameObject
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void SetupUntileComponentOnThisGameObject(){

			// There is no conflicting components
			if( Selection.activeGameObject.GetComponent<MeshKitUntileMeshSetup>() == null &&
				Selection.activeGameObject.GetComponent<MeshKitCombineMeshSetup>() == null &&
				Selection.activeGameObject.GetComponent<MeshKitCombineSkinnedMeshSetup>() == null &&

				(	// Make sure it has MF+MR or SMR setup ...
					Selection.activeGameObject.GetComponent<MeshFilter>() != null &&
					Selection.activeGameObject.GetComponent<MeshRenderer>() != null ||
					Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null
				)
			){

				Undo.RecordObject (Selection.activeGameObject, "Setup Mesh Untile");
				Undo.AddComponent<MeshKitUntileMeshSetup>(Selection.activeGameObject);
				GUIUtility.ExitGUI();

			} else {

				EditorUtility.DisplayDialog(	
					"MeshKitUntileMeshSetup Setup", 
					"MeshKit cannot untile this GameObject.\n\nIt either already has another MeshKit Setup component attached or doesn't have a valid mesh and renderer.", "OK"
				);
			}
		}

	/// -> UNDO UNTILE OPERATION

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	UNDO UNTILE OPERATION
		//	Remove the untile operation using the MeshKitUntileMeshSetup component
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void UndoUntile(){

			// Cache the MeshKitUntileMeshSetup component on this GameObject (if it exists)
			MeshKitUntileMeshSetup mkum = Selection.activeGameObject.GetComponent<MeshKitUntileMeshSetup>();

			// Make sure we have a MeshKitUntileMeshSetup component and pass the method along
			if( mkum != null ){
				mkum.UndoUntile();
			}
		}

	/// -> START UNTILE

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	START UNTILE
		//	Start combining the object now using the setup from the MeshKitUntileMeshSetup component
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool StartUntile(){

			// Cache the MeshKitUntileMeshSetup component on this GameObject (if it exists)
			MeshKitUntileMeshSetup mkum = Selection.activeGameObject.GetComponent<MeshKitUntileMeshSetup>();

			// Make sure we have a MeshKitUntileMeshSetup component
			if( mkum != null ){

				// Track if the untile was successful
				bool untileWasSuccessful = false;

				// Make sure our asset-relative folder is valid
				if( AssetDatabase.IsValidFolder( mkum.assetRelativePathToFolder ) == true ){

					untileWasSuccessful = EditorUntileUtility.UntileGameObject( 
						Selection.activeGameObject, 
						mkum.untileMode,
						mkum.maximumAtlasSize, 
						mkum.assetRelativePathToFolder
					);

				// If there is an issue with the asset-relative folder	
				} else {

					// Show a dialog to show 
					EditorUtility.DisplayDialog(	
						"Untile Mesh With Texture Atlassing", 
						"Original save location is no longer valid!\n\nTry pressing the 'Set Saved Asset Folder' button again to update the location.", 
						"OK"
					);

					untileWasSuccessful = false;
				}
				

				// Return if the untile was successful
				return untileWasSuccessful;
			}

			// Return false if something goes wrong
			return false;
		}


	}
}