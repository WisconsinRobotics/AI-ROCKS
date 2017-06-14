# AI-ROCKS

This README has various sections:
- Overview
- Building (and dependencies)
- Running
- Necessary installations
- References

## Overview:
AI-ROCKS contains the autonomous navigation software designed for usage with ROCKS, Wisconsin Robotics' control software 
for Ascent. This code is intended for the Autonomous Traversal Task of the 2017 University Rover Challenge.

AI-ROCKS is structured using a state pattern design to model a state machine based on drive states. The two drive states 
of AI-ROCKS are GPS and Vision, with obstacle avoidance running underneath both of these drive states. Specifically:

- **GPS**: GPS handles long-range, broad navigation to get us "close" to the tennis ball, using GPS and IMU sensors. 
- **Vision**: Vision handles short-range, precise movements to detect the tennis ball and navigate to within 3 meters of 
it. Vision uses a camera (USB or IP) for vision and also uses the GPS and IMU sensors.
- **Obstacle Avoidance**: Obstacle avoidance runs in its own thread and notifies the current drive state if any obstacles
are detected. The LRF data is received from ROCKS-LRF, which is the small separate solution for reading and sending LRF 
data.

## Building:

To build AI-ROCKS, all dependencies must have their `.dll`s in the top level of AI-ROCKS.
AI-ROCKS has the following dependencies: **ObstacleLibrary** and **EmguCV**.

**Build everything in x64.**

### Building Dependencies
Refer to each library's README for more complete build details. Summaries of building dependencies are given here:

#### ObstacleLibrary:
```
Buildling ObstacleLibrary:
- Open `csharp/ObstacleLibrary.sln` in Visual Studio
- Ensure you are building in x64: Navigate to Build > Configuration Manager. 
  Set `Active Solution Platform` to x64 and both `ObstacleLibrary` and `ObstacleLibraryNative` projects to x64.
- Build solution, or if there are issues, `ObstacleLibraryNative` and then `ObstacleLibrary` in that order
- Resultant `ObstacleLibrary.dll` and `ObstacleLibraryNative.lib` are in the `csharp/x64/Debug` directory
- Copy `ObstacleLibrary.dll` into top level of AI-ROCKS
```

#### EmguCV (Windows):
If EmguCV has not been installed, refer to the install guides for required Vision dependencies (below).
```
Add .dlls:
- Open your EmguCV install location in the file explorer
- Navigate to the `/bin` directory
- Find and copy the following .dlls to the top-level of AI-ROCKS:
	- `Emgu.CV.World.dll`
- Navigate to `/bin/x64`:
- Copy all four of the following .dlls to the `AI-ROCKS/AI-ROCKS/` project directory (not the top-level 
directory):
	- `cvextern.dll`
	- `msvcp140.dll`
	- `opencv_ffmpeg310_64.dll`
	- `vcruntime140.dll`
```

### Building AI-ROCKS
Once all dependencies are in the top level of AI-ROCKS (above), build AI-ROCKS:
```
Building AI-ROCKS:
- Open `AI-ROCKS.sln`
- Ensure you are building in x64: Navigate to Build > Configuration Manager.
  Set `Active Solution Platform` to x64 and the `AI-ROCKS` project to x64.
- Build solution
```

## Running:
### Running AI-ROCKS
AI-ROCKS is currently run from Visual Studio in Windows. To run, do the following:
```
If doing obstacle detection (via LRF or Gazebo):
- Run ROCKS-LRF before running AI-ROCKS. Rever to ROCKS-LRF README. This will allow you to receive LRF data 
over UDP.
- You should see 'Waiting for handshake' in a console window. This will wait until ROCKS-LRF and AI-ROCKS have
completed a handshake before sending LRF data.
- Run AI-ROCKS (below). 
- Once AI-ROCKS is running, ROCKS-LRF should give output that the handshake has succeeded. Let ROCKS-LRF run
in background.

Running AI-ROCKS:
- Open `AI-ROCKS.sln` in Visual Studio (VS).
- Ensure AI-ROCKS builds. Refer to 'Building' for details.
- Navigate to Solution Explorer in VS. Right click on AI-ROCKS project > Properties.
- Navigate to 'Debug' tab. Under 'Start options', specify required command line args. These are specified below.
- Hit 'Start' and AI-ROCKS should run.
```

### Command line arguments
The only required command line arg is for the LRF port, or `-l <port>` as shown below. All possible command line arguments 
to AI-ROCKS are as follows:
- `-l <port>`		- UDP port that AI-ROCKS communicates with ROCKS-LRF over. Default is 11000 and does not need to be 
specified.
	
