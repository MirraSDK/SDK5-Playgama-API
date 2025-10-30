1. Delete existing `WebGLTemplates`, `PlayGamaBridge` folder in `Assets` folder if it exists, and delete file `PlaygamaBridge.jslib` in `Assets/Plugins` folder if it exists.

2. Setup the PlayGama SDK for Unity using official documentation steps:
https://wiki.playgama.com/playgama/sdk/engines/unity/setup

3. Create assembly definition file called exactly `PlaygamaBridge` inside the folder `Assets\PlaygamaBridge\*`.

4. Add this package in `Unity Package Manager` using the following URL:

```
https://github.com/MirraSDK/SDK5-Playgama-API.git
```

5. Select `PlaygamaConfiguration` configuration in `MirraSDK Toolkit` build configuration dropdown.