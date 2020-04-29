// Event handler that is called when a file is selected with
// the <input type="file"> button (i.e. "Choose File"/"Browse").
function OnChooseFile(evt)
{
	if (evt.target.files.length <= 0)
		return;

	var file = evt.target.files[0];

	ImportFile(file);
}

function ImportFile(file)
{
	console.log("loading file: " + file.name + " (size " + file.size + ")");

	var reader = new FileReader(file);
	reader.filename = file.name;

	reader.onload = function(evt) {

		window.fileData = window.fileData || {};
		window.fileData[reader.filename] = reader.result;

        ClearImportLog();
		SendMessage("GameManager", "ImportFileWebGl", reader.filename);

	};

	reader.readAsArrayBuffer(file);
}

function ImportUrl(url)
{
	console.log("loading url: " + url);
    ClearImportLog();
	SendMessage("GameManager", "ImportUrlWebGl", url);
}

// Event handler that is called when something (e.g. a file) is dragged
// over the canvas.
//
// Counterintuitively, implementing a drop handler
// requires handling the `dragover` event and disabling default
// drag-and-drop behaviour by calling `event.preventDefault`,
// otherwise the drop handler is never invoked.
function OnCanvasDragOver(event)
{
	event.preventDefault();
}

// Event handler that is called when something (e.g. a file) is dropped
// onto the canvas.
function OnCanvasDrop(event)
{
	event.preventDefault();

	var i;
	var file;

	if (event.dataTransfer.items) {
		for (i = 0; i < event.dataTransfer.items.length; i++) {
			var item = event.dataTransfer.items[i];
			if (item.kind === 'file') {
				file = item.getAsFile();
				ImportFile(file);
				break;
			} else if (item.kind == 'string' && item.type == 'text/uri-list') {
				item.getAsString(ImportUrl);
                break;
			}
		}
	} else {
		for (i = 0; i < event.dataTransfer.files.length; i++) {
			file = event.dataTransfer.files[i];
			ImportFile(file);
			break;
		}
	}
}

// Remove all text (progress messages) from the import log.
function ClearImportLog()
{
    window.logArray = new Array();
    RebuildLogHtml();
}

// Rebuild the html content of the Import Log, located in the
// left panel of the web page.
function RebuildLogHtml()
{
    var html = '';
    for (var i = 0; i < window.logArray.length; ++i) {
        html = html + window.logArray[i] + '<br>\n';
    }

    var log = window.document.querySelector("#importLog");
    log.innerHTML = html;
}
