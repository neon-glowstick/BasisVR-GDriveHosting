using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeonGlowstick.BasisVr.GDriveHosting
{
    [InitializeOnLoad]
    internal static class AvatarUploaderGui
    {
        private const string AuthUrl = "https://neon-glowstick.github.io/BasisVR-GDriveHosting";

        static AvatarUploaderGui()
        {
            BasisAvatarSDKInspector.InspectorGuiCreated += OnInSpectorGuiCreated;
        }

        private static void OnInSpectorGuiCreated(BasisAvatarSDKInspector inspector)
        {
            inspector.rootElement.Add(BuildGui(inspector));
        }

        private static VisualElement BuildGui(BasisAvatarSDKInspector inspector)
        {
            var container = new VisualElement();
            container.Add(new Label
            {
                text = "Google Drive uploader"
            });
            container.Add(new Button(() =>
            {
                Application.OpenURL(AuthUrl);
            })
            {
                text = "Open auth page"
            });

            var config = GoogleDriveConfig.Load();
            var accessTokenField = PasswordField("OAuth token", config.OAuthToken);
            accessTokenField.RegisterValueChangedCallback(value => config.OAuthToken = value.newValue);
            container.Add(accessTokenField);

            var uploadButton = new Button
            {
                text = "Upload to drive"
            };
            uploadButton.clicked += () =>
            {
                uploadButton.SetEnabled(false);
                GoogleDriveConfig.Save(config);
                var avatarName = inspector.Avatar.BasisBundleDescription.AssetBundleName;
                AvatarUploader.Upload(config.OAuthToken, avatarName);
                uploadButton.SetEnabled(true);
            };
            container.Add(uploadButton);

            var deleteConfigButton = new Button
            {
                text = "Delete stored token"
            };
            deleteConfigButton.clicked += GoogleDriveConfig.Delete;
            container.Add(deleteConfigButton);

            return container;
        }

        private static TextField PasswordField(string label, string value)
        {
            return new TextField(-1, false, true, '•')
            {
                label = label,
                value = value
            };
        }
    }
}
