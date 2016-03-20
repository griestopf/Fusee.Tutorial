#Tutorial 01

##Goals
 - Understand how FUSEE deals with multiple platforms (desktop, web, Android).
 - Understand the basic setup of a FUSEE application.
	- Init contains initialization and startup code.
	- RenderAFrame is called repeatedly for each frame to draw.
 - Understand rendering pipeline basics.
 - Send simple geometry through the rendering pipeline.
 - Render a triangle.

##Get Started
 - Download and run the [FUSEE project] (https://github.com/FUSEEProjectTeam/Fusee) as described in [_Getting Started_] (https://github.com/FUSEEProjectTeam/Fusee/wiki/Getting-Started).
 - Open Fusee.Tutorial01.sln in Visual Studio (e.g. by double-clicking on the file).
 - The solution contains four projects
   ![Four Projects](_images/SolutionProjects.png)
   - *Core* - contains the main functionality of the application (- the "business logic")
   - *Desktop* - contains an Application for the (Windows) desktop loading and executing the Core functionality.
   - *Web* - contains a build process creating a JavaScript cross-compiled version of the Core functionality and generating an HTML page to load and execute that functionality.
   - *Android* - contains a [Xamarin] (https://xamarin.com/) project creating an Android APK loading and executing the Core functionality.
 