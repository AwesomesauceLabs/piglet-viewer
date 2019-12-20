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

	GetFileSize: function(filenamePtr) {
		var filename = Pointer_stringify(filenamePtr);
		return window.fileData[filename].byteLength;
	},

	FreeFileData: function(filenamePtr) {
		var filename = Pointer_stringify(filenamePtr);
		_free(window.fileDataPtr);
		delete window.filePtr[filename];
		delete window.fileData[filename];
	},

};

mergeInto(LibraryManager.library, JsLib);
