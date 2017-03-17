# AI-ROCKS

Readme in progress...

This README has various sections:
- Building (and dependencies)
- Running
- Necessary installations
- References

## Building:

To build AI-ROCKS, all dependencies must have their `.dll`s in the top level of AI-ROCKS.
AI-ROCKS has the following dependencies: **ObstacleLibrary**, **LRFLibrary**, **EmguCV**, **AForge.NET**, and 
{other things from Vision - TODO}.

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

#### LRFLibrary (Windows):
LRFLibrary has ObstacleLibrary as a dependency itself. As a result, do the following for ObstacleLibrary:
```
Building ObstacleLibrary for LRFLibrary:
- Clone ObstacleLibrary into the top level of LRFLibrary
  (Note: required to clone into top level - cannot just copy .dlls)
- Complete the above build instructions for ObstacleLibrary (which is cloned in LRFLibrary top level)
- Copy the `ObstacleLibrary.dll` and `ObstacleLibraryNative.lib` files into the top level of LRFLibrary
```

Now, build LRFLibrary:
```
Building LRFLibrary:
- Open `build/windows/LRFLibrary.sln` in Vision Studio
- Ensure you are building in x64: Navigate to Build > Configuration Manager.
  Set `Active Solution Platform` to x64 and both `LRFLibrarySharp` and `LRFLibrary` projects to x64.
- Build solution, or if there are issues, `LRFLibrary` and then `LRFLibrarySharp` in that order
- Resultant `LRFLibrarySharp.dll` is in the `build/windows/x64/Debug` directory
- Copy `LRFLibrarySharp.dll` into top level of AI-ROCKS
```

#### EmguCV (Windows):
If EmguCV has not been installed, refer to the install guides for required Vision dependencies (below).
```
Add .dlls:
- Open your EmguCV install location in the file explorer
- Navigate to the `/bin` directory
- Find and copy the following .dlls to the top-level of AI-ROCKS:
	- `Emgu.CV.World.dll`
	- {TODO more to come?}
- (TODO below is for VisionGUI. Transfer to AI-ROCKS? Look for update)
Navigate to `/bin/x64:
- Copy all four .dlls (below) to the top level of AI-ROCKS:
	- `cvextern.dll`
	- `msvcp140.dll`
	- `opencv_ffmpeg310_64.dll`
	- `vcruntime140.dll`
```

#### AForge.NET (Windows):
If AForge.NET has not been installed, refer to the install guides for required Vision dependencies (below).
```
- Open your AForge.NET install location in the file explorer
- Navigate to `/Framework/Release` directory
- Find and copy the following .dlls to the top-level of AI-ROCKS:
	- `AForge.dll`
	- `AForge.Video.dll`
	- `AForge.Vision.dll`
```

#### Other (yet to come...)
TODO

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
- `-l <port>`		- COM or UDP port that the LRF is communicating over. 
COM ports are specfied like eg. `COM4` and UDP ports are specified by their number, eg `20001`.
	
	Note: COM ports are primarily used when the LRF is connected to the computer which is running AI-ROCKS (i.e. Ascent),
	while UDP ports are primarly used when the LRF data is coming from some other source over UDP (i.e. Gazebo).
	
	Note 2: This is the **only required command line argument!**
- `-d <state>`		- Initial `DriveState` to start AI-ROCKS in, according to `StateType` enum.
	
	Note: 0 = `GPSDriveState`, 1 = `VisionDriveState`, and 2 = `ObstacleAvoidanceDriveState`.
	The default `DriveState` is `GPSDriveState`.

- `-g <address>`	- IP address to communicate to ROCKS over. 
	
	The default is Loopback (127.0.0.1) as this is used
	for communicating to Ascent, so if no value is specified, Loopback is used. If using Gazebo (i.e. testing), 
	specify the IP address of the computer running Gazebo (i.e. the IP of the comupter acting as Ascent) in dot notation. 
	
	Example: `-g 192.168.1.80`.

## Necessary installations:

AI-ROCKS requires installing certain frameworks to resolve dependencies and build. The following gives brief explanations
of these frameworks and describes short summaries for install processes.

### OpenCV (Windows)
How To Install and Setup OpenCV For Python.

1. Install Python 2.7 (32 bit version)

2. Install OpenCV
  Follow this link: 
  https://sourceforge.net/projects/opencvlibrary/files/opencv-win/
  Or search for "OpenCV" on google and get to the sourceforge site.  Download "opencv-2.4.13.exe".
  After downloading, look in the opencv directory: opencv\build\python\2.7\x86
  Copy the file "cv2.pyd" into your "Python27\Lib\site-packages directory".
  
3. Install NumPy 
  Follow this link:
  https://sourceforge.net/projects/numpy/files/NumPy/
  Or search for "NumPy" on google and get to the sourcefore site.
  Download "numpy-1.9.2-win32-superpack-python2.7.exe".
  Run the installer by executing the downloaded binary.
  
4. Test installation
  At the python terminal, type the following commands:
  >>import cv2
  >>import numpy
  If these commands produce no output, the packages are successfully installed.

5. PIP Install
From cmd run: 
>python -m pip install -U pip setuptools

Add the folder C:\Python27\Scripts to your PATH (Environment variables)

From cmd run
>pip install imutils

### EmguCV (Windows) (still in progress):
EmguCV is the C# wrapper of OpenCV:
- Download: https://sourceforge.net/projects/emgucv/
Version we use: 3.1.0.2504.

- Install:
```
Install EmguCV:
- Download from above link
- Run `.exe` downloaded to install. Change destination directory if desired.
- {TODO more?}
```

### AForge.NET (Windows) (still in progress):
AForge.NET is {TODO}
- Download: http://www.aforgenet.com/framework/downloads.html
Click 'Download Installer'

- Install:
```
Install AForge.NET:
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
