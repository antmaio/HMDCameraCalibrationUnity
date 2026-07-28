# HMD-Multiview Camera Rig Calibration in Unity

Unity application for the calibration of a Head-Mounted Display (HMD) against a multi-camera rig setup. 

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

## Usage
1. Scan the room with the room scan framework from by MRUK. 
2. Build the 


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