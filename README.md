# QuickLOD

__QuickLOD__ is a LOD pipeline that works on top of __Simplygon__, adding two main functionalities: 

1) Given a _Unity GameObject_, it allows to determine a specific reduction factor for each of the _Renderers_ that it contains. 
2) If the original mesh has blendshapes, those are transfered to the simplified mesh. 

This work is funded by the European Research Council (ERC) Advanced Grant Moments in Time in Immersive Virtual Environments (MoTIVE) 742989.

# Install

First of all you need to install [__Simplygon__](https://www.simplygon.com/downloads#latest-releases) in your Unity project. Then locate _Simplygon's_ plugin for _Unity_ (by default it will be located at _C:\Program Files\Simplygon\\[VersionNumber]\Unity\bin_). 

![](/Documentation~/img/install/00a.png)

Copy the file _Simplygon.Unity.EditorPlugin.dll_ in the _Assets_ folder of your _Unity_ project. 

![](/Documentation~/img/install/01.png)

Open the project using your _Unity Editor_. On the _Project_ window, select the _Simplygon.Unity.EditorPlugin.dll_ file that you have just copied, and in the _Inspector_ window, on _Include Platforms_ make sure that only _Editor_ is selected. Otherwise you won't be able to build your project. 

![](/Documentation~/img/install/02.png)

Next Go to _Window > Package Manager_ and click on the ‘+’ symbol in the top left corner of the new window. Select _Add package from git URL…_

A text field will open. Copy and paste the following URL, and then click on _Add_. 

https://github.com/eventlab-projects/com.quickvr.quicklod.git

Now just wait for the package to be installed. 

Done! Follow the documentation on the Wiki section to learn how to use QuickLOD. 
