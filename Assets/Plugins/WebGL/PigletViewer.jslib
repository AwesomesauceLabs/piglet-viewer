// Javascript methods that are callable from C# code.
// See `JavascriptAPI.cs` for the corresponding `extern`
// method declarations in the C# code.

var JsLib = {

	Init: function() {
		var root = window.document;

		var inputUri = root.querySelector("#inputFile");
		inputUri.addEventListener('change', OnChooseFile);

		var canvas = root.querySelector('canvas');
		canvas.addEventListener('dragover', OnCanvasDragOver);
		canvas.addEventListener('drop', OnCanvasDrop);
	},

    // Get the byte content of the currently selected file
	GetFileData: function(filenamePtr)
	{
		var filename = Pointer_stringify(filenamePtr);

		window.filePtr = window.filePtr || {};
		window.filePtr[filename] = _malloc(
			window.fileData[filename].byteLength);

		var heapSlice = new Uint8Array(
			HEAPU8.buffer, window.filePtr[filename],
			window.fileData[filename].byteLength);
		heapSlice.set(new Uint8Array(window.fileData[filename]));

		return window.filePtr[filename];
	},

    // Get the size in bytes of the currently selected file
	GetFileSize: function(filenamePtr) {
		var filename = Pointer_stringify(filenamePtr);
		return window.fileData[filename].byteLength;
	},

    // Deallocate the byte content of the currently selected file
	FreeFileData: function(filenamePtr) {
		var filename = Pointer_stringify(filenamePtr);
		_free(window.fileDataPtr);
		delete window.filePtr[filename];
		delete window.fileData[filename];
	},

    // Append a line to the Import Log, located in the left panel
    // of the web page.
	AppendLogLine: function(stringPtr)
	{
		var string = Pointer_stringify(stringPtr);
		window.logArray = window.logArray || new Array();
		window.logArray.push(string);

		RebuildLogHtml();
	},

    // Replace the last line of the Import Log, located in the
    // left panel of the web page.
	UpdateTailLogLine: function(stringPtr)
	{
		window.logArray = window.logArray || new Array();

		if (window.logArray.length == 0) {
			AppendToLog(stringPtr);
			return;
		}

		var string = Pointer_stringify(stringPtr);
		window.logArray[window.logArray.length - 1] = string;

		RebuildLogHtml();
	},

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
	// Note: `textureId` identifies the texture on the
	// Unity/C# side and `nativeTextureId` identifies the texture
	// on the Javascript/WebGL side.
	LoadTexture: function(array, size, textureId)
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
		var blob = new Blob([copy], {type: 'image/png'});

		// Make the data available through a (temporary) localhost URL,
		// so that it can be assigned to an image element.

		var url = URL.createObjectURL(blob);

		// Create an image element and load the PNG data into it.

		var image = new Image();

		image.onload = function() {

			// Find a texture id that isn't already used by Unity

			var nativeTextureId = 0;
			while (nativeTextureId in GL.textures)
				nativeTextureId++;

			// Create a new WebGL texture and add it to Unity's
			// `GL.textures` array, so that it can later be
			// found/used by `Texture2D.CreateExternalTexture`.
			// See: https://forum.unity.com/threads/video-player-render-to-texture-performance-issue-in-webgl-on-chrome.735701/#post-5520109

			var texture = GLctx.createTexture();
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

mergeInto(LibraryManager.library, JsLib);
