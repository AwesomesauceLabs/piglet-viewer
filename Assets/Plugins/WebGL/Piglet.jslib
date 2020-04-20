// Javascript methods that are callable from C# code.
// See `PigletJsLib.cs` for the corresponding `extern`
// method declarations in the C# code.

var PigletJsLib = {
	// Given a C# byte[] array, return a localhost URL through which
	// the data can be read.  This method was written to facilitate
	// using UnityWebRequestTexture on in-memory PNG/JPG data.
	CreateObjectUrl: function(array, size)
	{
		// Copy the input data (`array`) from the heap to
		// a separate array (`copy`).  This is necessary
		// because any views on the heap (e.g. `slice` below)
		// are invalidated when Unity increases the
		// the heap size.  It is safe to use the data in
		// place if it is used immediately (i.e. before
		// this function returns).
		//
		// For background info, see:
		// https://emscripten.org/docs/porting/connecting_cpp_and_javascript/Interacting-with-code.html#access-memory-from-javascript

		var slice = new Uint8Array(HEAPU8.buffer, array, size);
		var copy = new Uint8Array(slice);
		var blob = new Blob([copy], {type: 'application/octet-stream'});
		var url = URL.createObjectURL(blob);

		// Return a C# string to the caller.
		//
		// For discussion, see:
		// https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html

		var bufferSize = lengthBytesUTF8(url) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(url, buffer, bufferSize);

		return buffer;
	},

	// Create a WebGL texture from in-memory PNG data.  When the
	// texture has finished loading, invoke a Unity C# callback so
	// that a corresponding Unity Texture2D object can be created using
	// Texture2D.CreateExternalTexture.
	//
	// Note 1: `textureId` identifies the texture on the
	// Unity/C# side and `nativeTextureId` identifies the texture
	// on the Javascript/WebGL side.
    //
    // Note 2: `mimeType` is typically "image/png" or "image/jpeg".
	LoadTexture: function(array, size, mimeType, textureId)
	{
		// Copy the input PNG data (`array`) from the heap to
		// a separate array (`copy`).  This is necessary
		// because any views on the heap (e.g. `slice` below)
		// are invalidated whenever the heap size grows.
		//
		// For background info, see:
		// https://emscripten.org/docs/porting/connecting_cpp_and_javascript/Interacting-with-code.html#access-memory-from-javascript

		var slice = new Uint8Array(HEAPU8.buffer, array, size);
		var copy = new Uint8Array(slice);
		var blob = new Blob([copy], {type: mimeType});

		// Make the data available through a (temporary) localhost URL,
		// so that it can be assigned to an image element.

		var url = URL.createObjectURL(blob);

		// Create an image element and load the PNG data into it.

		var image = new Image();

		image.onload = function() {

			// Create a new WebGL texture and add it to emscripten's
			// `GL.textures` array, so that it can later be
			// found/used by `Texture2D.CreateExternalTexture`.
			// See: https://forum.unity.com/threads/video-player-render-to-texture-performance-issue-in-webgl-on-chrome.735701/#post-5520109
			// and https://github.com/emscripten-core/emscripten/issues/6103#issuecomment-358916669

			var texture = GLctx.createTexture();
			var nativeTextureId = GL.getNewId(GL.textures);
			texture.name = nativeTextureId;
			GL.textures[nativeTextureId] = texture;

			// Allocate memory for the texture.
			// Note: This creates an "immutable" texture.

			GLctx.bindTexture(GLctx.TEXTURE_2D, texture);
			var format = GLctx.RGBA8;
			GLctx.texStorage2D(GLctx.TEXTURE_2D, 1, format, image.width, image.height);

			// Load the PNG data into the texture.

			GLctx.texSubImage2D(GLctx.TEXTURE_2D, 0, 0, 0, GLctx.RGBA,
					GLctx.UNSIGNED_BYTE, image);

			// Invoke Unity C# callback for creating texture
			// on the Unity side, using Texture2D.CreateExternalTexture.

			var args = [textureId, nativeTextureId, image.width, image.height].join(':');
			SendMessage("Piglet.WebGlTextureImporter (Singleton)", "OnLoadTexture", args);

		};

		image.src = url;
	},
};

mergeInto(LibraryManager.library, PigletJsLib);
