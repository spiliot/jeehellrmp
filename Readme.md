#Jeehell RMP
##A virtual Radio Management Panel for Jeehell FMGS

![Jeehell RMP Photo](http://oi63.tinypic.com/52me6f.jpg)

###Introduction

[Jeehell FMGS](http://www.mycockpit.org/forums/forumdisplay.php?f=264) is an excellent Airbus systems simulator add-on for the Flight Simulator platforms (FSX/P3D). 
Since it focuses on home cockpit builders it lacks virtual panels for secondary cockpit functions that can be manipulated with hardware panels or programmatically. 
This is of course of limited use to anyone without appropriate hardware.

This project provides a virtual Airbus RMP panel for use with Jeehell.

###Application Requirements
You need at least .net Framework version 4 to run the application. 
Flight Simulator's SimConnect is used for interfacing to FS, the minimum version supported being FSX SP2. Any FS version after it (STEAM/P3D) should work. 
This has the added benefit of being able to run the panel on any network computer. Refer to SimConnect documentation on how to achieve this.

If you're not running FSX SP2 you may need to manually install the FSX SP2 SimConnect runtime library. P3D conviniently includes it in the `<P3D_installation>\redist\Interface\FSX-SP2-XPACK\retail\lib\SimConnect.msi` folder.

**NOTE:** Please place both *JeehellRMP.exe* and *JeehellRMP.exe.config* in the same folder or you might get a cryptic exception during program startup.

###Usage
If your flight simulator is running, the ACTIVE and STBY (standby) frequencies will be displayed and the VHF1 or VHF2 indicator will be lit according to the radio selected.

Left/Right clicking or spinning the mouse wheel on the outer/inner knobs will change the STBY frequency.

Press the TRANSFER button between the displays (the one with the green arrow) to tranfer the STBY frequency to the ACTIVE window and vice versa.

Select the radio to control with VHF1 or VHF2. FS only supports two radios (COM1/COM2) so the other radios (HF/AM) don't work.

A number of options are available by right clicking anywhere on the panel:

![Menu](http://i66.tinypic.com/28ji0cy.png)

 - Always on Top: Sets the window over every other window
 - Rotate Clockwise/Counterclockwise: Something obvious will happen (sorry)
 - Lock Position: The window title and resize border will disappear and you won't be able to move the window
 - Keep Proportions: Maintains the original panel proportions. Disable to allow stretching to window size
 - Jeehell colors: If you prefer the lighter panel color that the other Jeehell panels have I won't blame you
 - Save Settings: The current position, size and settings will be applied during each subsequent panel startup
 - Reset Settings: Handy if you completely messed up everything.

If connection to FS is lost, or no connection is established yet, the SEL lamp will be lit orange.
If frequencies are invalid (i.e. no aircraft is loaded) the panel will display `---.---` in the frequency windows.

**Note:** At this time the panel doesn't communicate directly with Jeehell FMGS, so the STBY NAV buttons and SEL indicator don't function.

###Development guidelines and build requirements
This is a C# .net WPF application created with Visual Studio 2015. The code should be straight forward so no comments should be needed but are allowed when deemed nessecary.

XAML is used for describing the UI elements. Style triggers are used for differentiating their visual output based on Data Binding values. Some templating needs to be employed in order to further declutter the XAML code and keep it DRY.

The `SimData` class can serve as a blueprint for using SimConnect on your own C# application. The `RmpData` class implements the `INotifyPropertyChanged` interface providing a WPF-aware layer around `SimData`, thus allowing its properties to be bound to XAML elements.

To build you will need to provide the path to FSX SP2 SimConnect managed library. Unfortunately it can't be distributed in this project but is freely available from Microsoft. Here's a link to [FSX SP2 SDK](https://www.microsoft.com/Products/Games/FSInsider/downloads/Pages/FSXSDK-SP2Update.aspx) which will completely replace BUT at the same time require FSX SP1 SDK which will in turn completely replace but require FSX SDK. I know; Microsoft. Instead, FSX SP2 managed library can be found in the *redist* folder of Lockheed Martin's Prepar3D installation (look at *Usage* for complete path).

I'm not a seasoned WPF developer and if you feel something is going the wrong direction do let me know. If you happen to know how to use a singleton class for data binding XAML elements or how to bind XAML element events to an internal class (sparing over-complicated hacks), I, and the rest of the internet looking for a solution to this, will be grateful.

If you decide to push code, many thanks in advance. Remember that the `master` branch should always have complete, working code (only bug fixes allowed). The `development` branch should also always have working code but it can be feature incomplete towards a release. Breaking/work-in-progress code in separate branches please.

###Contributing
Use Github Issues for suggesting features and filing bugs and Pull Requests for suggesting/fixing code. I welcome any feedback but since this is done on my spare time I can't provide any guarranties on timely response.

###License
[Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International](https://creativecommons.org/licenses/by-nc-sa/4.0/)

###Some typical yada-yada
For Flight Simulation use only. No warranties, use at your own risk.