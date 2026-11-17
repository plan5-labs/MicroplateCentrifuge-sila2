# Microplate Centrifuge Controller

A C# SiLA Server for controlling the Microplate Centrifuge made by Agilent Technologies.

## About

### System Architecture

This server application is composed of the following two separate applications:

* MicroplateCentrifuge.Server: A SiLA Server application implemented in compliance with the SiLA2 Standard  
* CommandExecutor: An application that sends commands to the instrument 

These applications communicate via gRPC. To control the Microplate Centrifuge, start both applications before sending any request.  

### Features

This server allows you to control the Microplate Centrifuge using the following features.

* Commands
  * OpenDoor
  * CloseDoor
  * SpinCycle
  * StopSpinCycle
  * Reset
  * EnumerateProfiles
  * Home
  * Park
  * LoadPlate
  * UnloadPlate
* Properties
  * ActiveXVersion
  * CentrifugeActiveXVersion
  * CentrifugeHardwareVersion
  * FirmwareVersion
  * HardwareVersion
  * Status

## Command Details

### OpenDoor

Open the door to specified bucket.  
If you call this command while another observable command is running, the request will be rejected.

#### Parameters

| Identifier | Type | Description |
| -- | -- | -- |
| BucketNumber | Integer | The bucket number to be presented.<br>The specified value must be 1 or 2. |

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Running
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### CloseDoor

Close the door.  
If you call this command while another observable command is running, the request will be rejected.

#### Parameters

None.

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Running
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### SpinCycle

Run a spin cycle with or without the Centrifuge Loader. 
- When Using the Loader:
  - Set BucketNumberToLoad and BucketNumberToUnload to 1 or 2.
- When Not Using the Loader:
  - Set BucketNumberToLoad to 0
  - Set BucketNumberToUnload to 1 or 2, based on the desired bucket to present after the spin completes. In this case, the following parameters are ignored:
      - GripperOffsetToLoad
      - GripperOffsetToUnload
      - PlateHeightToLoad
      - PlateHeightToUnload
      - SpeedToLoad
      - SpeedToUnload
      - OptionsToLoad
      - OptionsToUnload

If you call this command while another observable command is running, the request will be rejected.

#### Parameters

| Identifier | Type | Description |
| -- | -- | -- |
| VelocityPercent | Real | Percentage of maximum velocity for spinning the Microplate Centrifuge.<br>The specified value must be in the range 1–100. |
| AccelerationPercent | Real | Percentage of maximum acceleration for spinning the Microplate Centrifuge.<br>The specified value must be in the range 1–100. |
| DecelerationPercent | Real | Percentage of maximum deceleration for spinning the Microplate Centrifuge.<br>The specified value must be in the range 1–100. |
| TimerMode | Integer | Specify how the value entered in the Time parameter should be used for spinning.<br>0: total time (including acceleration and deceleration time)<br>1: duration of spinning at the target speed (excluding acceleration and deceleration time) |
| Time | Integer | Number of seconds to spin.<br>The specified value must be in the range 1–86400. |
| BucketNumberToLoad | Integer | The number of the Microplate Centrifuge bucket to load with a plate.<br>If you set 0, loading/unloading is not executed, just spin the Microplate Centrifuge.<br>The specified value must be in the range 0-2. |
| BucketNumberToUnload | Integer | The number of the Microplate Centrifuge bucket to unload a plate.<br>If you set 0 to BucketNumberToLoad (= no bucket is loaded), BucketNumberToUnload indicates which bucket to present after the spin.<br>The specified value must be in the range 0-2. |
| GripperOffsetToLoad | Real | Specifies the distance (mm) from the plate bottom to the gripping point of the gripper center during loading. The default gripper offset is 8 mm.<br>The specified value must be in the range 2.6-21.1. |
| GripperOffsetToUnload | Real | Specifies the distance (mm) from the plate bottom to the gripping point of the gripper center during unloading. The default gripper offset is 8 mm.<br>The specified value must be in the range 2.6-21.1. |
| PlateHeightToLoad | Real | The height of the plate to load. The default plate height is 15 mm.<br>The specified value must be in the range 2.6-48.3. |
| PlateHeightToUnload | Real | The height of the plate to unload. The default plate height is 15 mm.<br>The specified value must be in the range 2.6-48.3. |
| SpeedToLoad | Integer | The speed at which the Centrifuge Loader should move during the loading process.<br>0: slow<br>1: medium<br>2: fast |
| SpeedToUnload | Integer | The speed at which the Centrifuge Loader should move during the unloading process.<br>0: slow<br>1: medium<br>2: fast |
| OptionsToLoad | Integer | Options for loading plate.<br><br>0: No options set<br>1: Ignore plate sensor<br>2: Grip plates gently<br>3: Ignore plate sensor and grip plates gently<br>4: Assume maximum plate height<br>5: Assume maximum plate height and Ignore plate sensor<br>6: Assume maximum plate height and Grip plates gently<br>7: Use all options<br><br>The options should be applied during the loading process.<br>If the assume maximum plate height option is used, the PlateHeightToLoad parameter is ignored. |
| OptionsToUnload | Integer | Options for unloading plate.<br><br>0: No options set<br>1: Ignore plate sensor<br>2: Grip plates gently<br>3: Ignore plate sensor and grip plates gently<br>4: Assume maximum plate height<br>5: Assume maximum plate height and Ignore plate sensor<br>6: Assume maximum plate height and Grip plates gently<br>7: Use all options<br><br>The options should be applied during the unloading process.<br>If the assume maximum plate height option is used, the PlateHeightToUnload parameter is ignored. |

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Running
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error
  
