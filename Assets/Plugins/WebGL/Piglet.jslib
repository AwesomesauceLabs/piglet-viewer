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

};

mergeInto(LibraryManager.library, PigletJsLib);
