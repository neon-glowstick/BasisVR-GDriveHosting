using System;
using System.Threading;
using Google.Apis.Upload;
using UnityEditor;

namespace NeonGlowstick.BasisVr.GDriveHosting
{
    public class CancellableProgressBarForUpload : IDisposable
    {
        private readonly long _bytesToSend;
        private readonly CancellationTokenSource _cts;
        private readonly ResumableUpload _request;
        private long _uploadedBytes;

        public CancellableProgressBarForUpload(ResumableUpload request, CancellationTokenSource cts)
        {
            _bytesToSend = request.ContentStream.Length;
            if (_bytesToSend == 0 || double.IsNaN(_bytesToSend))
            {
                throw new ArgumentException("Upload request has no content", nameof(request));
            }

            _cts = cts;
            _request = request;
            _request.ProgressChanged += OnProgressChanged;
            EditorApplication.update += Update;
        }

        private void Update()
        {
            if (_cts.IsCancellationRequested)
                return;

            var info = $"Uploading {PrettyPrintByteCount(_uploadedBytes)}/{PrettyPrintByteCount(_bytesToSend)} to drive";
            var cancelled = EditorUtility.DisplayCancelableProgressBar("Uploading", info, (float)_uploadedBytes / _bytesToSend);
            if (cancelled && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        private void OnProgressChanged(IUploadProgress progress)
        {
            _uploadedBytes = progress.BytesSent;
        }

        public void Dispose()
        {
            _request.ProgressChanged -= OnProgressChanged;
            EditorApplication.update -= Update;
            EditorUtility.ClearProgressBar();
        }

        private static string PrettyPrintByteCount(double bytes)
        {
            var postfixes = new[] { "bytes", "KB", "MB", "GB" };
            var i = 0;
            while (Math.Round(bytes / 1024) >= 1)
            {
                bytes /= 1024;
                i++;
            }

            i = i < postfixes.Length ? i : 0;
            return $"{bytes}{postfixes[i]}";
        }
    }
}
