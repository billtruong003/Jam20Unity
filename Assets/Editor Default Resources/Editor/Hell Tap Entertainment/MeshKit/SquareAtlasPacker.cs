////////////////////////////////////////////////////////////////////////////////////////////////
//
//  SquareAtlasPacker.cs
//
//	Helper for creating consistent square texture atlases (compatible across all platforms).
//
//	- This script divides the atlas into equal slots based on the number of textures provided.
//	- Each texture is resized to completely fill its allocated slot, maximizing space usage.
//	- The atlas is built using a grid layout, and textures are packed without leaving empty spaces.
//	- The padding between slots can be customized, and the atlas supports a customizable background color.
//	- The script uses bilinear scaling to resize textures when fitting them into their respective slots.
//
//	Features:
//	1. Slot-Based Packing: Textures are packed into an NxN grid, where each texture fills its entire slot.
//	2. Efficient Space Utilization: Ensures that textures are resized to fully fill their allocated slots without empty space.
//	3. Bilinear Scaling: Uses bilinear scaling to resize textures smoothly when fitting them into their slots.
//	4. Customizable Padding: Allows padding between textures in the atlas to be specified.
//	5. Customizable Background Color: The background color of the atlas can be set (default is transparent black).
//
//	Parameters:
//	- "Texture2D[] textures": Array of textures to pack into the atlas.
//	- "int padding": Padding between textures in the atlas.
//	- "int maxAtlasSize": The maximum allowed size of the atlas (e.g., 2048, 4096, 8192).
//	- "bool makeNoLongerReadable": If true, the atlas will be marked as non-readable for optimization.
//	- "Color? defaultBackgroundColor": Optional argument to set the background color (default is transparent black).
//
//	Returns:
//	- "Texture2D atlas": The generated texture atlas.
//	- "Rect[] packedRects": The UV rects for each packed texture.
//	- "bool errorFound": Indicates if an error occurred during packing.
//
//	© 2024 Melli Georgiou.
//	Hell Tap Entertainment LTD
//
////////////////////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

namespace HellTap.MeshKit {
	public class SquareAtlasPacker {
		
		// Define a static readonly Color for transparent black
		private static readonly Color TRANSPARENT_BLACK = new Color(0, 0, 0, 0);

/// -> PACK TEXTURES

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	PACK TEXTURES
		//	Replacement For Unity's Texture2D.PackTextures. This version is better and more consistant for MeshKit style atlassing!
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Method to pack textures into a new atlas (returns atlas, packedRects and a bool with the result)
		public static ( Texture2D atlas, Rect[] packedRects, bool errorFound) PackTextures(

			// Arguments
			Texture2D[] textures, 
			int padding, 
			int maxAtlasSize, 
			bool makeNoLongerReadable = false, 
			Color? defaultBackgroundColor = null 
		){

			// Ensure maxAtlasSize is 2048, 4096, or 8192
			if (maxAtlasSize != 2048 && maxAtlasSize != 4096 && maxAtlasSize != 8192 && maxAtlasSize != 16384)
			{
				Debug.LogError("SQUARE ATLAS PACKER: Invalid maxAtlasSize. Only 2048, 4096, 8192 or 16384 are supported.");
				return (null, null, true); // <- true = errorFound
			}

			// Step 1: Validate textures and calculate how many we have
			List<(Texture2D texture, int originalIndex)> textureList = new List<(Texture2D, int)>();
			for (int i = 0; i < textures.Length; i++){
				if (textures[i] != null){
					textureList.Add((textures[i], i));
				}
			}

			int textureCount = textureList.Count;
			if (textureCount == 0){
				Debug.LogError("SQUARE ATLAS PACKER: No textures provided.");
				return (null, null, true);  // Error: No textures to pack
			}

			// Step 2: Divide the atlas into sections based on the number of textures
			// We'll calculate the grid size (number of columns and rows)
			int gridSize = Mathf.CeilToInt(Mathf.Sqrt(textureCount)); // Create an NxN grid
			int slotWidth = (maxAtlasSize - padding * (gridSize - 1)) / gridSize; // Width of each slot
			int slotHeight = (maxAtlasSize - padding * (gridSize - 1)) / gridSize; // Height of each slot

			// Step 3: Try packing textures into each slot and enlarge them to fit
			if ( TryPackTextures(textureList, maxAtlasSize, slotWidth, slotHeight, gridSize, padding, defaultBackgroundColor ?? TRANSPARENT_BLACK, out Rect[] packedRects, out Texture2D atlas)){
				
				// Apply the atlas and return if successful
				if (makeNoLongerReadable){

					// Make the texture non-readable
					atlas.Apply(false, true); 
				
				// Otherwise, apply the texture normally
				} else {
			
					atlas.Apply();
			
				}

				// Return the atlas and packed rects
				return (atlas, packedRects, false); 
			}

			Debug.LogError("SQUARE ATLAS PACKER: Could not pack textures into the atlas.");
			return (null, null, true);  // Return an error if something goes wrong
		}

/// -> TRY PACK TEXTURES

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//	TRY PACK TEXTURES
		//	Helper function to try packing textures into calculated slots
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private static bool TryPackTextures(

			// Arguments
			List<(Texture2D texture, int originalIndex)> textureList, 
			int atlasSize, 
			int slotWidth, 
			int slotHeight, 
			int gridSize, 
			int padding, 
			Color backgroundColor, 
			out Rect[] packedRects, out Texture2D atlas
		){

			packedRects = new Rect[textureList.Count];
			atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false); // Create the atlas texture without mipmaps

			// Fill the entire atlas with the background color
			Color[] backgroundPixels = new Color[atlasSize * atlasSize];
			for (int i = 0; i < backgroundPixels.Length; i++){
				backgroundPixels[i] = backgroundColor;
			}
			atlas.SetPixels(backgroundPixels);

			int textureIndex = 0;

			// Step 4: Iterate through each slot and place the textures in it
			for ( int row = 0; row < gridSize; row++ ){
				for ( int col = 0; col < gridSize; col++ ){

					if ( textureIndex >= textureList.Count ){
						break; // We've placed all textures
					}

					// Setup the texture and original index
					Texture2D tex = textureList[textureIndex].texture;
					int originalIndex = textureList[textureIndex].originalIndex;

					// Resize the texture to fill the slot (both width and height)
					int scaledWidth = slotWidth;
					int scaledHeight = slotHeight;

					// Calculate the position within the atlas
					int posX = col * (slotWidth + padding);
					int posY = row * (slotHeight + padding);

					// Ensure that the texture doesn't exceed the bounds of the atlas
					if (posX + scaledWidth > atlasSize || posY + scaledHeight > atlasSize){

						// If it exceeds, packing failed
						return false; 
					}

					// Resize the texture to fit the entire slot and copy it into the atlas
					TextureScale.Bilinear(tex, scaledWidth, scaledHeight);
					atlas.SetPixels(posX, posY, scaledWidth, scaledHeight, tex.GetPixels());

					// Store the UV rectangle for this texture (normalized UVs based on atlas size)
					packedRects[originalIndex] = new Rect((float)posX / atlasSize, (float)posY / atlasSize, (float)scaledWidth / atlasSize, (float)scaledHeight / atlasSize);

					textureIndex++;
				}
			}

			// Apply all the pixel changes to the texture
			atlas.Apply(); 

			// Successfully packed
			return true; 
		}
	}
}