### StopSpinCycle

Terminate the spin cycle currently in progress and open the door to specified bucket.  
The following notes apply to this command:
* The loader does not unload the plate after the door opens, so it is necessary to call the UnloadPlate command.
* After this command is called, the instrument status immediately becomes Idle, but the spin continues for a while. There is no command or property to confirm whether the stop process has actually completed. If you want to detect whether the spin has fully stopped, verify the instrument's physical state.

#### Parameters

| Identifier | Type | Description |
| -- | -- | -- |
| BucketNumber | Integer | The bucket number to be presented.<br>The specified value must be 1 or 2. |

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Same as the status at the start of execution
* Status after this command finishes
  * If it completes normally: Same as the status at the start of execution
  * If an error occurs: Error

### Reset

Reset the server to its Idle state. This command is common to all instruments.  
If you call this command while another observable command is running, the request will be rejected.

#### Parameters

None.

#### Response

None.

#### Status condition

* Status required to execute this command: Error, Idle
* Status while this command is running: Same as the status at the start of execution
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### EnumerateProfiles

Enumerate profiles for Microplate Centrifuge.

#### Parameters

None.

#### Response

| Identifier | Type | Description |
| -- | -- | -- |
| ProfileNames | String List | The names of the profiles. |

#### Status condition

* Status required to execute this command: Idle, Running
* Status while this command is running: Same as the status at the start of execution
* Status after this command finishes: Same as the status at the start of execution

### Home

Move the Centrifuge Loader to home position.  
If you call this command while another observable command is running, the request will be rejected.

#### Parameters

None.

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Running
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### Park

Move the Centrifuge Loader to park position.  
If you call this command while another observable command is running, the request will be rejected.

#### Parameters

None.

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Running
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### LoadPlate

Load a plate into the Microplate Centrifuge.  
If you call this command while another observable command is running, the request will be rejected.

#### Parameters

| Identifier | Type | Description |
| -- | -- | -- |
| BucketNumber | Integer | The number of the Microplate Centrifuge bucket to load a plate.<br>The specified value must be 1 or 2. |
| GripperOffset | Real | Specifies the distance (mm) from the plate bottom to the gripping point of the gripper center during loading. The default gripper offset is 8 mm.<br>The specified value must be in range 2.6-21.1. |
| PlateHeight | Real | The height of the plate to load. The default plate height is 15 mm.<br>The specified value must be in range 2.6-48.3. |
| Speed | Integer | The speed at which the Centrifuge Loader should move during the loading process.<br>0: slow<br>1: medium<br>2: fast |
| Options | Integer | Options for loading plate.<br><br>0: No options set<br>1: Ignore plate sensor<br>2: Grip plates gently<br>3: Ignore plate sensor and grip plates gently<br>4: Assume maximum plate height<br>5: Assume maximum plate height and Ignore plate sensor<br>6: Assume maximum plate height and Grip plates gently<br>7: Use all options<br><br>The options should be applied during the loading process.<br>If the assume maximum plate height option is used, the PlateHeight parameter is ignored. |

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Running
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### UnloadPlate

Unload a plate into the Microplate Centrifuge.  
If you call this command while another observable command is running, the request will be rejected.

#### Parameters

| Identifier | Type | Description |
| -- | -- | -- |
| BucketNumber | Integer | The number of the Microplate Centrifuge bucket to unload a plate.<br>The specified value must be 1 or 2. |
| GripperOffset | Real | Specifies the distance (mm) from the plate bottom to the gripping point of the gripper center during unloading. The default gripper offset is 8 mm.<br>The specified value must be in range 2.6-21.1. |
| PlateHeight | Real | The height of the plate to unload. The default plate height is 15 mm.<br>The specified value must be in range 2.6-48.3. |
| Speed | Integer | The speed at which the Centrifuge Loader should move during the unloading process.<br>0: slow<br>1: medium<br>2: fast |
| Options | Integer | Options for unloading plate.<br><br>0: No options set<br>1: Ignore plate sensor<br>2: Grip plates gently<br>3: Ignore plate sensor and grip plates gently<br>4: Assume maximum plate height<br>5: Assume maximum plate height and Ignore plate sensor<br>6: Assume maximum plate height and Grip plates gently<br>7: Use all options<br><br>The options should be applied during the unloading process.<br>If the assume maximum plate height option is used, the PlateHeight parameter is ignored. |

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Running
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

