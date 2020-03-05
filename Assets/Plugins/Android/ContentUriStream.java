package com.awesomesaucelabs.piglet;

import android.annotation.TargetApi;
import android.content.ContentResolver;
import android.database.Cursor;
import android.net.Uri;
import android.provider.OpenableColumns;

import com.unity3d.player.UnityPlayer;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;

/**
 * A Unity java plugin for reading data from Android
 * content URIs.
 *
 * Content URIs are frequently used in lieu of file
 * paths/URIs since Android 7.0, but as of Unity 2018.3,
 * Unity does not provide any facilities for reading
 * them.  For example, content URIs are not readable
 * by UnityWebRequest.
 */
@TargetApi(26)
public class ContentUriStream
{
    /** Max bytes per read per operation */
    private final int BLOCK_SIZE = 4096;

    /** The content URI used to construct this object */
    private Uri _uri;

    /** Buffer used to hold bytes for a single read operation */
    private byte[] _readBuffer;

    /**
     * Buffer with size that exactly matches the number of bytes read
     * in the last read operation.
     */
    private byte[] _resultBuffer;

    /**
     * Stream for reading bytes from the content URI.
     */
    private InputStream _stream;

    /**
     * Constructor that sets the content URI for this
     * object and opens an input stream to read it.
     */
    public ContentUriStream(String uri) throws FileNotFoundException
    {
        _uri = Uri.parse(uri);
        _readBuffer = new byte[BLOCK_SIZE];

        _stream = UnityPlayer.currentActivity
                .getContentResolver()
                .openInputStream(_uri);
    }

    /** Get the size in bytes of this object's content URI. */
    public long getSize()
    {
        ContentResolver resolver = UnityPlayer.currentActivity.getContentResolver();
        Cursor cursor = resolver.query(_uri,null, null, null);
        int columnIndex = cursor.getColumnIndex(OpenableColumns.SIZE);
        cursor.moveToFirst();
        return cursor.getLong(columnIndex);
    }

    /**
     * Read up to BLOCK_SIZE (4096) bytes from this object's
     * content URI.
     *
     * @return byte[] containing bytes read, or null for EOF
     */
    public byte[] read() throws IOException
    {
        int bytesRead = _stream.read(_readBuffer, 0, BLOCK_SIZE);
        if (bytesRead < 0)
            return null;

        _resultBuffer = new byte[bytesRead];
        System.arraycopy(_readBuffer, 0, _resultBuffer, 0, bytesRead);

        return _resultBuffer;
    }
}
