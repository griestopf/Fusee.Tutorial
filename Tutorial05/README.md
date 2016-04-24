#Tutorial 05

##Goals
 - Understand FUSEE's built-in SceneGraph and traversal functionality
 - Implement a more complex shader with one arbitrary light source

##Shaders are Assets now 
Build and run Tutorial 05. The output looks like we left off at [Tutorial 04](../Tutorial04). Under the hood we have one small enhancement:
Vertex and Pixel Shader are now in seperate source files: `VertexShader.vert` and `PixelShader.frag`. Both files can be found
under the `Assets` folder in the Core project.

![Shaders are assets now](_images/ShaderAssets.png)

All text files marked as `Content` with either file extension `.vert` (for vertex shader) or `.frag` (for fragment shader - the OpenGL terminology
for pixel shader) will be compiled using the [OpenGL reference compiler] (https://www.khronos.org/opengles/sdk/tools/Reference-Compiler/) *before*
the C# source code itself will be compiled. Thus you will get errors reported directly in the build output and will not have to run the program
to find any errors in your shader code wrapped in some exceptions at run-time.

In addition you might want to install the latest [release of the NShader Syntax Hilighter] (https://github.com/samizzo/nshader/releases) 
(See the [project web site] (http://www.horsedrawngames.com/shader-syntax-highlighting-in-visual-studio-2013/) for additional information).
This Visual Studio Extension produces visual support like shown below while editing your shader code:

![Syntax Hilighting in shader code](_images/ShaderSyntaxHilight.png)


##FUSEE's built-in SceneGraph
In Tutorial 04 we created a simple class, `SceneOb` that we used to create hierarchical scene graphs. In addition we added a method `RenderSceneOb()` 
that went through a tree of `SceneOb` instances and rendered the contents of the individual `SceneOb`s. From now on we will call the process of 
going through a hierarchy of objects ***traversing*** the tree. In addition we will call a hierarchy of objects making up parts of a 3D scene
a **Scene Graph***.

FUSEE already comes with a set of classes allowing to build up Scene Graphs. You can find these classes in the [Fusee.Serialization project]
(https://github.com/FUSEEProjectTeam/Fusee/tree/develop/src/Serialization). All classes found here can be serialized and deserialized using 
automatically generated serialization code. In Tutorial 04 you already used these classes to load a `.fus` file and retrieve some 
Mesh data out of it. But `.fus` can not only store simple meshes but can contain complete scene graphs.

A Scene Graph in FUSEE is always a tree of `SceneNodeContainer` objects (we will just call them *nodes*). 
Take a look at the [source code] (https://github.com/FUSEEProjectTeam/Fusee/blob/develop/src/Serialization/SceneNodeContainer.cs)
and you will notice that its declaration is very short: Besides a name, each node is only made up of two lists

 1. A list of `SceneComponentContainer` objects - we will just call them *components*

 2. A list of child nodes - this is how a hierarchy can be built just with our DIYS-`SceneObs
  
Let's talk about components a little bit. In the `Fusee.Serialization` project you can find a lot of classes derived from `SceneComponentContainer`
such as

 - `MeshComponent` - you already dealt with it in Tutorial 04
 - `TransformComponent` - contains a position, rotation and scale information
 - `MaterialComponent` - contains a material description (e.g. a color)

just to name a few. These components are the building blocks containing the contents a scene is made of. Not every node must contain a comlete
set of parameters. Some nodes are just there to group other nodes, so they don't contain any component at all. Some nodes might be a group
and at the same time allow their children to be transformed simultaneously in world space, the such a node may contain a transform component only.
Other nodes contain a complete set including a mesh, a transform and a material. And as you can see, there are more types of components 
which we will not talk about during this tutorial.

On the outermost level of a `.fus` file there is always one `SceneContainer` object making the "root" of the tree. We will simply call it
a *scene*. It contains a list of `Children` of `SceneNodeContainer` objects. This is the list of nodes at the root level. In a addition a 
scene containts some header information about the `.fus` file.

To summarize, you can imagine the contents of a `.fus` file as the example tree in the following image:

![Example Tree] (_images/SceneHierarchy.png)

The orange object is the one-and-only `SceneContainer` root instance. The yellow squares are `SceneNodeContainer` objects (*nodes*) 
and the green circles are different derivations of `SceneComponentContainer` instances (*components*).

The numbers are the order in which a traversal will visit each node and component. 


Now let's look at an example of a scene. The following image is a hierarchical model created in CINEMA 4D, a commercial 3D modelling software:

![Wuggy 3D Model](_images/WuggyModel.png)

Here you can see the scene graph inside the modelling software:

![Wuggy Scene Graph](_images/WuggySceneGraph.png)

Now we want to load this scene in our source code. Add the model file 'wuggy.fus' to the Assets sub folder in the `Fusee.Tutorial05.Core`
project and don't forget to set its properties to `Content` and `Copy if newer`. Add the following loading code to the `Init()` method of the `Tutorial` class in [Tutorial.cs] (Core/Tutorial.cs).

```C#
   SceneContainer wuggy = AssetStorage.Get<SceneContainer>("wuggy.fus");
```

Compile the code and set a breakpoint to the line right after the above. Starting the tutorial in the debugger will break at the position right
after deserializing the contents of `wuggy.fus` into an object tree. Drag the `wuggy` variable into the debugger's watch window and inspect
its contents:

![Wuggy in the debugger's Watch window](_images/WuggyDebugWatch.png)

###Practice
 - Draw an image of the hierarchy contained in `wuggy` using squares and circles like the image above.
 - Convince yourself about the 1:1 connection of the hierarchy in the `wuggy` variable and the scene 
   graph image from the modelling software above.
 - Look inside the various Components. What information is contained in the `MaterialComponent`, `TransformComponent` and `MeshComponent` types?
 
 
##Rendering with a Visitor 
To render a scene like the one stored in `wuggy` we need to *recursively* traverse all `Children` of the root `SceneContainer`. We already 
implemented a simple rendering traversal with our `RenderSceneOb()` method. But rendering is not the only purpose to traverse a scene. 
Here are two traversal reasons other than rendering:
 - Find a node or component based on some search criterion (e.g. all nodes with a certain name, all meshes with more than 50 triangles, etc.)
 - Picking - finding all meshes and nodes under a given position or withtin a rectangular range given in screen space, such as 
   the current mouse position.

So we now have the situation that we have a set of building blocks of different component types to build up our hierarchy and we 
also have a couple of different actions that should take place when traversing. So the action that occurs depends on two things:
 1. The type of the component being traversed
 2. The "reason" for traversing such as rendering, searchgin, picking, etc.
 
In computer science this problem and a solution is treated under the keyword ***Visitor Pattern***, or ***Double Dispatch***. FUSEE
comes with an implementation based on the classical Visitor Pattern and some extensions built around it to enable programmers
using scenes to easily implement their own traversals and at the same time extend the set of `SceneNodeContainer` classes for 
their own needs. These implementions around the core `SceneVisitor` class can be found in the [Fusee.Xene] (https://github.com/FUSEEProjectTeam/Fusee/tree/develop/src/Xene) subproject. You can also find some additional information in 
the [Fusee.Xene.md] (https://github.com/FUSEEProjectTeam/Fusee/blob/develop/src/Xene/Fusee.Xene.md) document.

To Implement your own Rendering Visitor you should do the following.
 
 1. Create a class derived from `Fusee.Xene.SceneVisitor` and add three visitor methods for mesh, transform and material
    components:

	```C#
	class Renderer : SceneVisitor
    {
        [VisitMethod]
        public void OnMesh(MeshComponent mesh)
        {
        }
        [VisitMethod]
        public void OnMaterial(MaterialComponent material)
        {
        }
        [VisitMethod]
        public void OnTransform(TransformComponent xform)
        {
        }
    }
	```

	The name of the class as well as the name of the methods may vary. Note how the methods are attributed with the
	`VisitMethod` attribute and how methods vary in the different parameter types all derived from `SceneComponentContainer`.
	
 2. Add two fields to the `Tutorial` class: One to keep the `_wuggy` scene and another one to keep an instance of our newly
    created `Renderer`:
	
	```C#
	    private SceneContainer _wuggy;
        private Renderer _renderer;
	```
	
 3. In the `Init()` method load the contents of wuggy.fus into the `_wuggy` field and initiate an instance of our `Renderer`:

 	```C#
		_wuggy = AssetStorage.Get<SceneContainer>("wuggy.fus");
		_renderer = new Renderer();
	```

 4. In the `RenderAFrame()` method, somwhere between `Clear()`ing the back buffer and `Present()`ing the contents to the front
    buffer, use our `Renderer` to traverse the wuggy scene:
	
	
		
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   
   

 