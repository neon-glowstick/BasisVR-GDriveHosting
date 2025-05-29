using System;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;

namespace NeonGlowstick.BasisVr.GDriveHosting
{
    internal static class AvatarUploader
    {
        private const string DriveApplicationName = "BasisGoogleDriveUploader";
        public static async void Upload(string oauthToken, string avatarName)
        {
            if (string.IsNullOrEmpty(oauthToken))
            {
                Debug.LogError("Cannot upload avatar, missing OAuth token");
                return;
            }

            var uploadedFileId = string.Empty;
            try
            {
                var assetBundleDirectory = Path.Combine(Environment.CurrentDirectory, "AssetBundles");
                if (!TryGetAvatarFile(assetBundleDirectory, out var filePath))
                {
                    Debug.LogError($"Could not find avatar to upload in: {assetBundleDirectory}");
                    return;
                }

                var cts = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
                var cancellationToken = cts.Token;
                await using var avatarFileStream = File.OpenRead(filePath);

                using var driveService = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleCredential.FromAccessToken(oauthToken),
                    ApplicationName = DriveApplicationName
                });

                var cancelled = EditorUtility.DisplayCancelableProgressBar("Upload", "Setting up drive directories", 0.3f);
                var directories = await driveService.GetOrCreateDirectories(cancellationToken);
                if(cancelled)
                    return;

                cancelled = EditorUtility.DisplayCancelableProgressBar("Upload", "Checking for avatar file on Drive", 0.5f);
                var existingFile = await driveService.GetExistingAvatarFile(directories, avatarName, cancellationToken);
                if(cancelled)
                    return;

                var uploadRequest = existingFile == null
                    ? driveService.CreateAvatarRequest(avatarFileStream, directories, avatarName)
                    : driveService.ReplaceAvatarRequest(avatarFileStream, existingFile);

                using var _ = new CancellableProgressBarForUpload(uploadRequest, cts);
                var response = await uploadRequest.UploadAsync(cancellationToken);
                if (response.Exception != null)
                    throw response.Exception;

                uploadedFileId = uploadRequest.ResponseBody.Id;
            }
            catch (Exception e)
            {
                Debug.LogError($"Upload failed: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (string.IsNullOrEmpty(uploadedFileId))
                return;

            Helpers.ShowSuccessDialog(avatarName, uploadedFileId);
        }

        private static bool TryGetAvatarFile(string directory, out string filePath)
        {
            filePath = null;
            if (!Directory.Exists(directory))
                return false;

            var files = Directory.GetFiles(directory);
            filePath = files.FirstOrDefault(IsAvatarFile);
            return !string.IsNullOrEmpty(filePath);

            bool IsAvatarFile(string value)
            {
                // Make some assumptions about how avatar files are built. At the time of writing:
                // - All platform variants are bundled into a single file. This file has the extension .BEE
                // - The file is assigned a random (Guid) name
                // - There can only be 1 avatar file present in the directory at a time
                return value.EndsWith(".BEE");
            }
        }
    }
}
