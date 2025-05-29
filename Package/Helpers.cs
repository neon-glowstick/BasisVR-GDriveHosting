using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;
using UnityEditor;
using UnityEngine;
using File = Google.Apis.Drive.v3.Data.File;

namespace NeonGlowstick.BasisVr.GDriveHosting
{
    internal static class Helpers
    {
        #region Consts
        private const string BasisVrDirectory = "BasisVr";
        private const string ScenesDirectory = "Scenes";
        private const string AvatarsDirectory = "Avatars";
        private const string PropsDirectory = "Props";
        #endregion

        #region Extensions
        /// <summary>
        /// Get or create the directories used by the app.
        /// The directories are:
        /// - BasisVr
        /// - BasisVr/Scenes
        /// - BasisVr/Avatars
        /// - BasisVr/Props
        /// </summary>
        /// <param name="service"></param>
        /// <param name="cancellationToken"></param>
        public static async Task<DirectoryIds> GetOrCreateDirectories(this DriveService service, CancellationToken cancellationToken)
        {
            var directoryIds = await service.GetExistingDirectoryIds(cancellationToken);

            if (string.IsNullOrEmpty(directoryIds.BasisVr))
            {
                // Basis directory at root of drive with no sharing permissions
                var basisDirectory = await service.CreateDirectory(BasisVrDirectory, null, cancellationToken);
                directoryIds.BasisVr = basisDirectory.Id;
            }

            // Scenes, avatars, and props with public readonly permissions
            // Files in directories inherit permissions
            if (string.IsNullOrEmpty(directoryIds.Scenes))
            {
                var directory = await service.CreateDirectory(ScenesDirectory, directoryIds.BasisVr, cancellationToken);
                await service.SetToPublicReadOnly(directory.Id, cancellationToken);
                directoryIds.Scenes = directory.Id;
            }
            if (string.IsNullOrEmpty(directoryIds.Avatars))
            {
                var directory = await service.CreateDirectory(AvatarsDirectory, directoryIds.BasisVr, cancellationToken);
                await service.SetToPublicReadOnly(directory.Id, cancellationToken);
                directoryIds.Avatars = directory.Id;
            }
            if (string.IsNullOrEmpty(directoryIds.Props))
            {
                var directory = await service.CreateDirectory(PropsDirectory, directoryIds.BasisVr, cancellationToken);
                await service.SetToPublicReadOnly(directory.Id, cancellationToken);
                directoryIds.Props = directory.Id;
            }

            return directoryIds;
        }

        public static async Task<File> GetExistingAvatarFile(this DriveService service, DirectoryIds directoryIds, string avatarName, CancellationToken cancellationToken)
        {
            var fileName = avatarName + ".BEE";
            var escapedName = Uri.EscapeUriString(fileName);

            // Filter to match a file with the same name in the avatar directory. And exclude trashed items
            var filter = $"name = '{escapedName}' and '{directoryIds.Avatars}' in parents and mimeType != 'application/vnd.google-apps.folder' and trashed = false";

            await foreach (var file in EnumerateFiles(service, filter, cancellationToken))
            {
                // We'll take the first match, the user can deal with any duplicates themselves in the drive gui
                return file;
            }

            return null;
        }

        public static ResumableUpload<File, File> ReplaceAvatarRequest(this DriveService driveService, FileStream avatarFileStream, File fileMetaData)
        {
            // We're not modifying any of the metadata so keep the File blank
            var fieldsToModify = new File();
            return driveService.Files.Update(fieldsToModify, fileMetaData.Id, avatarFileStream, "application/octet-stream");
        }

        public static ResumableUpload<File, File> CreateAvatarRequest(this DriveService driveService, FileStream avatarFileStream, DirectoryIds directories, string avatarName)
        {
            var fileMetaData = new File
            {
                Name = avatarName + ".BEE",
                Parents = new List<string> { directories.Avatars },
            };
            var createRequest = driveService.Files.Create(fileMetaData, avatarFileStream, "application/octet-stream");
            createRequest.Fields = "id";
            return createRequest;
        }

        private static async Task<DirectoryIds> GetExistingDirectoryIds(this DriveService service, CancellationToken cancellationToken)
        {
            var directories = new DirectoryIds();
            // Directories in drive are just files with a special mimetype
            const string filter = "mimeType = 'application/vnd.google-apps.folder'";

            // todo: This is too naive, use the Q filter to actually look for the directories
            var found = 0;
            await foreach (var file in EnumerateFiles(service, filter, cancellationToken))
            {
                switch (file.Name)
                {
                    case BasisVrDirectory:
                        directories.BasisVr = file.Id;
                        found++;
                        break;
                    case ScenesDirectory:
                        directories.Scenes = file.Id;
                        found++;
                        break;
                    case AvatarsDirectory:
                        directories.Avatars = file.Id;
                        found++;
                        break;
                    case PropsDirectory:
                        directories.Props = file.Id;
                        found++;
                        break;
                }

                if (found >= 4)
                    break;
            }

            return directories;
        }

        private static async IAsyncEnumerable<File> EnumerateFiles(this DriveService service, string q, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var listRequest = service.Files.List();
            listRequest.Q = q;
            var response = await listRequest.ExecuteAsync(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var file in response.Files)
                {
                    yield return file;
                }

                // List requests are paginated. If we have a NextPageToken we need to make another request
                listRequest.PageToken = response.NextPageToken;
                if (string.IsNullOrEmpty(response.NextPageToken))
                    break;

                response = await listRequest.ExecuteAsync(cancellationToken);
            }
        }

        private static async Task<File> CreateDirectory(this DriveService service, string directoryName, string parentId, CancellationToken cancellationToken)
        {
            // Directories in drive are just files with a special mimetype
            var directory = new File
            {
                MimeType = "application/vnd.google-apps.folder",
                Name = directoryName,
                Parents = new List<string>
                {
                    parentId
                },
            };
            var request = service.Files.Create(directory);
            var response = await request.ExecuteAsync(cancellationToken);
            return response;
        }

        private static async Task SetToPublicReadOnly(this DriveService service, string directoryId, CancellationToken cancellationToken)
        {
            var publicReadOnly = new Permission
            {
                Role = "reader",
                Type = "anyone"
            };
            var request = service.Permissions.Create(publicReadOnly, directoryId);
            await request.ExecuteAsync(cancellationToken);
        }
        #endregion

        #region EditorUtility
        internal static void ShowSuccessDialog(string avatarName, string id)
        {
            var downloadUrl = "https://drive.google.com/uc?export=download&id=" + id;
            var copyToClipboard = EditorUtility.DisplayDialog("Upload complete", $"{avatarName} was uploaded to your google drive. Use this link to load the avatar: {downloadUrl}", "Copy link to clipboard", "Close");
            if (copyToClipboard)
            {
                EditorGUIUtility.systemCopyBuffer = downloadUrl;
            }
            Debug.Log($"Download url for {avatarName}: {downloadUrl}");
        }
        #endregion

        #region Types
        internal class DirectoryIds
        {
            public string BasisVr;
            public string Scenes;
            public string Avatars;
            public string Props;
        }
        #endregion
    }
}
