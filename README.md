# Google Drive uploader for BasisVr avatars

This Unity package lets you upload a `.BEE` file from the Unity editor to your Google Drive.

`.BEE` files are used with applications built on the [BasisVr](https://github.com/BasisVR/Basis) framework.

## Installing

To install the package either:

1. Clone/download the repo and copy the Plugins folder into your Assets folder.
2. Use the package manager and select "Install package from git URL..." and paste this url `https://github.com/neon-glowstick/BasisVR-GDriveHosting.git?path=Plugins/BasisVrGDriveUploader`

## Uploading avatars

The package adds some new UI to the Basis Avatar component.

Step-by-step:

1. Follow the [guide for creating an avatar](https://docs.basisvr.org/docs/avatars) to build your assetbundle.
2. When the bundle is built scroll down in the inspector for the Basis Avatar component and click the "Open auth page" button to open the [authorization redirect landing page](https://neon-glowstick.github.io/BasisVR-GDriveHosting/).
3. Click the `Authorize` button and it should redirect you to the Google consent page.
   - The consent page should only ask for the [File](https://developers.google.com/workspace/drive/api/guides/api-specific-auth#drive-scopes) scope. This limits the application to only interact with files it has created.
5. When you are done with the consent page you will be redirected back to the [authorization redirect landing page](https://neon-glowstick.github.io/BasisVR-GDriveHosting/). A new button should have appeared. Click it to copy the OAuth token from your address bar to your clipboard.
6. Go back to the Unity editor and paste the token into the OAuth token password field.
7. Click the upload button. Your .BEE file will be uploaded to `BasisVr/Avatars/<YourAvatarName>.BEE`.
8. When upload is complete the URL for the .BEE file is printed to the dialogue and console. And there will be a button to copy the URL to your clipboard.

The OAuth token is stored locally in a config file when you click Upload and loaded automatically so you don't have to go through all these steps every time. But the token is only valid for an hour.

A BasisVr folder is created in your drive, and an Avatars folder inside of the BasisVr folder. The Avatars folder is made public and any file inside of it can be downloaded by anyone with the link to it.

> [!CAUTION]
> Never upload the dontuploadmepassword.txt file to the Avatars folder

### Props and scenes

Uploading props and scenes is not implemented yet.

## Dependencies

I'm using the [Google Drive sdk](https://www.nuget.org/packages/Google.Apis.Drive.v3/) imported via [NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity). The .dlls are not auto-included and the uploader assemblydefinition is editor only.

The authorization redirect landing page is hosted in github pages and uses plain inline javascript.