- `-d <state>`		- Initial `DriveState` to start AI-ROCKS in, according to `StateType` enum.
	
	Note: 0 = `GPSDriveState`, 1 = `VisionDriveState`, and 2 = `ObstacleAvoidanceDriveState`.
	The default `DriveState` is `GPSDriveState`.

- `-g <address>`	- IP address to communicate to ROCKS over. 
	
	The default is loopback (127.0.0.1) as this is used	for communicating with Ascent. If no value is specified, loopback 
	is used. If running AI-ROCKS remotely or using Gazebo for testing, specify the IP address of the computer running 
	ROCKS (i.e. the robot) or Gazebo in dot notation. 
	
	Example: `-g 192.168.1.80`.

- `-lat`			- Latitude of the GPS coordinates of the gate, in Degrees, Minutes, Seconds format.
	
	Note: the list is specified in `degrees` `minutes` `seconds`, separated by spaces. For latitude/longitude to be 
	specified as a param, both latitude and longitude must be specified (by `-lat` and `-long`)
	
	Example: `-lat 43 4 17.9`

- `-long`			- Longitude of the GPS coordinates of the gate, in Degrees, Minutes, Seconds format.
	
	Note: the list is specified in `degrees` `minutes` `seconds`, separated by spaces. For latitude/longitude to be 
	specified as a param, both latitude and longitude must be specified (by `-lat` and `-long`)
	
	Example: `-long -89 24 41.1`

- `-nogate`			- Test mode for GPS gate coordinates. If specified, do not use any gate GPS coordinates (from 
parameters via `-lat` or `-long`, or wait to receive from the base station).
	
	Note: this initializes the gate as having both latitude and longitude as 0,0,0.
	
- `-t`				- Test mode for LRF data. This flag will not try a handshake with ROCKS-LRF and no obstacle avoidance
code will run.

## Necessary installations:

AI-ROCKS requires installing certain frameworks to resolve dependencies and build. The following gives brief explanations
of these frameworks and describes short summaries for install processes.

### OpenCV (Windows) (still in progress)
How To Install and Setup OpenCV For Python.

1. Install Python 2.7 (32 bit version)

2. Install OpenCV
	Follow this link: 
	https://sourceforge.net/projects/opencvlibrary/files/opencv-win/.
	
	Or search for "OpenCV" on Google and get to the sourceforge site.
	
	Download `opencv-2.4.13.exe`.
	
	After downloading, look in the OpenCV directory: `opencv/build/python/2.7/x86`.
	
	Copy the file `cv2.pyd` into your `Python27/Lib/site-packages` directory.
	
3. Install NumPy 
	Follow this link:
	https://sourceforge.net/projects/numpy/files/NumPy/.
	
	Or search for "NumPy" on google and get to the sourcefore site.
	
	Download `numpy-1.9.2-win32-superpack-python2.7.exe`.
	
	Run the installer by executing the downloaded binary.
	
4. Test installation
	At the python terminal, type the following commands:
	```
	>>import cv2
	>>import numpy
	```
	If these commands produce no output, the packages are successfully installed.
	
5. PIP Install
	From cmd run: 
	```
	>python -m pip install -U pip setuptools
	```
	
	Add the folder `C:\Python27\Scripts` (or whereever else you installed Python) to your PATH (Environment variables)
	
	From cmd run
	```
	>pip install imutils
	```

### EmguCV (Windows) (still in progress):
EmguCV is the C# wrapper of OpenCV:
Download: https://sourceforge.net/projects/emgucv/.

Version we use: 3.1.0.2504.

Install:
```
Install EmguCV:
- Download from above link
- Run `.exe` downloaded to install. Change destination directory if desired.
- {TODO more?}
```
	
## References:
(Pretty much just a dump of useful resources for now)

Follow the Gap obstacle avoidance method: 
- Here is a reference: https://pdfs.semanticscholar.org/c432/3017af7bce46fc7574ada008b8af1011e614.pdf
- Here is a YouTube video: https://www.youtube.com/watch?v=TohW9xokbaM

EmguCV:
- Follow this for installing EmguCV: http://www.emgu.com/wiki/index.php/Setting_up_EMGU_C_Sharp
(May be out of date for our current version though)
- The primary resource for everything EmguCV: http://www.emgu.com/wiki/index.php/Main_Page
- Good for searching specific OpenCV functions: http://docs.opencv.org/2.4/index.html
- This site has some good tutorials: 
http://www.pyimagesearch.com/2015/01/19/find-distance-camera-objectmarker-using-python-opencv/
