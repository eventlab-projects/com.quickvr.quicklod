# QuickLOD

__QuickLOD__ is a LOD pipeline that works on top of __Simplygon__, adding two main functionalities: 

1) Given a _Unity GameObject_, it allows to determine a specific reduction factor for each of the _Renderers_ that it contains. 
2) If the original mesh has blendshapes, those are transfered to the simplified mesh. 

This work is funded by the European Research Council (ERC) Advanced Grant Moments in Time in Immersive Virtual Environments (MoTIVE) 742989.

# Install

First of all you need to install __Simplygon__ in your Unity project. Follow the official instructions here:

https://www.youtube.com/watch?v=gy7eHl0cpg4

Now in the Unity project, select the _Simplygon.Unity.EditorPlugin.dll_ that you have just copied into the Assets folder, and in the _Inspector_ window, on _Include Platforms_ make sure that only _Editor_ is selected. Otherwise you won't be able to build your project. 

Next Go to _Window > Package Manager_ and click on the ‘+’ symbol in the top left corner of the new window. Select _Add package from git URL…_

![](/Documentation~/img/install/00.png)

A text field will open. Copy and paste the following URL, and then click on _Add_. 

https://github.com/eventlab-projects/com.quickvr.quickbase.git

__Now be patient__. Sometimes it seems that Unity does not produce any kind of visual feedback and it looks like nothing is happening, but the package is downloading. Then it will be automatically imported. 

Done! Follow the documentation on the wiki to learn how to use QuickLOD. 

# Known Issues 

It looks like newest __Simplygon__ version does not work with USD v3.0.0-exp-2. So USD package (com.unity.formats.usd) must be at version 3.0.0-exp.1 at much. 

