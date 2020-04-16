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
};

mergeInto(LibraryManager.library, JsLib);