## Property Details

### ActiveXVersion

The version number of the Centrifuge Loader's ActiveX Control.  
Status required to get this property: Idle, Running

### CentrifugeActiveXVersion

The version number of the Microplate Centrifuge's ActiveX Control.   
Status required to get this property: Idle, Running

### CentrifugeHardwareVersion

The version number of the Microplate Centrifuge's hardware.  
Status required to get this property: Idle, Running

### FirmwareVersion

The version number of the Centrifuge Loader's firmware.  
Status required to get this property: Idle, Running

### HardwareVersion

The version number of the Centrifuge Loader's hardware.  
Status required to get this property: Idle, Running

### Status

Returns the status of the instrument. This property is common to all instruments.  

* 0: Not Connected  
* 1: Idle  
* 2: Running  
* 3: Error  

## Getting Started

### Cabling

Use a USB-to-serial (RS-232) conversion cable to connect the PC running this server to the Microplate Centrifuge.  
Note: Both the main unit of the Microplate Centrifuge and its Loader must be connected to the computer.
### Prerequisites

* .NET 10 (for SiLA Server)
* .NET Framework 4.8 (for CommandExecutor)
* VWorks ActiveX Control (for CommandExecutor)
  * version: 13.1.9.1493 or later

### Usage

#### On the first launch (Configure Microplate Centrifuge Profile and Settings File)

1. Double-click MicroplateCentrifugeCommandExecutor.exe. After the Command Executor is launched, a light-purple square icon appears in the task tray.  
Note: If a confirmation dialog appears, click the **Yes** button to continue launching the Command Executor.
2. Right-click the icon. A context menu appears.
3. Click the **Settings** menu in the context menu. A settings window opens.
4. Click **Centrifuge diagnostics** button. A centrifuge diagnostics window opens.
5. Click **Create a new profile** button. A child window opens.
6. Enter a profile name of your choice for the main unit of the Microplate Centrifuge and click the **OK** button. The child window closes.  
7. Configure each setting as needed and click the **OK** button. The centrifuge diagnostics window closes.  
**Warning:** Follow the procedure mentioned on the user guide for the Microplate Centrifuge when teaching the bucket position. Mishandling the instrument may cause injury.
8. Click **Create a new profile** button. A child window opens.
9. Enter a profile name of your choice for the loader and click the **OK** button. The child window closes.  
10. Configure each setting as needed and click the **OK** button. The settings window closes.  
11. Quit the Command Executor. The detailed procedure is described in a later section.
12. Edit the following files:  
    - appsettings.json (For SiLA Server) : Set the items in the ServerConfig section as needed.
    - application.json (For CommandExecutor) : Set the "profileName" to the profile name of the Loader, and anything if you need to configure.

#### Launch the Software

1. Double-click MicroplateCentrifugeCommandExecutor.exe. After the Command Executor is launched, a light-purple square icon appears in the task tray.  
2. Double-click MicroplateCentrifugeController.Server.exe. A console window opens and hosting starts.

#### Quit the Server

1. Focus on the Console Window.
2. Press *Ctrl + C*. The console window will close.

#### Quit the Command Executor

1. Right-click the icon on task tray. A context menu appears.
2. Click **Exit** menu on the context menu.

#### Encryption

You can configure whether to enable TLS.  

If you want to enable it, follow the procedure below:
- Set "EnableTLS" in appsettings.json to True
- Place the public and private keys in the same directory as MicroplateCentrifugeController.Server.exe. The names of these files must be as follows:
  - Public key: server.crt
  - Private key: server.key
- If the software is already running, quit it and relaunch it

Note: For other certificate requirements, refer to the SiLA2 Documentation (especially *Encryption* in Part B).

If you want to disable it (i.e., run the SiLA Server in insecure mode), follow the procedure below:
- Set "EnableTLS" in appsettings.json to False
- If the software is already running, quit it and relaunch it

### Sample Scripts

You can find a simple script at `PythonClient/microplate_centrifuge_controller/examples/snip_microplate_centrifuge.py` that can be used to verify the basic operation of this software. The script contains example usage for each command and property. To test a specific command or property, follow the procedure below:
- Copy and paste the corresponding region from the script into your own script. Make sure your script is placed in the same directory as snip_microplate_centrifuge.py
- Set "host", "port", and "cert_path" in  `PythonClient/microplate_centrifuge_controller/examples/config.yml`
- Create and activate a virtual environment if you prefer to use one
- Install the mock server as a module by running the following command in the `PythonClient` directory
```bash
pip install .
```
- Run your script

## License

MIT License. Copyright (c) 2025-2026 RIKEN. See [LICENSE](LICENSE).

Third-party components are subject to their own licenses (see `Server/Licenses/`). The Agilent ActiveX controls used by the command executor are not included in this repository and are subject to Agilent's license terms.
