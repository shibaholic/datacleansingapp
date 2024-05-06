using dc_app.ServiceLibrary.Entities;
using dc_app.ServiceLibrary.ServiceLayer;
using DocumentFormat.OpenXml.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dc_app.ServiceLibrary.Utilities;

public class UploadStream: Stream
{
    private readonly Stream _innerStream;
    private readonly long _contentLength;
    private readonly ISpreadsheetCreateDeleteService _createService;
    private UploadStatusEntity _uploadStatus;
    private long _bytesReadTotal;

    public UploadStream(Stream innerStream, long contentLength, ISpreadsheetCreateDeleteService createService, UploadStatusEntity uploadStatus)
    {
        _innerStream = innerStream;
        _contentLength = contentLength;
        _createService = createService;
        _uploadStatus = uploadStatus;
        _uploadStatus.status = "uploading";
        _bytesReadTotal = 0;
    }
    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = _innerStream.Read(buffer, offset, count);
        _bytesReadTotal += bytesRead;
        Console.WriteLine($"Bytes read: {_bytesReadTotal} / {_contentLength}");
        return bytesRead;
    }


    private DateTime _lastExecutionTime = DateTime.MinValue;
    private int _counter = 0;
    public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) 
    {
        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count);
        _bytesReadTotal += bytesRead;
        int percent = (int) (((double)_bytesReadTotal / _contentLength) * 100);
        // Console.WriteLine($"Bytes readAsync: {_bytesRead} / {_contentLength} {percent}%");

        DateTime now = DateTime.Now;
        if(now - _lastExecutionTime >= TimeSpan.FromSeconds(2))
        {
            _lastExecutionTime = now;
            Console.WriteLine($"Bytes readAsync: {_bytesReadTotal} / {_contentLength} {percent}%");
            _uploadStatus.value_percent = percent;
            Console.WriteLine("Updating UploadStatusRepo " + ++_counter + " percent: " + _uploadStatus.value_percent + "%" + " totalRead: " + _bytesReadTotal);
            
            await _createService.UpdateUploadStatus(_uploadStatus);
        }
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

}