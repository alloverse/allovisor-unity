# allovisor

Allovisor is the "user interface" into the Alloverse. Use this Unity app to connect to an Alloverse Place'
and interact with the apps available in that place.

## Compiling and developing Allovisor

1. Install Unity Hub, and from it, Install Unity 2018.3.8f1. Make sure to add-on iOS build support (needed to build the Mac app for inexplicable reasons).
2. Clone allovisor (this repo). Use Github Desktop, or somehow make sure that your git has LFS (Large File Support).
3. Open the project in Unity.
4. Check the Console. It should have no errors. If it does, file an issue here, ask [@nevyn on Twitter](https://twitter.com/nevyn), or [ping on Slack](https://join.slack.com/t/alloverse/shared_invite/enQtNTE3NTI3Mjc5NzUxLTBhNjExOTExOWZiZjAyYmFkOTNkMDBkMGE2MTlhMjU1NmJmZDVjOGRhNGVkMTRlZTJhODlkOTYyMmYzYTJkMzU).
5. From the "Build" menu, do "Download allonet assets". This will download native libraries from CI.
6. Open the "Menu" scene. This is the scene you must start from, because it configures the "Network" scene.
7. You should be able to connect to Nevyn's Place. If not, you can set up your own Alloplace and use an `alloplace://localhost` URL to connect to it.
