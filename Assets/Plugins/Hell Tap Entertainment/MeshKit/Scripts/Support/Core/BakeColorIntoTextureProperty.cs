////////////////////////////////////////////////////////////////////////////////////////////////
//
//  BakeColorIntoTextureProperty.cs
//
//  Helper class for setting up how to process pixels during an atlas combine
//
//  © 2022 Melli Georgiou.
//  Hell Tap Entertainment LTD
//
////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
	using UnityEditor;
#endif

namespace HellTap.MeshKit {

	[System.Serializable]
	public class BakeColorIntoTextureProperty {

		// ------------
		// VARIABLES
		// ------------

		public string texturePropertyName = "";     						// <- Name of texture property to bake to. eg "_MainTex"
		public string colorPropertyName = "";      							// <- Name of color property to bake from. eg "_Color"
		public PixelProcess pixelProcess = PixelProcess.MultiplyRGBA;		// <- The pixel process to use when applying the color

			// Enum for Color
			public enum PixelProcess { 
				SetRGBA = 0, SetRGB = 1, SetRed = 2, SetGreen = 3, SetBlue = 4, SetAlpha = 5,									// <- Set Colors
				MultiplyRGBA = 6, MultiplyRGB = 7, MultiplyRed = 8, MultiplyGreen = 9, MultiplyBlue = 10, MultiplyAlpha = 11 	// <- Multiply Colors
			}

/// -> 	DO GUI LAYOUT

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	DO GUI LAYOUT
		//	GUI Helper method to display BakeColorIntoTextureProperty arrays
		//	Returns 0 = No issues
		//	Returns 1 = Property Cannot Be Empty
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		#if UNITY_EDITOR

