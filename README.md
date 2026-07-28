# HMD-Multiview Camera Rig Calibration in Unity

Unity application for calibrating a Head-Mounted Display (HMD) against a multi-camera rig setup.

The method defines a fixed coordinate system, `H_f`, that bridges the virtual and physical worlds. It uses the room scan from Meta's Mixed Reality Utility Kit (MRUK) to identify a physical reference feature (e.g. a wall, floor, or table) visible in the passthrough camera view, and establishes `H_f` relative to it.

A snapshot of the checkerboard pattern used to calibrate the multi-view camera rig is then captured from the HMD's left camera view, along with its pose expressed in `H_f`.

Finally, we compute the transformation `T(H_f → W)` between the virtual frame `H_f` and the physical world frame `W`. The world frame `W` is defined separately via standard multi-view RGB camera calibration (e.g. checkerboard-based extrinsic calibration across the rig).

<!-- Add a screenshot or a GIF here-->
<!-- ![demo](docs/demo.gif) -->

## Requirements 
- **Hardware:** Tested on MetaQuest 3
- **Unity:** Tested on Unity 6000.0.42f1
- **Packages:**
    - Meta MR Utility Kit 201.0.0
    - Meta XR Core SDK 201.0.0
    - Inference Engine 2.2.2
    - Oculus XR Plugin 4.5.4
    - OpenXR Plugin 1.15.1
    - XR Interaction Toolkit 3.0.11
    - XR Plugin Managment 4.5.3

## Installation
1. Clone the below repository and follow their installation guidelines 
```bash
   git clone https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples.git
``` 
2. Clone the repository  
```bash 
     git clone https://github.com/antmaio/HMDCameraCalibrationUnity
```
3. Copy paste ./Start Calibration/ folder into ./Assets/PassthroughCameraApiSamples/

## Project Structure
After install, Unity project structure is supposed to look like:
```
Assets/
├── PassthroughCameraApiSamples/       
│   ├── PassthroughCamera/
│   │   ├── Scripts/ 
│   │   ├── Prefabs/
│   ├── Start Calibration/ 
│   │   ├── Scripts/ 
│   │   ├── Prefabs/
│   ├── Start Scene/
├── MetaXR/
├── Oculus/          
├── Plugins/
├── Resouces/
├── Samples/
├── StreamingAssets/
├── XR/
└──XRI/
Packages/
```


## Features
- **`SceneReferenceFrame`** — using MRUK's room scan, spawns one coordinate frame per selected surface (floor, ceiling, walls, table, couch), each with a deterministic orientation (world-up + longest edge/wall normal).
- **`AxisDrawerForGO`** — reusable debug visualizer; draws an RGB axis triad on any GameObject.
- **`PassthroughSnapshot`** - on a controller button press, it captures a passthrough image and the current head/camera/XR-camera poses, re-expresses them in the active reference frame (tracking space or each MRUK frame), and saves it all to a JSON + PNG pair.

```
MRUK scan ─► SceneReferenceFrame ─► per-surface frames
                    │
                    ▼
        PassthroughSnapshot (on button press)
        → captures image + poses, re-expressed i n every frame
        → saves JSON + PNG
```

## Usage
1. Scan the room with the room scan framework from by MRUK. Additionally, you can set up the guardian boundaries if you plan to use Guardian center-of-mass as $\mathcal{H}_f$.
2. Drag and drop  ```CalibrationRig``` in ```PassthroughCameraApiSamples/Start Calibration/Prefabs``` into the empty scene.
3. Build the solution into an Android application and run it in standalone mode on the VR device.


<!--> 
## Configuration
 
Calibration parameters can be adjusted in `Assets/Resources/CalibrationConfig.json`:
 
```json
{
  "numCaptures": 20,
  "targetType": "checkerboard",
  "cameraCount": 4,
  "exportFormat": "json"
}
```


## Roadmap

 
- [ ] Support for additional HMD SDKs
- [ ] Automatic outlier rejection during calibration
- [ ] Real-time calibration accuracy feedback
## Contributing
 
Contributions are welcome. Please open an issue to discuss major changes before submitting a pull request.
 
## License
 
This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
 
## Acknowledgments
 
- List any papers, libraries, or prior work you're building on here.
<-->
