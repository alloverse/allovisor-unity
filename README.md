# allovisor

Allovisor is the "user interface" into the Alloverse. Use this Unity app to connect to an Alloverse Place'
and interact with the apps available in that place.

## Compiling and developing Allovisor

1. Install Unity Hub, and from it, Install Unity 2019.2.2f1. Make sure to add-on iOS build support (needed to build the Mac app for inexplicable reasons) and IL2CPP.
2. Clone allovisor (this repo). Use Github Desktop, or somehow make sure that your git has LFS (Large File Support).
3. Open the project in Unity.
4. Check the Console. It should have no errors. If it does, file an issue here, ask [@nevyn on Twitter](https://twitter.com/nevyn), or [ping on Slack](https://join.slack.com/t/alloverse/shared_invite/enQtNTE3NTI3Mjc5NzUxLTBhNjExOTExOWZiZjAyYmFkOTNkMDBkMGE2MTlhMjU1NmJmZDVjOGRhNGVkMTRlZTJhODlkOTYyMmYzYTJkMzU).
5. From the "Build" menu, do "Download allonet assets". This will download native libraries from CI.
6. Open the "Menu" scene. This is the scene you must start from, because it configures the "Network" scene.
7. You should be able to connect to Nevyn's Place. If not, you can set up your own Alloplace and use an `alloplace://localhost` URL to connect to it.

## Debugging in Windows

For some reason, Windows triggers a lot more heap misuses than MacOS. When triggered, this causes
the Unity editor (or the visor, if built), to crash completely with no error message. This is a
great opportunity to debug and fix a problem in `allonet`.

It takes a bit of work to configure a debugger, though. Here's how:

1. In Unity, build a standalone app. Check "create visual studio solution",
   "development build", "script debugging", and "wait for managed debugger".
2. In the destination folder, open the solution in Visual Studio 2019.
3. Follow [this guide](https://docs.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=vs-2019) to add Alloverse Azure Devops organization
   to your symbol file location, if you're in the azure devops org. If not,
   keep going.
3. Press "Local Windows Debugger" in the toolbar.
4. Crash the app.
5. Now you're gonna have to load debug symbols. CI has them generated. Check
   your Assets/allonet/allonet.cache file in Notepad to find the build number.
6. Head over to [Azure Pipelines for the build](https://dev.azure.com/alloverse/allonet/_build/results?buildId=73)
   (link to build # 73, change url to match your allonet.cache).
7. From the assets, download allonet.pdb. Rename it liballonet.pdb, I think.
8. In the guide linked above, configure "symbol file locations" to include
   your downloads folder.
9. Tada. Symbols.

To debug loading of symbols, check Windows > Modules and find liballonet.dll in the list.