			public static int DoGUILayout( Object objectToUndo, ref BakeColorIntoTextureProperty[] properties, Texture2D propertyIcon, Texture2D removeButton, Texture2D addButton ){

				// ----------------------
				//	TITLE
				// ----------------------

				// Big Spacer
				GUILayout.Label( string.Empty, GUILayout.MinHeight(16), GUILayout.MaxHeight(16) );

				// Start Horizontal Group
				GUILayout.BeginHorizontal();
				
					GUILayout.FlexibleSpace();
					GUILayout.Label("------------------      Process Color Data      ------------------", "boldLabel");
					GUILayout.FlexibleSpace();
				
				// End Horizontal Group
				GUILayout.EndHorizontal();

				// Big Spacer
				GUILayout.Label( string.Empty, GUILayout.MinHeight(16), GUILayout.MaxHeight(16) );

				// Helper
				bool listContainsEmptyPropertyName = false;
				
				// ---------------------
				//	PROPERTY LIST SETUP
				// ---------------------

				if( properties.Length == 0 ){

					// Start Horizontal Group
					GUILayout.BeginHorizontal();

						GUILayout.FlexibleSpace();
						GUILayout.Label("No custom pixel processing is currently set for color data.");
						GUILayout.FlexibleSpace();

					// End Horizontal Group
					GUILayout.EndHorizontal();

				} else {


					// ----------------------
					//	PROPERTY LIST HEADER
					// ----------------------

					// Start Horizontal Group
					GUILayout.BeginHorizontal();

						// Name / Title of object
						GUILayout.Label( string.Empty, GUILayout.MaxWidth(20), GUILayout.MaxHeight(20) );
						GUILayout.Label( string.Empty, GUILayout.MinWidth(32), GUILayout.MaxWidth(32), GUILayout.MaxHeight(20));

						// We need this weird spacing to line up the columns - weird.
						GUILayout.Label( "Texture Property:", "BoldLabel", GUILayout.MinWidth(70) );
						GUILayout.Label( "Pixel Process:         ", "BoldLabel", GUILayout.MinWidth(60) );
						GUILayout.Label( "Color Property:     ", "BoldLabel", GUILayout.MinWidth(70) );

					// End Horizontal Group
					GUILayout.EndHorizontal();

					// ----------------------
					//	ACTUAL PROPERTY LIST
					// ----------------------

					// Helper and loop
					BakeColorIntoTextureProperty p = null;
					BakeColorIntoTextureProperty tempP = new BakeColorIntoTextureProperty( string.Empty, string.Empty );
					
					// Loop through the properties
					for( int i = 0; i < properties.Length; i++ ){

						// Cache the current entry
						p = properties[i];

						// Create a new entry if one doesn't already exist
						if( p == null ){ p = new BakeColorIntoTextureProperty( string.Empty, string.Empty ); }

						// Setup the temporary property so we can use the undo check
						tempP.texturePropertyName = p.texturePropertyName;
						tempP.pixelProcess = p.pixelProcess;
						tempP.colorPropertyName = p.colorPropertyName;

						// If we find a property name that is empty, track it
						if( p.texturePropertyName == string.Empty || tempP.colorPropertyName == string.Empty ){ listContainsEmptyPropertyName = true; }

						// Create a new variable to hold the Field
						EditorGUI.BeginChangeCheck();

							// Start Horizontal Group
							GUILayout.BeginHorizontal();

								// Name / Title of object
								GUILayout.Label( propertyIcon, GUILayout.MaxWidth(20), GUILayout.MaxHeight(20) );
								GUILayout.Label( " " + (i+1).ToString() + ": ", "BoldLabel", GUILayout.MinWidth(32), GUILayout.MaxWidth(32), GUILayout.MaxHeight(20));
								tempP.texturePropertyName = EditorGUILayout.TextField( string.Empty, tempP.texturePropertyName, GUILayout.MinWidth(70) );
								tempP.pixelProcess = (PixelProcess)EditorGUILayout.EnumPopup(string.Empty, tempP.pixelProcess, GUILayout.MinWidth(60) );
								tempP.colorPropertyName = EditorGUILayout.TextField( string.Empty, tempP.colorPropertyName, GUILayout.MinWidth(70) );

							// End Horizontal Group
							GUILayout.EndHorizontal();

						// If a GUI Control has been updated while we updated the above value, record the undo!
						if (EditorGUI.EndChangeCheck()){

							// Record the undo object and set the reference to the new value
							Undo.RecordObject ( objectToUndo, "Pixel Processing Property");
							p.texturePropertyName = tempP.texturePropertyName;
							p.pixelProcess = tempP.pixelProcess;
							p.colorPropertyName = tempP.colorPropertyName;
						}

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
					if( properties.Length > 0 &&
						GUILayout.Button( new GUIContent( System.String.Empty, removeButton, "Remove Property" ), GUILayout.MinWidth(32), GUILayout.MaxWidth(32) ) 
					){
						Undo.RecordObject ( objectToUndo, "Remove Property" );
						Arrays.RemoveItemAtIndex( ref properties, properties.Length-1 );

						GUIUtility.ExitGUI();
					}

					// Add Property
					if( GUILayout.Button( new GUIContent( System.String.Empty, addButton, "Add Property" ), GUILayout.MinWidth(32), GUILayout.MaxWidth(32) ) 
					){
						Undo.RecordObject ( objectToUndo, "Add Property" );
						Arrays.AddItem( ref properties, new BakeColorIntoTextureProperty( string.Empty, string.Empty ) );
						GUIUtility.ExitGUI();
					}

				// End Horizontal Group
				GUILayout.EndHorizontal();

				// ----------------------
				//	ERROR CHECKING
				// ----------------------

				// Let the calling script know it has an empty property name.
				if( listContainsEmptyPropertyName == true ){ return 1; }
	
				// Otherwise, return 0 if all is cool
				return 0;
			}


		#endif

/// -> 	CONSTRUCTOR

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	CONSTRUCTOR
		//	Constructor method to build new BakeColorIntoTextureProperty classes
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Return an empty Array
		public static BakeColorIntoTextureProperty[] EmptyArray (){
			return new BakeColorIntoTextureProperty[0];
		}

		// Constructor
		public BakeColorIntoTextureProperty( 
			string colorPropertyName, 
			string texturePropertyName, 
			PixelProcess pixelProcess = PixelProcess.MultiplyRGBA
		){
			this.colorPropertyName = colorPropertyName;
			this.texturePropertyName = texturePropertyName;
			this.pixelProcess = pixelProcess;
		}

/// -> 	PROCESS PIXELS ( called from "ProcessPixelsUsingPropertyNameAndMaterial" )

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	PROCESS PIXELS
		//	Helper method ( called from "ProcessPixelsUsingPropertyNameAndMaterial" ) to help us process the pixels with our custom setups
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void ProcessPixels( 
			ref Color[] pixels, string propertyName, Material material, bool textureWasOriginallyNull, ref BakeColorIntoTextureProperty[] bakeColorIntoTextures
		){

			// Loop through the custom setups
			foreach( BakeColorIntoTextureProperty bcittp in bakeColorIntoTextures ){

				//Debug.Log( propertyName + " - propertyName matches with " + bcittp.texturePropertyName + "? " + (bcittp.texturePropertyName == propertyName) );
				//Debug.Log( " - material has property? " + (material.HasProperty( bcittp.floatPropertyName )) );

				// Make sure the texture property name matches and the float property exists
				if( bcittp.texturePropertyName == propertyName && material.HasProperty( bcittp.colorPropertyName ) ){ 

					// DEBUG
					// Debug.LogWarning( "Applying Custom Processing on " + propertyName + " using " + bcittp.pixelProcess + " with float value: " + f );

					// Process the pixels by sending the needed data to the raw function below
					ProcessPixels( ref pixels, bcittp.pixelProcess, material.GetColor(bcittp.colorPropertyName ) );
				}	
			}
		}

/// -> 	PROCESS PIXELS ( called from "CustomPixelProcessingIntoMainTextureAtlas" )

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	PROCESS PIXELS
		//	Helper method ( called from "CustomPixelProcessingIntoMainTextureAtlas" )
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static void ProcessPixels( ref Color[] pixels, PixelProcess pixelProcess, Color c ){

			// Cache the number of pixels in this array
			int pixelsLength = pixels.Length;

			// --------------------------
			// PIXEL PROCESS: SET RGBA
			// --------------------------

			if( pixelProcess == PixelProcess.SetRGBA ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i] = c;

				}
			}

			// --------------------------
			// PIXEL PROCESS: SET RGB
			// --------------------------

			else if( pixelProcess == PixelProcess.SetRGB ){
				for( int i = 0; i < pixelsLength; i++ ){ 
				
					pixels[i].r = c.r;
					pixels[i].g = c.g;
					pixels[i].b = c.b;
				
				}
			}

			// --------------------------
			// PIXEL PROCESS: SET RED
			// --------------------------

			else if( pixelProcess == PixelProcess.SetRed ){
				for( int i = 0; i < pixelsLength; i++ ){ 
				
					pixels[i].r = c.r;

				}
			}

			// --------------------------
			// PIXEL PROCESS: SET GREEN
			// --------------------------

			else if( pixelProcess == PixelProcess.SetGreen ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].g = c.g;

				}
			}

