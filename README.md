# TBSF Nakama Client

## Overview
This Unity package contains the Nakama Client implementation for the Turn Based Strategy Framework. The Framework is a flexible tool for developing turn-based strategy games in Unity and is available in the [Unity Asset Store](http://u3d.as/mfd). Additionally, this package is designed to work with a custom Nakama server, which is detailed in the GitHub repository at [TBSF Nakama Server](https://github.com/mzetkowski/tbsf-nakama-server).

## Prerequisites
- Unity version 2020.3 or higher.
- Turn Based Strategy Framework version 3.0 or higher.
- Git version control system
- Nakama Unity SDK version 3.10.1 or higher.

## Installing Turn Based Strategy Framework
1. If you haven't already, purchase the Turn Based Strategy Framework from the [Unity Asset Store](http://u3d.as/mfd).
2. Import it into your Unity project following the Asset Store's instructions.

## Installing Nakama Unity SDK

This package requires the Nakama Unity SDK. Follow these steps to install the Nakama Unity SDK in your Unity project:

1. **Open Your Unity Project:**
   - Make sure you have your Unity project open and that it meets the minimum version requirements.

2. **Unity Package Manager (UPM):**
   - Go to `Window` > `Package Manager` in Unity.

3. **Add Package from Git URL:**
   - Click the '+' button at the top-left corner of the Package Manager window.
   - Select `Add package from git URL`.

4. **Enter the Git URL of the Nakama Unity SDK:**
   - Enter the Git URL for the Nakama Unity SDK:
     ```
     https://github.com/heroiclabs/nakama-unity.git?path=/Packages/Nakama#v3.10.1
     ```
5. **Install the Package:**
   - Click 'Add' to install the Nakama Unity SDK into your project.

6. **Verify Installation:**
   - After installation, the Nakama Unity SDK should appear in the list of packages in the Package Manager. Ensure that it's correctly installed and there are no errors.

## Installing TBSF Nakama Client

After setting up the Nakama Unity SDK, you can proceed with installing the TBSF Nakama Client:

1. **Navigate to Package Manager:**
   - In your Unity project, open the Package Manager from `Window` > `Package Manager`.

2. **Add Package from Git URL:**
   - Click the '+' button at the top-left corner of the Package Manager.
   - Select `Add package from git URL`.

3. **Enter the TBSF Nakama Client URL:**
   - Input the Git URL for the TBSF Nakama Client package:
     ```
     https://github.com/mzetkowski/tbsf-nakama-client.git?path=/Packages/com.crookedhead.tbsf.nakama
     ```
4. **Install the Package:**
   - Click 'Add' to install. This action will download and add the TBSF Nakama Client to your Unity project.

5. **Check for Successful Installation:**
   - Ensure that the TBSF Nakama Client appears in the Package Manager and that there are no errors.

## Using TBSF Nakama Client

The `TBSF Nakama Client` package includes a single script named `NakamaConnection`. This script is an implementation of the abstract `NetworkConnection` class found in the Turn Based Strategy Framework. The `NakamaConnection` class is specifically tailored to work with a custom Nakama server, which is also available on GitHub at [TBSF Nakama Server](https://github.com/mzetkowski/tbsf-nakama-server). Detailed instructions for deploying this server can be found in its repository.

Within the Turn Based Strategy Framework, there are several demo scenes located in the `Assets/Examples` folder. Each of these scenes has a multiplayer counterpart designed for network play. To utilize `NakamaConnection` in these scenes, follow these steps:

1. **Create a New GameObject:** In your desired scene, create a new GameObject.

2. **Attach NakamaConnection Script:** Attach the `NakamaConnection` script to this newly created GameObject.

3. **Configure Server Address:** Enter the address of your Nakama server in the `host` field of the `NakamaConnection` script.

4. **Assign CellGrid GameObject**: Assign the CellGrid GameObject from the scene to the `CellGrid` field of the `NakamaConnection` script.

5. **Set Up Network GUI:** Locate the `NetworkGUI` GameObject in your scene. Assign the GameObject with the `NakamaConnection` script to the `NetworkConnection` field in the `NetworkGUI`.

6. **Set Up CellGrid:** The multiplayer scene includes game end conditions for host or player disconnections. Configure these by assigning the `NakamaConnection` script to `NetworkConnection` fields on the `PlayerDisconnectedCondition` and `HostDisconnectedCondition` attached to the `CellGrid` GameObject.

This setup integrates the `NakamaConnection` with your chosen scene. For more in-depth details and guidelines, refer to the TBSF documentation that accompanies the Framework.

## Troubleshooting

In case you encounter issues during the installation or operation of the TBSF Nakama Client, this section provides guidance to resolve common problems.

1. **Missing Nakama SDK**
   - **Error Message:** `error CS0246: The type or namespace name 'Nakama' could not be found (are you missing a using directive or an assembly reference?)`
   - **Cause:** The Nakama Unity SDK is not installed in your Unity project.
   - **Solution:** To install the Nakama Unity SDK, follow the [installation instructions](https://github.com/mzetkowski/tbsf-nakama-client/edit/master/README.md#installing-nakama-unity-sdk) provided in this README.

2. **Missing Turn Based Strategy Framework (TBSF)**
   - **Error Message:** `error CS0246: The type or namespace name 'NetworkConnection' could not be found (are you missing a using directive or an assembly reference?)`
   - **Cause:** The Turn Based Strategy Framework is not imported into your project.
   - **Solution:** Ensure the TBSF is properly installed by following these [installation instructions](https://github.com/mzetkowski/tbsf-nakama-client/edit/master/README.md#installing-turn-based-strategy-framework).

3. **Incorrect Git Revision**
   - **Error Message:** `Cannot perform upm operation: Unable to add package [https://github.com/mzetkowski/tbsf-nakama-client.git?path=/Packages/tbsf-nakama-client#<branch-name>]: Could not clone [https://github.com/mzetkowski/tbsf-nakama-client.git]. Make sure [<branch-name>] is a valid branch name, tag, or full commit hash on the remote registry.`
   - **Cause:** The specified revision in the package URL is incorrect or does not exist.
   - **Solution:** Verify and provide a valid package URL. Replace `<branch-name>` with a valid branch name, tag, or full commit hash. A list of available branches and tags can be found on the main page of the repository.

4. **Missing Git version control system**
   - **Error Message:** `Error adding package: [https://github.com/mzetkowski/tbsf-nakama-client.git?path=/Packages/tbsf-nakama-client#<branch-name>]. Unable to add package [https://github.com/mzetkowski/tbsf-nakama-client.git?path=/Packages/tbsf-nakama-client#<branch-name>]: No 'git' executable was found. Please install Git on your system then restart Unity and Unity Hub`
   - **Cause:** Git version control system is not installed on your machine
   - **Solution:** Install [Git](https://git-scm.com/) and restart Unity

## Contact and Support
If you have any questions, feedback, or need assistance with the TBSF Nakama Client, feel free to reach out. You can contact me directly via email at crookedhead@outlook.com for specific queries or suggestions. Additionally, for broader community support and discussions, join the TBSF Discord server: [TBSF Discord](https://discord.gg/uBJNPJHFjB). This platform is ideal for connecting with other TBSF users, sharing experiences, and getting help from the community.

## Additional Notices

### Acknowledgments

This package uses the Nakama Unity SDK by Heroic Labs. Nakama is an open-source server for social and realtime games.

### Nakama Unity SDK

The Nakama Unity SDK is used under the terms of the Apache License 2.0. A copy of the Apache License 2.0 can be found in the `LICENSE-Nakama.md` file in this repository.

### Trademarks

This project is not endorsed by or affiliated with Heroic Labs. "Nakama" is a trademark of Heroic Labs and is used here for descriptive purposes only. This project's use of the "Nakama" name is not intended to imply any affiliation with or endorsement by Heroic Labs.
