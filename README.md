# Phi
Phi is a Rimworld mod that enables online multiplayer interactions between players.

## Projects
### PhiClient
This is the mod for Rimworld. Rim-world specific files (like Defs) are located in PhiClient/Phi

PhiClient has multiples dependencies, and Rimworld Mod's Launcher unfortunately requires assemblies to be loaded in the order in which all dependencies are available.
A good way to ensure that is that change the name of the assemblies to make sure that the alphabetical order respects the dependency order. For example:

* 1-websocket-sharp.dll
* 2-SocketLibrary.dll
* 3-PhiData.dll
* PhiClient.dll

### PhiServer
This is the server program.

### PhiData
Contains the shared code between the server and the client. It is mainly the data structures that are synced between the clients and the server.
Used by PhiClient and PhiServer.

### SocketLibrary
A wrapper library around websocket-sharp.
Used by PhiClient and PhiServer.

## Building
Previously game data .dlls were included in the ExternPackages folder. They have since been removed from the repository, but are still required by the project.

To work around this, copy both Assembly-CSharp.dll and UnityEngine.dll from your Rimworld data folder into the ExternPackages folder. The project still references their location, so simply placing them in that folder should be enough.