			// --------------------------
			// PIXEL PROCESS: SET BLUE
			// --------------------------

			else if( pixelProcess == PixelProcess.SetGreen ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].b = c.b;

				}
			}

			// --------------------------
			// PIXEL PROCESS: SET ALPHA
			// --------------------------

			else if( pixelProcess == PixelProcess.SetAlpha ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].a = c.a;
				}
			}

			// -----------------------------
			// PIXEL PROCESS: MULTIPLY RGBA
			// -----------------------------

			else if( pixelProcess == PixelProcess.MultiplyRGBA ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].r *= c.r;
					pixels[i].g *= c.g;
					pixels[i].b *= c.b;
					pixels[i].a *= c.a;

				}
			}

			// ----------------------------
			// PIXEL PROCESS: MULTIPLY RGB
			// ----------------------------

			else if( pixelProcess == PixelProcess.MultiplyRGB ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].r *= c.r;
					pixels[i].g *= c.g;
					pixels[i].b *= c.b;
				
				}
			}

			// ----------------------------
			// PIXEL PROCESS: MULTIPLY RED
			// ----------------------------

			else if( pixelProcess == PixelProcess.MultiplyRed ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].r *= c.r;

				}
			}

			// ------------------------------
			// PIXEL PROCESS: MULTIPLY GREEN
			// ------------------------------

			else if( pixelProcess == PixelProcess.MultiplyGreen ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].g *= c.g;

				}
			}

			// -----------------------------
			// PIXEL PROCESS: MULTIPLY BLUE
			// -----------------------------

			else if( pixelProcess == PixelProcess.MultiplyGreen ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].b *= c.b;

				}
			}

			// ------------------------------
			// PIXEL PROCESS: MULTIPLY ALPHA
			// ------------------------------

			else if( pixelProcess == PixelProcess.MultiplyAlpha ){
				for( int i = 0; i < pixelsLength; i++ ){ 

					pixels[i].a *= c.a;
				}
			}
		}

	}
}