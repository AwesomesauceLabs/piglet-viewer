function OnSelectGltfFile(evt)
{
	if (evt.target.files.length <= 0)
		return;

	var file = evt.target.files[0];

	console.log("loading file: " + file.name + " (size " + file.size + ")");

	var reader = new FileReader(file);
	reader.filename = file.name;

	reader.onload = function(evt) {

		window.fileData = window.fileData || {};
		window.fileData[reader.filename] = reader.result;

		SendMessage("GameManager", "ImportFileWebGl", reader.filename);

	};

	reader.readAsArrayBuffer(file);
}
